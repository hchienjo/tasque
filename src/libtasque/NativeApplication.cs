// 
// NativeApplication.cs
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
using System.Diagnostics;
using Mono.Options;
using System.Reflection;
using System.Collections.Generic;
using Tasque.Backends;
using System.IO;

namespace Tasque
{
	public abstract class NativeApplication : INativeApplication
	{
		public abstract string ConfDir { get; }
		
		public Preferences Preferences { get { return preferences; } }

		public void Exit (int exitcode)
		{
			OnExit (exitcode);

			if (Exiting != null)
				Exiting (this, EventArgs.Empty);

			Environment.Exit (exitcode);
		}

		public virtual void Initialize (string[] args)
		{
			if (IsRemoteInstanceRunning ()) {
				Logger.Info ("Another instance of Tasque is already running.");
				Exit (0);
			}
			
			RemoteInstanceKnocked += delegate { ShowMainWindow (); };
			
			preferences = new Preferences (ConfDir);
			
			ParseArgs (args);
			
			SetCustomBackend ();
			
			// Discover all available backends
			LoadAvailableBackends ();
			
			OnInitialize ();
		}

		protected virtual void OnInitialize () {}

		protected void InitializeIdle ()
		{
			if (customBackend != null) {
				Backend = customBackend;
			} else {
				// Check to see if the user has a preference of which backend
				// to use.  If so, use it, otherwise, pop open the preferences
				// dialog so they can choose one.
				var backendTypeString = preferences.Get (Preferences.CurrentBackend);
				Logger.Debug ("CurrentBackend specified in Preferences: {0}", backendTypeString);
				if (backendTypeString != null && availableBackends.ContainsKey (backendTypeString))
					Backend = availableBackends [backendTypeString];
			}
			
			OnInitializeIdle ();
		}
		
		protected virtual void OnInitializeIdle () {}

		protected virtual void OnExit (int exitCode) {}

		public virtual void OpenUrl (string url)
		{
			Process.Start (url);
		}
		
		#region Backend
		public IList<IBackend> AvailableBackends {
			get { return new List<IBackend> (availableBackends.Values); }
		}
		
		public IBackend Backend { get { return backend; } set { SetBackend (value); } }
		
		public event EventHandler BackendChanged;
		
		protected virtual void OnBackendChanged ()
		{
			if (BackendChanged != null)
				BackendChanged (this, EventArgs.Empty);
		}
		
		protected virtual void OnBackendChanging () {}
		
		void SetBackend (IBackend value)
		{
			if (value == backend)
				return;
			
			OnBackendChanging ();
			
			if (backend != null) {
				// Cleanup the old backend
				try {
					Logger.Debug ("Cleaning up backend: {0}", backend.Name);
					backend.Cleanup ();
				} catch (Exception e) {
					Logger.Warn ("Exception cleaning up '{0}': {1}", backend.Name, e);
				}
			}
			
			// Initialize the new backend
			backend = value;
			if (backend == null)
				return;
			
			Logger.Info ("Using backend: {0} ({1})", backend.Name, backend.GetType ().ToString ());
			backend.Initialize ();
			
			OnBackendChanged ();
		}
		#endregion

		public abstract void QuitMainLoop ();

		public abstract void StartMainLoop ();

		public event EventHandler Exiting;

		/// <summary>
		/// Determines whether this a remote instance is running.
		/// </summary>
		/// <returns>
		/// <c>true</c> if a remote instance is running; otherwise, <c>false</c>.
		/// </returns>
		protected abstract bool IsRemoteInstanceRunning ();

		protected abstract void ShowMainWindow ();
		
		protected abstract event EventHandler RemoteInstanceKnocked;
		
		#region IDisposable implementation
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}
		
		protected virtual void Dispose (bool disposing) {}
		
		~NativeApplication ()
		{
			Dispose (false);
		}
		#endregion
		
		#region Backend loading and helpers
		protected void RetryBackend ()
		{
			try {
				if (backend != null && !backend.Configured) {
					backend.Cleanup ();
					backend.Initialize();
				}
			} catch (Exception e) {
				Logger.Error ("{0}", e.Message);
			}
		}
		
		/// <summary>
		/// Load all the available backends that Tasque can find.  First look in
		/// Tasque.exe and then for other DLLs in the same directory Tasque.ex
		/// resides.
		/// </summary>
		void LoadAvailableBackends ()
		{
			availableBackends = new Dictionary<string, IBackend> ();
			var backends = new List<IBackend> ();
			var tasqueAssembly = Assembly.GetCallingAssembly ();
			
			// Look for other backends in Tasque.exe
			backends.AddRange (GetBackendsFromAssembly (tasqueAssembly));
			
			// Look through the assemblies located in the same directory as Tasque.exe.
			Logger.Debug("Tasque.exe location: " + tasqueAssembly.Location);
			
			var loadPathInfo = Directory.GetParent (tasqueAssembly.Location);
			Logger.Info ("Searching for Backend DLLs in: " + loadPathInfo.FullName);
			
			foreach (var fileInfo in loadPathInfo.GetFiles ("*.dll")) {
				Logger.Info ("\tReading " + fileInfo.FullName);
				Assembly asm = null;
				try {
					asm = Assembly.LoadFile (fileInfo.FullName);
				} catch (Exception e) {
					Logger.Debug("Exception loading {0}: {1}", fileInfo.FullName, e.Message);
					continue;
				}
				
				backends.AddRange (GetBackendsFromAssembly (asm));
			}
			
			foreach (var backend in backends) {
				string typeId = backend.GetType ().ToString ();
				if (availableBackends.ContainsKey (typeId))
					continue;
				
				Logger.Debug("Storing '{0}' = '{1}'", typeId, backend.Name);
				availableBackends [typeId] = backend;
			}
		}
		
		List<IBackend> GetBackendsFromAssembly (Assembly asm)
		{
			var backends = new List<IBackend> ();
			Type[] types = null;
			
			try {
				types = asm.GetTypes ();
			} catch (Exception e) {
				Logger.Warn ("Exception reading types from assembly '{0}': {1}", asm.ToString (), e.Message);
				return backends;
			}
			
			foreach (var type in types) {
				if (!type.IsClass)
					continue; // Skip non-class types
				
				if (!typeof(IBackend).IsAssignableFrom (type))
					continue;
				
				Logger.Debug("Found Available Backend: {0}", type);
				
				IBackend availableBackend = null;
				try {
					availableBackend = (IBackend)asm.CreateInstance (type.ToString ());
				} catch (Exception e) {
					Logger.Warn ("Could not instantiate {0}: {1}", type.ToString (), e.Message);
					continue;
				}
				
				if (availableBackend != null)
					backends.Add (availableBackend);
			}
			
			return backends;
		}
		
		void SetCustomBackend ()
		{
			// See if a specific backend is specified
			if (potentialBackendClassName != null) {
				Logger.Debug("Backend specified: " + potentialBackendClassName);
				
				Assembly asm = Assembly.GetCallingAssembly ();
				try {
					customBackend = (IBackend)asm.CreateInstance (potentialBackendClassName);
				} catch (Exception e) {
					Logger.Warn ("Backend specified on args not found: {0}\n\t{1}",
					                    potentialBackendClassName, e.Message);
				}
			}
		}
		#endregion
		
		protected bool QuietStart { get; private set; }
		
		void ParseArgs (string[] args)
		{
			bool showHelp = false;
			var p = new OptionSet () {
				{ "q|quiet", "hide the Tasque window upon start.", v => QuietStart = true },
				{ "b|backend=", "the name of the {BACKEND} to use.", v => potentialBackendClassName = v },
				{ "h|help",  "show this message and exit.", v => showHelp = v != null },
			};
			
			try {
				p.Parse (args);
			} catch (OptionException e) {
				Console.Write ("tasque: ");
				Console.WriteLine (e.Message);
				Console.WriteLine ("Try `tasque --help' for more information.");
				Exit (-1);
			}
			
			if (showHelp) {
				Console.WriteLine ("Usage: tasque [[-q|--quiet] [[-b|--backend] BACKEND]]");
				Console.WriteLine ();
				Console.WriteLine ("Options:");
				p.WriteOptionDescriptions (Console.Out);
				Exit (-1);
			}
		}
		
		/// <value>
		/// Keep track of the available backends.  The key is the Type name of
		/// the backend.
		/// </value>
		Dictionary<string, IBackend> availableBackends;
		IBackend backend;
		IBackend customBackend;
		string potentialBackendClassName;
		Preferences preferences;
	}
}
