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

		ObservableCollection<ITask> taskStore;
		ObservableCollection<ICategory> categoryListStore;
		ReadOnlyObservableCollection<ITask> readOnlyTaskStore;
		ReadOnlyObservableCollection<ICategory> readOnlyCategoryStore;
		
		TaskComparer taskComparer;
		CategoryComparer categoryComparer;
		
		private Database db;

		public event BackendInitializedHandler BackendInitialized;
		public event BackendSyncStartedHandler BackendSyncStarted;
		public event BackendSyncFinishedHandler BackendSyncFinished;
		
		SqliteCategory defaultCategory;
		//SqliteCategory workCategory;
		//SqliteCategory projectsCategory;
		
		public SqliteBackend ()
		{
			initialized = false;
			taskStore = new ObservableCollection<ITask> ();
			categoryListStore = new ObservableCollection<ICategory> ();
			readOnlyTaskStore = new ReadOnlyObservableCollection<ITask> (taskStore);
			readOnlyCategoryStore
				= new ReadOnlyObservableCollection<ICategory> (categoryListStore);
			taskComparer = new TaskComparer ();
			categoryComparer = new CategoryComparer ();
		}
		
		#region Public Properties
		public string Name
		{
			get { return "Local File"; } // TODO: Return something more usable to the user like, "Built-in" or whatever
		}
		
		/// <value>
		/// All the tasks including ITaskDivider items.
		/// </value>
		public ICollection<ITask> Tasks
		{
			get { return readOnlyTaskStore; }
		}
		
		/// <value>
		/// This returns all the task lists (categories) that exist.
		/// </value>
		public ICollection<ICategory> Categories
		{
			get { return readOnlyCategoryStore; }
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
		public ITask CreateTask (string taskName, ICategory category)		
		{
			// not sure what to do here with the category
			SqliteTask task = new SqliteTask (this, taskName);
			
			// Determine and set the task category
			if (category == null || category is Tasque.AllCategory)
				task.Category = defaultCategory; // Default to work
			else
				task.Category = category;
			
			AddTask (task);
			task.PropertyChanged += HandlePropertyChanged;
			
			return task;
		}
		
		public void DeleteTask(ITask task)
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
			// Add in the "All" Category
			//
			AllCategory allCategory = new Tasque.AllCategory (preferences);
			AddCategory (allCategory);

			RefreshCategories();
			RefreshTasks();		

			initialized = true;
			if(BackendInitialized != null) {
				BackendInitialized();
			}		
		}

		public void Dispose()
		{
			if (disposed)
				return;

			this.categoryListStore.Clear();
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
		public void RefreshCategories()
		{
			SqliteCategory newCategory;
			bool hasValues = false;
			
			string command = "SELECT id FROM Categories";
			SqliteCommand cmd = db.Connection.CreateCommand();
			cmd.CommandText = command;
			SqliteDataReader dataReader = cmd.ExecuteReader();
			while(dataReader.Read()) {
			    int id = dataReader.GetInt32(0);
				hasValues = true;
				
				newCategory = new SqliteCategory (this, id);
				if( (defaultCategory == null) || (newCategory.Name.CompareTo("Work") == 0) )
					defaultCategory = newCategory;
				AddCategory (newCategory);
			}
			
			dataReader.Close();
			cmd.Dispose();

			if(!hasValues)
			{
				defaultCategory = newCategory = new SqliteCategory (this, "Work");
				AddCategory (defaultCategory);

				newCategory = new SqliteCategory (this, "Personal");
				AddCategory (newCategory);
				
				newCategory = new SqliteCategory (this, "Family");
				AddCategory (newCategory);

				newCategory = new SqliteCategory (this, "Project");
				AddCategory (newCategory);
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
				int category = dataReader.GetInt32(1);
				string name = dataReader.GetString(2);
				long dueDate = dataReader.GetInt64(3);
				long completionDate = dataReader.GetInt64(4);
				int priority = dataReader.GetInt32(5);
				int state = dataReader.GetInt32(6);
				
				hasValues = true;
				
				newTask = new SqliteTask(this, id, category,
				                         name, dueDate, completionDate,
				                         priority, state);
				AddTask (newTask);
				newTask.PropertyChanged += HandlePropertyChanged;
			}
			
			dataReader.Close();
			cmd.Dispose();
			
			if(!hasValues)
			{
				newTask = new SqliteTask (this, "Create some tasks");
				newTask.Category = defaultCategory;
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

		void AddCategory (ICategory category)
		{
			var index = categoryListStore.Count;
			var valIdx = categoryListStore.Select ((val, idx) => new { val, idx })
				.FirstOrDefault (x => categoryComparer.Compare (x.val, category) > 0);
			if (valIdx != null)
				index = valIdx.idx;
			categoryListStore.Insert (index, category);
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
