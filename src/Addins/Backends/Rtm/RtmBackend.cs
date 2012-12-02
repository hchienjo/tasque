//
// RtmBackend.cs
//
// Author:
//       Antonius Riha <antoniusriha@gmail.com>
//
// Copyright (c) 2012 Antonius Riha
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace Tasque.Backends.Rtm
{
	public class RtmBackend : IBackend
	{
		public RtmBackend ()
		{
			taskComparer = new TaskComparer ();
			categoryComparer = new CategoryComparer ();

			tasks = new ObservableCollection<ITask> ();
			categories = new ObservableCollection<ICategory> ();

			Tasks = new ReadOnlyObservableCollection<ITask> (tasks);
			Categories = new ReadOnlyObservableCollection<ICategory> (categories);
		}

		public string Name { get { return "Remember the Milk"; } }
		
		public ICollection<ITask> Tasks { get; private set; }
		
		public ICollection<ICategory> Categories { get; private set; }
		
		public bool Configured { get; private set; }
		
		public bool Initialized { get; private set; }
		
		public IBackendPreferences Preferences {
			get { return new RtmPreferencesWidget (this, preferences); }
		}

		public ITask CreateTask (string taskName, ICategory category)
		{
			RtmTask rtmTask = null;

			string categoryID = null;
			if (!(category is AllCategory))
				categoryID = (category as RtmCategory).ID;
			
			if (rtm != null) {
				try {
					RtmNet.List list;
					if (categoryID == null)
						list = rtm.TasksAdd (timeline, taskName);
					else
						list = rtm.TasksAdd (timeline, taskName, categoryID);
					rtmTask = UpdateTaskFromResult (list);
				} catch (Exception e) {
					Logger.Debug ("Unable to set create task: " + taskName);
					Logger.Debug (e.ToString ());
				}
			} else
				throw new Exception ("Unable to communicate with Remember The Milk");
			
			return rtmTask;
		}

		public void DeleteTask (ITask task)
		{
			var rtmTask = task as RtmTask;
			if (rtm != null) {
				try {
					rtm.TasksDelete (timeline, rtmTask.ListID,
					                 rtmTask.SeriesTaskID, rtmTask.TaskTaskID);
					rtmTask.Delete ();
				} catch (Exception e) {
					Logger.Debug ("Unable to delete task: " + task.Name);
					Logger.Debug (e.ToString ());
				}
			} else
				throw new Exception ("Unable to communicate with Remember The Milk");
		}

		public void Refresh ()
		{
			if (rtmAuth == null)
				return;

			Logger.Debug("Refreshing data...");

			if (BackendSyncStarted != null)
				BackendSyncStarted ();

			var lists = rtm.ListsGetList ();
			UpdateCategories (lists);
			UpdateTasks (lists);

			if (BackendSyncFinished != null)
				BackendSyncFinished ();

			Logger.Debug("Done refreshing data!");
		}

		public void Initialize (Preferences preferences)
		{
			if (preferences == null)
				throw new ArgumentNullException ("preferences");
			this.preferences = preferences;
			
			// make sure we have the all Category in our list
			AllCategory allCategory = new AllCategory (preferences);
			AddCategory (allCategory);
			
			// *************************************
			// AUTHENTICATION to Remember The Milk
			// *************************************
			string authToken = preferences.Get (Tasque.Preferences.AuthTokenKey);
			if (authToken != null) {
				Logger.Debug ("Found AuthToken, checking credentials...");
				try {
					rtm = new RtmNet.Rtm (apiKey, sharedSecret, authToken);
					rtmAuth = rtm.AuthCheckToken (authToken);
					timeline = rtm.TimelineCreate ();
					Logger.Debug ("RTM Auth Token is valid!");
					Logger.Debug ("Setting configured status to true");
					Configured = true;
				} catch (RtmNet.RtmApiException e) {
					preferences.Set (Tasque.Preferences.AuthTokenKey, null);
					preferences.Set (Tasque.Preferences.UserIdKey, null);
					preferences.Set (Tasque.Preferences.UserNameKey, null);
					rtm = null;
					rtmAuth = null;
					Logger.Error ("Exception authenticating, reverting" + e.Message);
				} catch (RtmNet.ResponseXmlException e) {
					rtm = null;
					rtmAuth = null;
					Logger.Error ("Cannot parse RTM response. Maybe the service is down."
						+ e.Message);
				} catch (RtmNet.RtmWebException e) {
					rtm = null;
					rtmAuth = null;
					Logger.Error ("Not connected to RTM, maybe proxy: #{0}", e.Message);
				} catch (System.Net.WebException e) {
					rtm = null;
					rtmAuth = null;
					Logger.Error ("Problem connecting to internet: #{0}", e.Message);
				}
			}
			
			if (rtm == null)
				rtm = new RtmNet.Rtm (apiKey, sharedSecret);

			Refresh ();

			Initialized = true;
			if (BackendInitialized != null)
				BackendInitialized ();
		}

		public void Cleanup ()
		{
			tasks.Clear ();
			categories.Clear ();

			rtm = null;
			Initialized = false;
		}

		public event BackendInitializedHandler BackendInitialized;
		public event BackendSyncStartedHandler BackendSyncStarted;
		public event BackendSyncFinishedHandler BackendSyncFinished;

		#region Internals
		internal void DeleteTask (RtmTask task)
		{
			if (tasks.Remove (task))
				task.PropertyChanged -= HandlePropertyChanged;
		}

		internal void FinishedAuth ()
		{
			rtmAuth = rtm.AuthGetToken (frob);
			if (rtmAuth != null) {
				preferences.Set (Tasque.Preferences.AuthTokenKey, rtmAuth.Token);
				if (rtmAuth.User != null) {
					preferences.Set (Tasque.Preferences.UserNameKey, rtmAuth.User.Username);
					preferences.Set (Tasque.Preferences.UserIdKey, rtmAuth.User.UserId);
				}
			}
			
			var authToken = preferences.Get (Tasque.Preferences.AuthTokenKey);
			if (authToken != null) {
				Logger.Debug ("Found AuthToken, checking credentials...");
				try {
					rtm = new RtmNet.Rtm (apiKey, sharedSecret, authToken);
					rtmAuth = rtm.AuthCheckToken (authToken);
					timeline = rtm.TimelineCreate ();
					Logger.Debug ("RTM Auth Token is valid!");
					Logger.Debug ("Setting configured status to true");
					Configured = true;
					Refresh ();
				} catch (Exception e) {
					rtm = null;
					rtmAuth = null;				
					Logger.Error ("Exception authenticating, reverting" + e.Message);
				}	
			}
		}

		internal string GetAuthUrl ()
		{
			frob = rtm.AuthGetFrob ();
			string url = rtm.AuthCalcUrl (frob, RtmNet.AuthLevel.Delete);
			return url;
		}

		internal RtmCategory GetCategory (string id)
		{
			foreach (var item in categories) {
				var category = item as RtmCategory;
				if (category != null && category.ID == id)
					return category;
			}
			return null;
		}

		internal void UpdateTaskName (RtmTask task)
		{
			if (rtm != null) {
				try {
					RtmNet.List list = rtm.TasksSetName (timeline, task.ListID, task.SeriesTaskID, task.TaskTaskID, task.Name);		
					UpdateTaskFromResult (list);
				} catch (Exception e) {
					Logger.Debug ("Unable to set name on task: " + task.Name);
					Logger.Debug (e.ToString ());
				}
			}
		}
		
		internal void UpdateTaskDueDate (RtmTask task)
		{
			if (rtm != null) {
				try {
					if (task.DueDate == DateTime.MinValue)
						rtm.TasksSetDueDate (timeline, task.ListID, task.SeriesTaskID, task.TaskTaskID);
					else
						rtm.TasksSetDueDate (timeline, task.ListID, task.SeriesTaskID, task.TaskTaskID, task.DueDateString);
				} catch (Exception e) {
					Logger.Debug ("Unable to set due date on task: " + task.Name);
					Logger.Debug (e.ToString ());
				}
			}
		}
		
		internal void UpdateTaskPriority (RtmTask task)
		{
			if (rtm != null) {
				try {
					RtmNet.List list = rtm.TasksSetPriority (timeline, task.ListID, task.SeriesTaskID, task.TaskTaskID, task.PriorityString);
					UpdateTaskFromResult (list);
				} catch (Exception e) {
					Logger.Debug ("Unable to set priority on task: " + task.Name);
					Logger.Debug (e.ToString ());
				}
			}
		}
		
		internal void UpdateTaskActive (RtmTask task)
		{
			if (task.State == TaskState.Completed) {
				if (rtm != null) {
					try {
						rtm.TasksUncomplete (timeline, task.ListID, task.SeriesTaskID, task.TaskTaskID);
					} catch (Exception e) {
						Logger.Debug ("Unable to set Task as completed: " + task.Name);
						Logger.Debug (e.ToString ());
					}
				}
			}
		}
		
		internal void UpdateTaskCompleted (RtmTask task)
		{
			if (rtm != null) {
				try {
					rtm.TasksComplete (timeline, task.ListID, task.SeriesTaskID, task.TaskTaskID);
				} catch (Exception e) {
					Logger.Debug ("Unable to set Task as completed: " + task.Name);
					Logger.Debug (e.ToString ());
				}
			}
		}
		
		internal void MoveTaskCategory (RtmTask task, string id)
		{
			if (rtm != null) {
				try {
					rtm.TasksMoveTo (timeline, task.ListID, id, task.SeriesTaskID, task.TaskTaskID);
				} catch (Exception e) {
					Logger.Debug ("Unable to set Task as completed: " + task.Name);
					Logger.Debug (e.ToString ());
				}
			}					
		}

		internal RtmNote CreateNote (RtmTask rtmTask, string text)
		{
			RtmNet.Note note = null;
			RtmNote rtmNote = null;
			
			if (rtm != null) {
				try {
					note = rtm.NotesAdd (timeline, rtmTask.ListID, rtmTask.SeriesTaskID, rtmTask.TaskTaskID, String.Empty, text);
					rtmNote = new RtmNote (note);
				} catch (Exception e) {
					Logger.Debug ("RtmBackend.CreateNote: Unable to create a new note");
					Logger.Debug (e.ToString ());
				}
			} else
				throw new Exception ("Unable to communicate with Remember The Milk");
			
			return rtmNote;
		}

		internal void DeleteNote (RtmTask rtmTask, RtmNote note)
		{
			if (rtm != null) {
				try {
					rtm.NotesDelete (timeline, note.ID);
				} catch (Exception e) {
					Logger.Debug ("RtmBackend.DeleteNote: Unable to delete note");
					Logger.Debug (e.ToString ());
				}
			} else
				throw new Exception ("Unable to communicate with Remember The Milk");
		}
		
		internal void SaveNote (RtmTask rtmTask, RtmNote note)
		{
			if (rtm != null) {
				try {
					rtm.NotesEdit (timeline, note.ID, String.Empty, note.Text);
				} catch (Exception e) {
					Logger.Debug ("RtmBackend.SaveNote: Unable to save note");
					Logger.Debug (e.ToString ());
				}
			} else
				throw new Exception ("Unable to communicate with Remember The Milk");
		}
		#endregion

		#region My privates
		/// <summary>
		/// Update the model to match what is in RTM
		/// FIXME: This is a lame implementation and needs to be optimized
		/// </summary>		
		void UpdateCategories (RtmNet.Lists lists)
		{
			Logger.Debug ("RtmBackend.UpdateCategories was called");
			try {
				foreach (var list in lists.listCollection) {
					if (list.Smart == 1) {
						Logger.Warn ("Smart list \"{0}\" omitted", list.Name);
						continue;
					}
					
					var rtmCategory = new RtmCategory (list);
					if (categories.Any (c => c.Name == rtmCategory.Name))
						continue;
					
					AddCategory (rtmCategory);
				}
			} catch (Exception e) {
				Logger.Debug ("Exception in fetch " + e.Message);
			}
			Logger.Debug ("RtmBackend.UpdateCategories is done");			
		}
		
		/// <summary>
		/// Update the model to match what is in RTM
		/// FIXME: This is a lame implementation and needs to be optimized
		/// </summary>		
		void UpdateTasks (RtmNet.Lists lists)
		{
			Logger.Debug ("RtmBackend.UpdateTasks was called");
			try {
				foreach (var list in lists.listCollection) {
					// smart lists are based on criteria and therefore
					// can contain tasks that actually belong to another list.
					// Hence skip smart lists in task list population.
					if (list.Smart == 1)
						continue;
					
					RtmNet.Tasks rtmTasks = null;
					try {
						rtmTasks = rtm.TasksGetList (list.ID);
					} catch (Exception tglex) {
						Logger.Debug ("Exception calling TasksGetList (list.ListID) "
							+ tglex.Message);
					}
					
					if (rtmTasks != null) {
						foreach (var tList in rtmTasks.ListCollection) {
							if (tList.TaskSeriesCollection == null)
								continue;

							foreach (var ts in tList.TaskSeriesCollection)
								UpdateTaskCore (ts, tList.ID);
						}
					}
				}
			} catch (Exception e) {
				Logger.Debug ("Exception in fetch " + e.Message);
				Logger.Debug (e.ToString ());
			}
			Logger.Debug ("RtmBackend.UpdateTasks is done");			
		}

		RtmTask UpdateTaskFromResult (RtmNet.List list)
		{
			var ts = list.TaskSeriesCollection [0];
			if (ts != null)
				return UpdateTaskCore (ts, list.ID);
			return null;
		}

		RtmTask UpdateTaskCore (RtmNet.TaskSeries taskSeries, string listId)
		{
			RtmTask rtmTask = null;
			foreach (var task in taskSeries.TaskCollection) {
				rtmTask = new RtmTask (taskSeries, task, this, listId);
				if (tasks.Any (t => t.Id == rtmTask.Id))
					continue;
				
				rtmTask.PropertyChanged += HandlePropertyChanged;
				AddTask (rtmTask);
			}
			/* Always return the last task received */
			return rtmTask;
		}
		
		void AddCategory (ICategory category)
		{
			var index = categories.Count;
			var valIdx = categories.Select ((val, idx) => new { val, idx })
				.FirstOrDefault (x => categoryComparer.Compare (x.val, category) > 0);
			if (valIdx != null)
				index = valIdx.idx;
			categories.Insert (index, category);
		}
		
		void AddTask (RtmTask task)
		{
			var index = tasks.Count;
			var valIdx = tasks.Select ((val, idx) => new { val, idx })
				.FirstOrDefault (t => taskComparer.Compare (t.val, task) > 0);
			if (valIdx != null)
				index = valIdx.idx;
			
			tasks.Insert (index, task);
		}
		
		void HandlePropertyChanged (object sender, PropertyChangedEventArgs e)
		{
			// when a property changes (any property atm), "reorder" tasks
			var task = (RtmTask)sender;
			if (tasks.Remove (task))
				AddTask (task);
		}

		ObservableCollection<ITask> tasks;
		ObservableCollection<ICategory> categories;
		TaskComparer taskComparer;
		CategoryComparer categoryComparer;

		Preferences preferences;

		const string apiKey = "b29f7517b6584035d07df3170b80c430";
		const string sharedSecret = "93eb5f83628b2066";

		RtmNet.Rtm rtm;
		string frob;
		RtmNet.Auth rtmAuth;
		string timeline;
		#endregion
	}
}
