// SqliteBackend.cs created with MonoDevelop
// User: boyd at 7:10 AM 2/11/2008
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Mono.Data.Sqlite;
using Tasque.Backends;
using Gtk.Tasque.Backends.Sqlite;

namespace Tasque.Backends.Sqlite
{
	public class SqliteBackend : IBackend
	{
		private bool initialized;
		private bool configured = true;

		ObservableCollection<Task> taskStore;
		ObservableCollection<TaskList> taskListListStore;
		ReadOnlyObservableCollection<Task> readOnlyTaskStore;
		ReadOnlyObservableCollection<TaskList> readOnlyTaskListStore;
		
		TaskComparer taskComparer;
		TaskListComparer taskListComparer;
		
		private Database db;

		public event EventHandler BackendInitialized;
		public event EventHandler BackendSyncStarted;
		public event EventHandler BackendSyncFinished;
		
		SqliteList defaultTaskList;
		//SqliteTaskList workTaskList;
		//SqliteTaskList projectsTaskList;
		
		public SqliteBackend ()
		{
			initialized = false;
			taskStore = new ObservableCollection<Task> ();
			taskListListStore = new ObservableCollection<TaskList> ();
			readOnlyTaskStore = new ReadOnlyObservableCollection<Task> (taskStore);
			readOnlyTaskListStore
				= new ReadOnlyObservableCollection<TaskList> (taskListListStore);
			taskComparer = new TaskComparer ();
			taskListComparer = new TaskListComparer ();
		}
		
		#region Public Properties
		public string Name
		{
			get { return "Local File"; } // TODO: Return something more usable to the user like, "Built-in" or whatever
		}
		
		/// <value>
		/// All the tasks including ITaskDivider items.
		/// </value>
		public ICollection<Task> Tasks
		{
			get { return readOnlyTaskStore; }
		}
		
		/// <value>
		/// This returns all the task lists (taskLists) that exist.
		/// </value>
		public ICollection<TaskList> TaskLists
		{
			get { return readOnlyTaskListStore; }
		}
		
		/// <value>
		/// Indication that the Sqlite backend is configured
		/// </value>
		public bool Configured 
		{
			get { return configured; }
		}
		
		
		/// <value>
		/// Inidication that the backend is initialized
		/// </value>
		public bool Initialized
		{
			get { return initialized; }
		}		
		
		public Database Database
		{
			get { return db; }
		}
		
		public IBackendPreferences Preferences {
			get {
				// TODO: Replace this with returning null once things are going
				// so that the Preferences Dialog doesn't waste space.
				return new SqlitePreferences ();
			}
		}
		#endregion // Public Properties
		
		#region Public Methods
		public Task CreateTask (string taskName, TaskList taskList)
		{
			// not sure what to do here with the taskList
			SqliteTask task = new SqliteTask (this, taskName);
			
			// Determine and set the task taskList
			if (taskList == null || taskList is Tasque.AllList)
				defaultTaskList.Add (task); // Default to work
			else
				taskList.Add (task);
			
			AddTask (task);
			task.PropertyChanged += HandlePropertyChanged;
			
			return task;
		}
		
		public void DeleteTask(Task task)
		{
			//string id = task.Id;
			task.Delete ();
			//string command = "delete from Tasks where id=" + id;
			//db.ExecuteNonQuery (command);
		}
		
		public void Refresh()
		{}
		
		public void Initialize (IPreferences preferences)
		{
			if (preferences == null)
				throw new ArgumentNullException ("preferences");

			if(db == null)
				db = new Database();
				
			db.Open();
			
			//
			// Add in the "All" TaskList
			//
			var allList = new AllList (this, preferences);
			AddTaskList (allList);

			RefreshTaskLists();
			RefreshTasks();

			initialized = true;
			if(BackendInitialized != null) {
				BackendInitialized(null, null);
			}
		}

		public void Dispose()
		{
			if (disposed)
				return;

			this.taskListListStore.Clear();
			this.taskStore.Clear();

			if (db != null)
				db.Close();
			db = null;
			initialized = false;
			disposed = true;
		}

		/// <summary>
		/// Given some text to be input into the database, do whatever
		/// processing is required to make sure special characters are
		/// escaped, etc.
		/// </summary>
		public string SanitizeText (string text)
		{
			return text.Replace ("'", "''");
		}
		
		#endregion // Public Methods
		public void RefreshTaskLists()
		{
			SqliteList newTaskList;
			bool hasValues = false;
			
			string command = "SELECT id, name FROM Categories";
			SqliteCommand cmd = db.Connection.CreateCommand();
			cmd.CommandText = command;
			SqliteDataReader dataReader = cmd.ExecuteReader();
			while(dataReader.Read()) {
			    int id = dataReader.GetInt32(0);
				var name = dataReader.GetString (1);
				hasValues = true;
				
				newTaskList = new SqliteList (this, id, name);
				if( (defaultTaskList == null) || (newTaskList.Name.CompareTo("Work") == 0) )
					defaultTaskList = newTaskList;
				AddTaskList (newTaskList);
			}
			
			dataReader.Close();
			cmd.Dispose();

			if(!hasValues)
			{
				defaultTaskList = newTaskList = new SqliteList (this, "Work");
				AddTaskList (defaultTaskList);

				newTaskList = new SqliteList (this, "Personal");
				AddTaskList (newTaskList);
				
				newTaskList = new SqliteList (this, "Family");
				AddTaskList (newTaskList);

				newTaskList = new SqliteList (this, "Project");
				AddTaskList (newTaskList);
			}
		}

		public void RefreshTasks()
		{
			SqliteTask newTask;
			bool hasValues = false;
			
			string command = "SELECT id,Category,Name,DueDate,CompletionDate,Priority, State FROM Tasks";
			SqliteCommand cmd = db.Connection.CreateCommand();
			cmd.CommandText = command;
			SqliteDataReader dataReader = cmd.ExecuteReader();
			while(dataReader.Read()) {
				int id = dataReader.GetInt32(0);
				int taskList = dataReader.GetInt32(1);
				string name = dataReader.GetString(2);
				long dueDate = dataReader.GetInt64(3);
				long completionDate = dataReader.GetInt64(4);
				int priority = dataReader.GetInt32(5);
				int state = dataReader.GetInt32(6);
				
				hasValues = true;
				
				newTask = new SqliteTask (this, id, name, dueDate,
				                          completionDate, priority, state);
				var list = TaskLists.Single (l => {
					var sqliteList = l as SqliteList;
					if (sqliteList != null)
						return sqliteList.ID == taskList;
					return false;
				});
				list.Add (newTask);
				AddTask (newTask);
				newTask.PropertyChanged += HandlePropertyChanged;
			}
			
			dataReader.Close();
			cmd.Dispose();
			
			if(!hasValues)
			{
				newTask = new SqliteTask (this, "Create some tasks");
				defaultTaskList.Add (newTask);
				newTask.DueDate = DateTime.Now;
				newTask.Priority = TaskPriority.Medium;
				AddTask (newTask);
				newTask.PropertyChanged += HandlePropertyChanged;
			}
		}

		#region Private Methods
		internal void DeleteTask (SqliteTask task)
		{
			if (taskStore.Remove (task))
				task.PropertyChanged -= HandlePropertyChanged;
		}

		void AddTaskList (TaskList taskList)
		{
			var index = taskListListStore.Count;
			var valIdx = taskListListStore.Select ((val, idx) => new { val, idx })
				.FirstOrDefault (x => taskListComparer.Compare (x.val, taskList) > 0);
			if (valIdx != null)
				index = valIdx.idx;
			taskListListStore.Insert (index, taskList);
		}
		
		void AddTask (SqliteTask task)
		{
			var index = taskStore.Count;
			var valIdx = taskStore.Select ((val, idx) => new { val, idx })
				.FirstOrDefault (t => taskComparer.Compare (t.val, task) > 0);
			if (valIdx != null)
				index = valIdx.idx;
			
			taskStore.Insert (index, task);
		}

		void HandlePropertyChanged (object sender, PropertyChangedEventArgs e)
		{
			// when a property changes (any property atm), "reorder" tasks
			var task = (SqliteTask)sender;
			if (taskStore.Remove (task))
				AddTask (task);
		}
		#endregion // Private Methods
		
		#region Event Handlers
		#endregion // Event Handlers

		bool disposed;
	}
}
