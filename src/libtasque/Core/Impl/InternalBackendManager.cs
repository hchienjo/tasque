//
// InternalBackendManager.cs
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
using System.Linq;
using Mono.Addins;
using Tasque.Data;

namespace Tasque.Core.Impl
{
	using BackendNode = TypeExtensionNode<BackendExtensionAttribute>;

	public partial class InternalBackendManager
	{
		public InternalBackendManager (IPreferences preferences)
		{
			if (preferences == null)
				throw new ArgumentNullException ("preferences");
			this.preferences = preferences;
			
			availableBackendNodes = AddinManager
				.GetExtensionNodes<BackendNode> (typeof(IBackend));

			taskLists = new TaskListCollection ();
		}

		public IDictionary<string, string> AvailableBackends {
			get {
				ThrowIfDisposed ();
				return availableBackendNodes.ToDictionary (
					n => n.Id, n => n.Data.Name);
			}
		}

		public string CurrentBackend {
			get {
				ThrowIfDisposed ();
				return currentBackend;
			}
		}
		
		public bool IsBackendInitialized {
			get {
				ThrowIfDisposed ();
				return backend != null ? backend.IsInitialized : false;
			}
		}
		
		public bool IsBackendConfigured {
			get {
				ThrowIfDisposed ();
				return backend != null ? backend.IsConfigured : false;
			}
		}

		public ObservableCollection<ITaskList> TaskLists {
			get {
				ThrowIfDisposed ();
				return taskLists;
			}
		}

		public ReadOnlyObservableCollection<ITask> Tasks {
			get {
				ThrowIfDisposed ();
				return taskLists.Tasks;
			}
		}

		public void SetBackend (string id)
		{
			ThrowIfDisposed ();
			if (id != null && !AvailableBackends.ContainsKey (id))
				throw new ArgumentException ("The provided backend type is" +
					" not listed in AvailableBackends.", "id");

			if (currentBackend == id)
				return;

			if (BackendChanging != null)
				BackendChanging (this, EventArgs.Empty);

			currentBackend = id;
			ReInitializeBackend ();

			if (BackendChanged != null)
				BackendChanged (this, EventArgs.Empty);
		}

		public void ReInitializeBackend ()
		{
			ThrowIfDisposed ();
			if (currentBackend == null)
				return;

			if (backend != null) {
				Logger.Debug ("Cleaning up backend: {0}", backend.GetType ());
				backend.Disposed += delegate {
					backend = null;
					InitializeBackend ();
				};
				backend.Dispose ();
				taskLists.UnloadTaskLists ();
			} else
				InitializeBackend ();
		}
		
		public IBackendPreferences GetBackendPreferencesWidget ()
		{
			ThrowIfDisposed ();
			return backend.Preferences;
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (disposed)
				return;
			disposed = true;

			if (disposing) {
				if (backend != null)
					backend.Dispose ();
				taskLists = null;
				backend = null;
			}
		}

		public event EventHandler BackendChanged;
		public event EventHandler BackendChanging;
		public event EventHandler BackendInitialized;
		public event EventHandler BackendConfigurationRequested;

		void InitializeBackend ()
		{
			var node = availableBackendNodes.Single (
				n => n.Id == currentBackend);
			Logger.Info ("Using backend: {0} ({1})",
			             node.Data.Name, currentBackend);
			backend = (IBackend)node.CreateInstance ();
			backend.NeedsConfiguration += delegate {
				if (BackendConfigurationRequested != null)
					BackendConfigurationRequested (this, EventArgs.Empty);
			};
			backend.Initialized += delegate {
				// load data
				taskLists.LoadTaskLists (backend);
				if (BackendInitialized != null)
					BackendInitialized (this, EventArgs.Empty);
			};
			
			try {
				backend.Initialize (preferences);
			} catch (Exception ex) {
				backend.Dispose ();
				backend = null;
				throw ex;
			}
		}

		void ThrowIfDisposed ()
		{
			if (disposed)
				throw new ObjectDisposedException ("BackendManager");
		}

		ExtensionNodeList<BackendNode> availableBackendNodes;
		IBackend backend;
		string currentBackend;
		IPreferences preferences;
		TaskListCollection taskLists;
		bool disposed;
	}
}
