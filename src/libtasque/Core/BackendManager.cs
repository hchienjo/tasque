//
// BackendManager.cs
//
// Author:
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Tasque.Data;

namespace Tasque.Core
{
	public class BackendManager : IDisposable
	{
		/// <summary>
		/// Initializes a new instance of the
		/// <see cref="Tasque.Core.BackendManager"/> class.
		/// </summary>
		/// <param name='preferences'>
		/// Preferences.
		/// </param>
		/// <exception cref="T:System.ArgumentNullException">
		/// thrown when preferences is <c>null</c>.
		/// </exception>
		public BackendManager (IPreferences preferences)
		{
			manager = new InternalBackendManager (preferences);
			
			// setup backend manager for AllList
			Tasque.Utils.AllList.SetBackendManager (this);
		}
		
		/// <summary>
		/// Gets the available backend ids and the corresponding
		/// human-readable names.
		/// </summary>
		/// <value>
		/// The available backends.
		/// </value>
		/// <exception cref="T:System.ObjectDisposedException">
		/// thrown when the object has been disposed.
		/// </exception>
		public IDictionary<string, string> AvailableBackends {
			get { return manager.AvailableBackends; }
		}

		/// <summary>
		/// Gets the id of the current backend.
		/// </summary>
		/// <value>
		/// The id of the current backend.
		/// </value>
		/// <exception cref="T:System.ObjectDisposedException">
		/// thrown when the object has been disposed.
		/// </exception>
		public string CurrentBackend { get { return manager.CurrentBackend; } }

		/// <summary>
		/// Gets a value indicating whether the current backend is initialized.
		/// </summary>
		/// <value>
		/// <c>true</c> if backend is initialized; otherwise, <c>false</c>.
		/// </value>
		public bool IsBackendInitialized {
			get { return manager.IsBackendInitialized; }
		}
		
		/// <summary>
		/// Gets a value indicating whether the current backend is configured.
		/// </summary>
		/// <value>
		/// <c>true</c> if the backend is configured; otherwise, <c>false</c>.
		/// </value>
		public bool IsBackendConfigured {
			get { return manager.IsBackendConfigured; }
		}
		
		/// <summary>
		/// Gets the task lists.
		/// </summary>
		/// <value>
		/// The task lists.
		/// </value>
		/// <exception cref="T:System.ObjectDisposedException">
		/// thrown when the object has been disposed.
		/// </exception>
		public ObservableCollection<ITaskList> TaskLists {
			get { return manager.TaskLists; }
		}

		/// <summary>
		/// Gets all tasks of the current backend.
		/// </summary>
		/// <value>
		/// The tasks.
		/// </value>
		/// <exception cref="T:System.ObjectDisposedException">
		/// thrown when the object has been disposed.
		/// </exception>
		public ReadOnlyObservableCollection<ITask> Tasks {
			get { return manager.Tasks; }
		}

		/// <summary>
		/// Sets the backend.
		/// </summary>
		/// <param name='backendType'>
		/// The backend id. This must be one of <see cref="AvailableBackends"/>
		/// 's ids or <c>null</c>.
		/// </param>
		/// <exception cref="T:System.ObjectDisposedException">
		/// thrown when the object has been disposed.
		/// </exception>
		/// <exception cref="T:System.ArgumentException">
		/// thrown when the provided backendType is not one of AvailableBackends.
		/// </exception>
		public void SetBackend (string id)
		{
			manager.SetBackend (id);
		}

		/// <summary>
		/// Reinitializes the current backend. This is a no-op, if
		/// <see cref="CurrentBackendType"/> is <c>null</c>.
		/// </summary>
		/// <exception cref="T:System.ObjectDisposedException">
		/// thrown when the object has been disposed.
		/// </exception>
		public void ReInitializeBackend ()
		{
			manager.ReInitializeBackend ();
		}

		/// <summary>
		/// Gets the backend preferences widget.
		/// </summary>
		/// <returns>
		/// The backend preferences widget.
		/// </returns>
		public IBackendPreferences GetBackendPreferencesWidget ()
		{
			return manager.GetBackendPreferencesWidget ();
		}

		/// <summary>
		/// Releases all resource used by the <see cref="Tasque.Core.BackendManager"/> object.
		/// </summary>
		/// <remarks>
		/// Call <see cref="Dispose"/> when you are finished using the <see cref="Tasque.Core.BackendManager"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="Tasque.Core.BackendManager"/> in an unusable state. After
		/// calling <see cref="Dispose"/>, you must release all references to the <see cref="Tasque.Core.BackendManager"/> so
		/// the garbage collector can reclaim the memory that the <see cref="Tasque.Core.BackendManager"/> was occupying.
		/// </remarks>
		public void Dispose ()
		{
			manager.Dispose ();
		}

		/// <summary>
		/// Occurs when the backend has been changed. This doesn't necessarily
		/// imply that the new backend has been initialized and is ready for
		/// use. Use <see cref="BackendInitialized"/> for that.
		/// </summary>
		public event EventHandler BackendChanged {
			add { manager.BackendChanged += value; }
			remove { manager.BackendChanged -= value; }
		}

		/// <summary>
		/// Occurs when the backend is changing.
		/// </summary>
		public event EventHandler BackendChanging {
			add { manager.BackendChanging += value; }
			remove { manager.BackendChanging -= value; }
		}

		/// <summary>
		/// Occurs when th backend has been initialized and is ready to use.
		/// </summary>
		public event EventHandler BackendInitialized {
			add { manager.BackendInitialized += value; }
			remove { manager.BackendInitialized -= value; }
		}

		/// <summary>
		/// Occurs when the current backend needs configuration.
		/// </summary>
		public event EventHandler BackendConfigurationRequested {
			add { manager.BackendConfigurationRequested += value; }
			remove { manager.BackendConfigurationRequested -= value; }
		}
		
		InternalBackendManager manager;
	}
}
