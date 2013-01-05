// ITaskBackend.cs created with MonoDevelop
// User: boyd at 7:02 AM 2/11/2008

using System;
using System.Collections.Generic;

namespace Tasque.Backends
{
	public delegate void BackendInitializedHandler ();
	public delegate void BackendSyncStartedHandler ();
	public delegate void BackendSyncFinishedHandler ();
	
	/// <summary>
	/// This is the main integration interface for different backends that
	/// Tasque can use.
	/// </summary>
	public interface IBackend : IDisposable
	{
		event BackendInitializedHandler BackendInitialized;
		event BackendSyncStartedHandler BackendSyncStarted;
		event BackendSyncFinishedHandler BackendSyncFinished;

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
		ICollection<ITask> Tasks
		{
			get;
		}
		
		/// <value>
		/// This returns all the ICategory items from the backend.
		/// </value>
		ICollection<ICategory> Categories
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
		ITask CreateTask (string taskName, ICategory category);

		/// <summary>
		/// Deletes the specified task.
		/// </summary>
		/// <param name="task">
		/// A <see cref="ITask"/>
		/// </param>
		void DeleteTask (ITask task);
		
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
