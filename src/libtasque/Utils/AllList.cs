//
// AllList.cs
//
// Original header:
// AllCategory.cs created with MonoDevelop
// User: boyd at 3:45 PM 2/12/2008
//
// Authors:
//       Unknown author
//       Antonius Riha <antoniusriha@gmail.com>
//
// Copyright (c) 2013 Antonius Riha
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Mono.Unix;
using Tasque.Core;

namespace Tasque.Utils
{
	public sealed class AllList : ITaskList
	{
		internal static void SetBackendManager (BackendManager manager)
		{
			if (manager == null)
				throw new ArgumentNullException ("manager");
			AllList.manager = manager;
		}

		static BackendManager manager;

		public AllList (IPreferences preferences)
		{
			if (manager == null)
				throw new InvalidOperationException ("Internal error. " +
					"BackendManager must be set beforehand.");
			if (preferences == null)
				throw new ArgumentNullException ("preferences");
			backendManager = manager;

			tasks = new ObservableCollection<ITask> ();
			
			taskListsToHide = preferences.GetStringList (
				PreferencesKeys.HideInAllTaskList);
			preferences.SettingChanged += (prefs, settingKey) => {
				if (settingKey != PreferencesKeys.HideInAllTaskList)
					return;
				taskListsToHide = prefs.GetStringList (
					PreferencesKeys.HideInAllTaskList);
				UpdateList ();
			};
			
			observedLists = new List<ITaskList> ();
			UpdateList ();
			
			((INotifyCollectionChanged)manager.TaskLists)
				.CollectionChanged += delegate { UpdateList (); };
		}

		public bool CanChangeName { get { return false; } }

		public string Name {
			get { return Catalog.GetString ("All"); }
			set {
				throw new InvalidOperationException (
					"The name of this list cannot be changed.");
			}
		}

		public TaskListType ListType { get { return TaskListType.Smart; } }

		public int Count { get { return tasks.Count; } }

		public bool IsReadOnly { get { return true; } }

		public bool Contains (ITask item)
		{
			return tasks.Contains (item);
		}

		public void CopyTo (ITask [] array, int arrayIndex)
		{
			tasks.CopyTo (array, arrayIndex);
		}

		public IEnumerator<ITask> GetEnumerator ()
		{
			return tasks.GetEnumerator ();
		}

		public event NotifyCollectionChangedEventHandler CollectionChanged {
			add {
				tasks.CollectionChanged += (sender, e) => { value (this, e); };
			}
			remove {
				tasks.CollectionChanged -= (sender, e) => { value (this, e); };
			}
		}

		#region Explicit content

		string ITasqueCore.Id { get { return null; } }

		bool ITaskList.SupportsSharingTasksWithOtherTaskLists {
			get { return true; }
		}

		ITask ITaskList.CreateTask (string text)
		{
			return null;
		}

		void ITasqueObject.Refresh () {}

		void ICollection<ITask>.Add (ITask item)
		{
			ThrowReadOnly ();
		}

		void ICollection<ITask>.Clear ()
		{
			ThrowReadOnly ();
		}

		bool ICollection<ITask>.Remove (ITask item)
		{
			return false;
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}

		event PropertyChangedEventHandler
			INotifyPropertyChanged.PropertyChanged {
			add {} remove {}
		}

		event PropertyChangingEventHandler
			INotifyPropertyChanging.PropertyChanging {
			add {} remove {}
		}

		#endregion

		void UpdateList ()
		{
			// clear all
			foreach (var list in observedLists)
				list.CollectionChanged -= HandleTaskListChanged;
			observedLists.Clear ();
			tasks.Clear ();

			// add all
			foreach (var list in backendManager.TaskLists) {
				if (list.ListType == TaskListType.Smart
				    || taskListsToHide.Contains (list.Name))
					continue;

				foreach (var task in list)
					AddTask (task);

				// register list
				observedLists.Add (list);
				list.CollectionChanged += HandleTaskListChanged;
			}
		}

		void HandleTaskListChanged (object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action) {
			case NotifyCollectionChangedAction.Add:
				AddTask (e.NewItems [0] as ITask);
				break;
			case NotifyCollectionChangedAction.Remove:
				RemoveTask (e.OldItems [0] as ITask);
				break;
			case NotifyCollectionChangedAction.Replace:
				RemoveTask (e.OldItems [0] as ITask);
				AddTask (e.NewItems [0] as ITask);
				break;
			case NotifyCollectionChangedAction.Reset:
				UpdateList ();
				break;
			}
		}

		void AddTask (ITask task)
		{
			if (!tasks.Contains (task))
				tasks.Add (task);
		}

		void RemoveTask (ITask task)
		{
			// if the old task exists exactly once, remove it
			if (observedLists.Count (l => l.Contains (task)) == 1)
				tasks.Remove (task);
		}

		void ThrowReadOnly ()
		{
			throw new InvalidOperationException (
				"This collection is read-only.");
		}

		// A "set" of taskLists specified by the user to show when the "All"
		// taskList is selected in the TaskWindow. If the list is empty, tasks
		// from all taskLists will be shown. Otherwise, only tasks from the
		// specified lists will be shown.
		List<string> taskListsToHide;
		List<ITaskList> observedLists;
		ObservableCollection<ITask> tasks;
		BackendManager backendManager;
	}
}
