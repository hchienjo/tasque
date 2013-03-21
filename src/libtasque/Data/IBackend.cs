// ITaskBackend.cs created with MonoDevelop
// User: boyd at 7:02 AM 2/11/2008

using System;
using System.Collections.Generic;

namespace Tasque.Backends
{
	/// <summary>
	/// This is the main integration interface for different backends that
	/// Tasque can use.
	/// </summary>
	public interface IBackend : IDisposable
	{
		event EventHandler BackendInitialized;
		event EventHandler BackendSyncStarted;
		event EventHandler BackendSyncFinished;

		#region Properties
		/// <value>
		/// A human-readable name for the backend that will be displayed in the
		/// preferences dialog to allow the user to select which backend Tasque
		/// should use.
		/// </value>
		string Name
		{
			get;
		}
		
		/// <value>
		/// All the tasks provided by the backend.
		/// </value>
		ICollection<Task> Tasks
		{
			get;
		}
		
		/// <value>
		/// This returns all the ITaskList items from the backend.
		/// </value>
		ICollection<TaskList> TaskLists
		{
			get;
		}
		
		/// <value>
		/// Indication that the backend has enough information
		/// (credentials/etc.) to run.  If false, the properties dialog will
		/// be shown so the user can configure the backend.
		/// </value>
		bool Configured
		{
			get;
		}
		
		/// <value>
		/// Inidication that the backend is initialized
		/// </value>
		bool Initialized
		{
			get;
		}
		
		/// <summary>
		/// An object that provides a means of managing backend specific preferences.
		/// </summary>
		/// <returns>
		/// A <see cref="Tasque.Backends.IBackendPreferences"/>
		/// </returns>
		IBackendPreferences Preferences { get; }
		#endregion // Properties
		
		#region Methods
		/// <summary>
		/// Create a new task.
		/// </summary>
		Task CreateTask (string taskName, TaskList list);

		/// <summary>
		/// Deletes the specified task.
		/// </summary>
		/// <param name="task">
		/// A <see cref="Task"/>
		/// </param>
		void DeleteTask (Task task);
		
		/// <summary>
		/// Refreshes the backend.
		/// </summary>
		void Refresh();
		
		/// <summary>
		/// Initializes the backend
		/// </summary>
		void Initialize (IPreferences preferences);
		#endregion // Methods
	}
}
