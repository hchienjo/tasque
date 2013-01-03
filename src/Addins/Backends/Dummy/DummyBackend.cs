// DummyBackend.cs created with MonoDevelop
// User: boyd at 7:10 AM 2/11/2008

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Mono.Unix;
using Tasque.Backends;
using Gtk.Tasque.Backends.Dummy;
using System.ComponentModel;

namespace Tasque.Backends.Dummy
{
	public class DummyBackend : IBackend
	{
		private int newTaskId;
		private bool initialized;
		private bool configured = true;
		
		ObservableCollection<ITask> taskStore;
		ObservableCollection<ICategory> categoryListStore;
		ReadOnlyObservableCollection<ITask> readOnlyTaskStore;
		ReadOnlyObservableCollection<ICategory> readOnlyCategoryStore;
		
		TaskComparer taskComparer;
		CategoryComparer categoryComparer;
		
		public event BackendInitializedHandler BackendInitialized;
		public event BackendSyncStartedHandler BackendSyncStarted;
		public event BackendSyncFinishedHandler BackendSyncFinished;
		
		DummyCategory homeCategory;
		DummyCategory workCategory;
		DummyCategory projectsCategory;
		
		public DummyBackend ()
		{
			initialized = false;
			newTaskId = 0;
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
			get { return "Debugging System"; }
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
		/// Indication that the dummy backend is configured
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
		#endregion // Public Properties
		
		#region Public Methodsopen source contributors badges
		public ITask CreateTask (string taskName, ICategory category)		
		{
			// not sure what to do here with the category
			DummyTask task = new DummyTask (this, newTaskId, taskName);
			
			// Determine and set the task category
			if (category == null || category is Tasque.AllCategory)
				task.Category = workCategory; // Default to work
			else
				task.Category = category;
			
			AddTask (task);
			newTaskId++;
			
			task.PropertyChanged += HandlePropertyChanged;
			
			return task;
		}
		
		public void DeleteTask(ITask task)
		{}
		
		public void Refresh()
		{}

		public void Initialize (IPreferences preferences)
		{
			if (preferences == null)
				throw new ArgumentNullException ("preferences");

			//
			// Add in the "All" Category
			//
			AddCategory (new AllCategory (preferences));
			
			//
			// Add in some fake categories
			//
			homeCategory = new DummyCategory ("Home");
			AddCategory (homeCategory);
			
			workCategory = new DummyCategory ("Work");
			AddCategory (workCategory);
			
			projectsCategory = new DummyCategory ("Projects");
			AddCategory (projectsCategory);
			
			//
			// Add in some fake tasks
			//
			
			DummyTask task = new DummyTask (this, newTaskId, "Buy some nails");
			task.Category = projectsCategory;
			task.DueDate = DateTime.Now.AddDays (1);
			task.Priority = TaskPriority.Medium;
			AddTask (task);
			task.PropertyChanged += HandlePropertyChanged;
			newTaskId++;
			
			task = new DummyTask (this, newTaskId, "Call Roger");
			task.Category = homeCategory;
			task.DueDate = DateTime.Now.AddDays (-1);
			task.Complete ();
			task.CompletionDate = task.DueDate;
			AddTask (task);
			task.PropertyChanged += HandlePropertyChanged;
			newTaskId++;
			
			task = new DummyTask (this, newTaskId, "Replace burnt out lightbulb");
			task.Category = homeCategory;
			task.DueDate = DateTime.Now;
			task.Priority = TaskPriority.Low;
			AddTask (task);
			task.PropertyChanged += HandlePropertyChanged;
			newTaskId++;
			
			task = new DummyTask (this, newTaskId, "File taxes");
			task.Category = homeCategory;
			task.DueDate = new DateTime (2008, 4, 1);
			AddTask (task);
			task.PropertyChanged += HandlePropertyChanged;
			newTaskId++;
			
			task = new DummyTask (this, newTaskId, "Purchase lumber");
			task.Category = projectsCategory;
			task.DueDate = DateTime.Now.AddDays (1);
			task.Priority = TaskPriority.High;
			AddTask (task);
			task.PropertyChanged += HandlePropertyChanged;
			newTaskId++;
						
			task = new DummyTask (this, newTaskId, "Estimate drywall requirements");
			task.Category = projectsCategory;
			task.DueDate = DateTime.Now.AddDays (1);
			task.Priority = TaskPriority.Low;
			AddTask (task);
			task.PropertyChanged += HandlePropertyChanged;
			newTaskId++;
			
			task = new DummyTask (this, newTaskId, "Borrow framing nailer from Ben");
			task.Category = projectsCategory;
			task.DueDate = DateTime.Now.AddDays (1);
			task.Priority = TaskPriority.High;
			AddTask (task);
			task.PropertyChanged += HandlePropertyChanged;
			newTaskId++;
			
			task = new DummyTask (this, newTaskId, "Call for an insulation estimate");
			task.Category = projectsCategory;
			task.DueDate = DateTime.Now.AddDays (1);
			task.Priority = TaskPriority.Medium;
			AddTask (task);
			task.PropertyChanged += HandlePropertyChanged;
			newTaskId++;
			
			task = new DummyTask (this, newTaskId, "Pay storage rental fee");
			task.Category = homeCategory;
			task.DueDate = DateTime.Now.AddDays (1);
			task.Priority = TaskPriority.None;
			AddTask (task);
			task.PropertyChanged += HandlePropertyChanged;
			newTaskId++;
			
			task = new DummyTask (this, newTaskId, "Place carpet order");
			task.Category = projectsCategory;
			task.Priority = TaskPriority.None;
			AddTask (task);
			task.PropertyChanged += HandlePropertyChanged;
			newTaskId++;
			
			task = new DummyTask (this, newTaskId, "Test task overdue");
			task.Category = workCategory;
			task.DueDate = DateTime.Now.AddDays (-89);
			task.Priority = TaskPriority.None;
			task.Complete ();
			AddTask (task);
			task.PropertyChanged += HandlePropertyChanged;
			newTaskId++;
			
			initialized = true;
			if(BackendInitialized != null) {
				BackendInitialized();
			}		
		}

		public void Cleanup()
		{}
		
		public IBackendPreferences Preferences
		{
			get {
				// TODO: Replace this with returning null once things are going
				// so that the Preferences Dialog doesn't waste space.
				return new DummyPreferences ();
			}
		}
		#endregion // Public Methods
		
		#region Private Methods
		internal void DeleteTask (DummyTask task)
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
		
		void AddTask (DummyTask task)
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
			var task = (DummyTask)sender;
			if (taskStore.Remove (task))
				AddTask (task);
		}
		#endregion // Private Methods
		
		#region Event Handlers
		#endregion // Event Handlers
	}
}
