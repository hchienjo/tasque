// RemoteControl.cs created with MonoDevelop
// User: sandy at 9:49 AM 2/14/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.Collections.Generic;
using System.Linq;

using Mono.Unix; // for Catalog.GetString ()
using Tasque;
using Tasque.Core;
using Tasque.DateFormatters;

#if ENABLE_NOTIFY_SHARP
using Notifications;
#endif

using org.freedesktop.DBus;
using DBus;

namespace Gtk.Tasque
{
	[Interface ("org.gnome.Tasque.RemoteControl")]
	public class RemoteControl : MarshalByRefObject
	{
		const string Namespace = "org.gnome.Tasque";
		const string Path = "/org/gnome/Tasque/RemoteControl";
		
		public static RemoteControl GetInstance ()
		{
			BusG.Init ();
			
			if (!Bus.Session.NameHasOwner (Namespace))
				Bus.Session.StartServiceByName (Namespace);
			
			return Bus.Session.GetObject<RemoteControl> (Namespace, new ObjectPath (Path));
		}
		
		public static RemoteControl Register (GtkApplicationBase application)
		{
			BusG.Init ();
			
			var remoteControl = new RemoteControl (application);
			Bus.Session.Register (new ObjectPath (Path), remoteControl);
			
			if (Bus.Session.RequestName (Namespace) != RequestNameReply.PrimaryOwner)
				return null;
			
			return remoteControl;
		}
		
		RemoteControl (GtkApplicationBase application)
		{
			if (application == null)
				throw new ArgumentNullException ("application");
			this.application = application;
		}
		
		public void KnockKnock ()
		{
			if (RemoteInstanceKnocked != null)
				RemoteInstanceKnocked ();
		}
		
		public System.Action RemoteInstanceKnocked { get; set; }
				
		/// <summary>
		/// Create a new task in Tasque using the given taskListName and name.
		/// Will not attempt to parse due date information.
		/// </summary>
		/// <param name="taskListName">
		/// A <see cref="System.String"/>.  The name of an existing taskList.
		/// Matches are not case-sensitive.
		/// </param>
		/// <param name="taskName">
		/// A <see cref="System.String"/>.  The name of the task to be created.
		/// </param>
		/// <param name="enterEditMode">
		/// A <see cref="System.Boolean"/>.  Specify true if the TaskWindow
		/// should be shown, the new task scrolled to, and have it be put into
		/// edit mode immediately.
		/// </param>
		/// <returns>
		/// A unique <see cref="System.String"/> which can be used to reference
		/// the task later.
		/// </returns>
		public string CreateTask (string taskListName, string taskName,
						bool enterEditMode)
		{
			return CreateTask (taskListName, taskName, enterEditMode, false);
		}

		/// <summary>
		/// Create a new task in Tasque using the given taskListName and name.
		/// </summary>
		/// <param name="taskListName">
		/// A <see cref="System.String"/>.  The name of an existing taskList.
		/// Matches are not case-sensitive.
		/// </param>
		/// <param name="taskName">
		/// A <see cref="System.String"/>.  The name of the task to be created.
		/// </param>
		/// <param name="enterEditMode">
		/// A <see cref="System.Boolean"/>.  Specify true if the TaskWindow
		/// should be shown, the new task scrolled to, and have it be put into
		/// edit mode immediately.
		/// </param>
		/// <param name="parseDate">
		/// A <see cref="System.Boolean"/>.  Specify true if the 
		/// date should be parsed out of the taskName (in case 
		/// Preferences.ParseDateEnabledKey is true as well).
		/// </param>
		/// <returns>
		/// A unique <see cref="System.String"/> which can be used to reference
		/// the task later.
		/// </returns>
		public string CreateTask (string taskListName, string taskName,
						bool enterEditMode, bool parseDate)
		{
			var model = application.BackendManager.TaskLists;
			
			//
			// Validate the input parameters.  Don't allow null or empty strings
			// be passed-in.
			//
			if (taskListName == null || taskListName.Trim () == string.Empty
					|| taskName == null || taskName.Trim () == string.Empty) {
				return string.Empty;
			}
			
			//
			// Look for the specified taskList
			//
			if (model.Count == 0) {
				return string.Empty;
			}
			
			ITaskList taskList = model.FirstOrDefault (c => c.Name.ToLower () == taskListName.ToLower ());
			
			if (taskList == null) {
				return string.Empty;
			}
			
			// If enabled, attempt to parse due date information
			// out of the taskName.
			DateTime taskDueDate = DateTime.MinValue;
			if (parseDate && application.Preferences.GetBool (PreferencesKeys.ParseDateEnabledKey))
				TaskParser.Instance.TryParse (
				                         taskName,
				                         out taskName,
				                         out taskDueDate);
			ITask task = null;
			try {
				task = taskList.CreateTask (taskName);
				taskList.Add (task);
				if (taskDueDate != DateTime.MinValue)
					task.DueDate = taskDueDate;
			} catch (Exception e) {
				Logger.Error ("Exception calling Application.Backend.CreateTask from RemoteControl: {0}", e.Message);
				return string.Empty;
			}
			
			if (task == null) {
				return string.Empty;
			}
			
			if (enterEditMode) {
//				TaskWindow.SelectAndEdit (task, application);
			}
			
			#if ENABLE_NOTIFY_SHARP
			// Use notify-sharp to alert the user that a new task has been
			// created successfully.
			application.ShowAppNotification (
				Catalog.GetString ("New task created."), // summary
				Catalog.GetString (taskName)); // body
			#endif
			
			return task.Id;
		}
		
		/// <summary>
		/// Return an array of ITaskList names.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/>
		/// </returns>
		public string[] GetTaskListNames ()
		{
			List<string> taskLists = new List<string> ();
			string[] emptyArray = taskLists.ToArray ();

			var model = application.BackendManager.TaskLists;
			
			if (model.Count == 0)
				return emptyArray;
			
			foreach (var item in model) {
				if (!(item is AllList))
					taskLists.Add (item.Name);
			}
			
			return taskLists.ToArray ();
		}
		
		public void ShowTasks ()
		{
//			TaskWindow.ShowWindow (application);
		}
		
		/// <summary>
		/// Retreives the IDs of all tasks for the current backend.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/> array containing the ID of all tasks
		/// in the current backend.
		/// </returns>
		public string[] GetTaskIds ()
		{
			var ids = new List<string> ();
			var model = application.BackendManager.Tasks;

			if (model.Count == 0)
				return new string[0];
			
			foreach (var item in model)
				ids.Add (item.Id);
			
			return ids.ToArray ();
		}
		
		/// <summary>
		/// Gets the name of a task for a given ID
		/// </summary>
		/// <param name="id">
		/// A <see cref="System.String"/> for the ID of the task
		/// </param>
		/// <returns>
		/// A <see cref="System.String"/> the name of the task
		/// </returns>
		public string GetNameForTaskById (string id)
		{
			ITask task = GetTaskById (id);
			return task != null ? task.Text : string.Empty;
		}
		
		/// <summary>
		/// Sets the name of a task for a given ID
		/// </summary>
		/// <param name="id">
		/// A <see cref="System.String"/> for the ID of the task
		/// </param>
		/// <param>
		/// A <see cref="System.String"/> the name of the task
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>, true for success, false
		/// for failure.
		/// </returns>
		public bool SetNameForTaskById (string id, string name)
		{
			ITask task = GetTaskById (id);
			if (task == null)
			{
				return false;
			}
			task.Text = name;
			return true;
		}

		/// <summary>
		/// Gets the list of a task for a given ID
		/// </summary>
		/// <param name="id">
		/// A <see cref="System.String"/> for the ID of the task
		/// </param>
		/// <returns>
		/// A <see cref="System.String"/> the list of the task
		/// </returns>
		public string GetTaskListForTaskById (string id)
		{
			var task = GetTaskById (id);
			var list = application.BackendManager.TaskLists.FirstOrDefault (
				l => !(l is AllList) && l.Contains (task));
			return task != null ? list.Name : string.Empty;
		}
		
		/// <summary>
		/// Sets the list of a task for a given ID
		/// </summary>
		/// <param name="id">
		/// A <see cref="System.String"/> for the ID of the task
		/// </param>
		/// <param name="listName">
		/// A <see cref="System.String"/> the list of the task
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>, true for success, false
		/// for failure.
		/// </returns>
		public bool SetTaskListForTaskById (string id,
			string listName)
		{
			var task = GetTaskById (id);
			if (task == null)
				return false;

			// NOTE: the previous data model had a one taskList to many tasks
			// relationship. Now it's many-to-many. However, we stick to the
			// old model until a general overhaul.
			var lists = application.BackendManager.TaskLists;
			var prevList = lists.FirstOrDefault (
				l => !(l is AllList) && l.Contains (task));
			var newList = lists.FirstOrDefault (
				l => l.Name == listName);

			if (prevList != null)
				prevList.Remove (task);

			if (newList != null) {
				newList.Add (task);
				return true;
			}

			return false;
		}
		
		/// <summary>
		/// Get the due date of a task for a given ID
		/// </summary>
		/// <param name="id">
		/// A <see cref="System.String"/> for the ID of the task
		/// </param>
		/// <returns>
		/// A <see cref="System.Int32"/> containing the POSIX time
		/// of the due date
		/// </returns>
		public int GetDueDateForTaskById (string id)
		{
			ITask task = GetTaskById (id);
			if (task == null)
				return -1;
			if (task.DueDate == DateTime.MinValue)
				return 0;
			return (int)(task.DueDate - new DateTime(1970,1,1)).TotalSeconds;
		}
		
		/// <summary>
		/// Set the due date of a task for a given ID
		/// </summary>
		/// <param name="id">
		/// A <see cref="System.String"/> for the ID of the task
		/// </param>
		/// <param name="duedate">
		/// A <see cref="System.Int32"/> containing the POSIX time
		/// of the due date
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>, true for success, false
		/// for failure.
		/// <returns>
		public bool SetDueDateForTaskById (string id, int dueDate)
		{
			ITask task = GetTaskById (id);
			if (task == null)
			{
				return false;
			}
			if (dueDate == 0)
				task.DueDate = DateTime.MinValue;
			else
				task.DueDate = new DateTime(1970,1,1).AddSeconds(dueDate);
			return true;
		}
		
		/// <summary>
		/// Gets the state of a task for a given ID
		/// </summary>
		/// <param name="id">
		/// A <see cref="System.String"/> for the ID of the task
		/// </param>
		/// <returns>
		/// A <see cref="System.Int32"/> the state of the task
		/// </returns>
		public int GetStateForTaskById (string id)
		{
			ITask task = GetTaskById (id);
			return task != null ? (int) task.State : -1;
		}

		/// <summary>
		/// Gets the priority of a task for a given ID
		/// </summary>
		/// <param name="id">
		/// A <see cref="System.String"/> for the ID of the task
		/// </param>
		/// <returns>
		/// A <see cref="System.Int32"/> the priority of the task
		/// </returns>
		public int GetPriorityForTaskById (string id)
		{
			ITask task = GetTaskById (id);
			return task != null ? (int) task.Priority : -1;
		}
		
		/// <summary>
		/// Sets the priority of a task for a given ID
		/// </summary>
		/// <param name="id">
		/// A <see cref="System.String"/> for the ID of the task
		/// </param>
		/// <param name="priority">
		/// A <see cref="System.Int32"/> the priority of the task
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>, true for success, false
		/// for failure.
		/// </returns>
		public bool SetPriorityForTaskById (string id, int priority)
		{
			ITask task = GetTaskById (id);
			if (task == null)
			{
				return false;
			}
			task.Priority = (TaskPriority) priority;
			return true;
		}
		
		/// <summary>
		/// Marks a task active
		/// </summary>
		/// <param name="id">
		/// A <see cref="System.String"/> for the ID of the task
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>, true for success, false
		/// for failure.
		/// </returns>
		public bool MarkTaskAsActiveById (string id)
		{
			ITask task = GetTaskById (id);
			if (task == null)
				return false;
				
			task.Activate ();
			return true;
		}
		
		/// <summary>
		/// Marks a task complete
		/// </summary>
		/// <param name="id">
		/// A <see cref="System.String"/> for the ID of the task
		/// </param>
		public void MarkTaskAsCompleteById (string id)
		{
			ITask task = GetTaskById (id);
			if (task == null)
				return;
				
			if (task.State == TaskState.Active) {
				// Complete immediately; no timeout or fancy
				// GUI stuff.
				task.Complete ();
			}
		}
		
		/// <summary>
		/// Deletes a task
		/// </summary>
		/// <param name="id">
		/// A <see cref="System.String"/> for the ID of the task
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>, true for sucess, false
		/// for failure.
		/// </returns>
		public bool DeleteTaskById (string id)
		{
			ITask task = GetTaskById (id);
			if (task == null)
				return false;
			
			var taskList = application.BackendManager.TaskLists.First (
				l => !(l is AllList) && l.Contains (task));
			taskList.Remove (task);
			return true;
		}
		
		/// <summary>
		/// Looks up a task by ID in the backend
		/// </summary>
		/// <param name="id">
		/// A <see cref="System.String"/> for the ID of the task
		/// </param>
		/// <returns>
		/// A <see cref="ITask"/> having the given ID
		/// </returns>
		private ITask GetTaskById (string id)
		{
			var model = application.BackendManager.Tasks;
			return model.FirstOrDefault (t => t.Id == id);
		}

		GtkApplicationBase application;
	}
}
