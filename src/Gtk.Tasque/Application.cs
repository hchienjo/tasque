/***************************************************************************
 *  Application.cs
 *
 *  Copyright (C) 2008 Novell, Inc.
 *  Written by Calvin Gaisford <calvinrg@gmail.com>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml;
using System.Net.Sockets;

using Gtk;
using Gdk;
using Mono.Unix;
using Mono.Unix.Native;
#if ENABLE_NOTIFY_SHARP
using Notifications;
#endif
using Tasque.Backends;

namespace Tasque
{
	public class Application
	{
		private static Tasque.Application application = null;
		private static System.Object locker = new System.Object();
		private bool initialized;
		private INativeApplication nativeApp;

		private Gdk.Pixbuf normalPixBuf;
		private Gtk.Image trayImage;
		private GtkTray trayIcon;
		private Preferences preferences;
		private EventBox eb;
		private IBackend backend;
		private PreferencesDialog preferencesDialog;
		private bool quietStart = false;
		
		private DateTime currentDay = DateTime.Today;
		
		/// <value>
		/// Keep track of the available backends.  The key is the Type name of
		/// the backend.
		/// </value>
		private Dictionary<string, IBackend> availableBackends;
		
		private IBackend customBackend;

		public static IBackend Backend
		{ 
			get { return Application.Instance.backend; }
			set { Application.Instance.SetBackend (value); }
		}
		
		public static List<IBackend> AvailableBackends
		{
			get {
				return new List<IBackend> (Application.Instance.availableBackends.Values);
			}
//			get { return Application.Instance.availableBackends; }
		}
		
		public static Application Instance
		{
			get {
				lock(locker) {
					return application;
				}
			}
		}

		public INativeApplication NativeApplication
		{
			get
			{
				return nativeApp;
			}
		}

		public event EventHandler BackendChanged;

		void OnBackendChanged ()
		{
			if (BackendChanged != null)
				BackendChanged (this, EventArgs.Empty);
		}
		
		public TaskGroupModel OverdueTasks { get; private set; }
		
		public TaskGroupModel TodayTasks { get; private set; }
		
		public TaskGroupModel TomorrowTasks { get; private set; }
		
		public static Preferences Preferences
		{
			get { return Application.Instance.preferences; }
		}

		public Application (INativeApplication nativeApp)
		{
			if (nativeApp == null)
				throw new ArgumentNullException ("nativeApp");
			this.nativeApp = nativeApp;
			application = this;
		}

		public void Init (string[] args)
		{
			lock (locker) {
				if (initialized)
					return;
				initialized = true;
			}

			nativeApp.Initialize (args);
			
			preferences = new Preferences (nativeApp.ConfDir);
			
			string potentialBackendClassName = null;
			
			// See if a specific backend is specified
			if (potentialBackendClassName != null) {
				Logger.Debug ("Backend specified: " +
				              potentialBackendClassName);
				
				customBackend = null;
				Assembly asm = Assembly.GetCallingAssembly ();
				try {
					customBackend = (IBackend)
						asm.CreateInstance (potentialBackendClassName);
				} catch (Exception e) {
					Logger.Warn ("Backend specified on args not found: {0}\n\t{1}",
						potentialBackendClassName, e.Message);
				}
			}
			
			// Discover all available backends
			LoadAvailableBackends ();

			GLib.Idle.Add(InitializeIdle);
			GLib.Timeout.Add (60000, CheckForDaySwitch);
		}
		
		/// <summary>
		/// Load all the available backends that Tasque can find.  First look in
		/// Tasque.exe and then for other DLLs in the same directory Tasque.ex
		/// resides.
		/// </summary>
		private void LoadAvailableBackends ()
		{
			availableBackends = new Dictionary<string,IBackend> ();
			
			List<IBackend> backends = new List<IBackend> ();
			
			Assembly tasqueAssembly = Assembly.GetCallingAssembly ();
			
			// Look for other backends in Tasque.exe
			backends.AddRange (GetBackendsFromAssembly (tasqueAssembly));
			
			// Look through the assemblies located in the same directory as
			// Tasque.exe.
			Logger.Debug ("Tasque.exe location:  {0}", tasqueAssembly.Location);
			
			DirectoryInfo loadPathInfo =
				Directory.GetParent (tasqueAssembly.Location);
			Logger.Info ("Searching for Backend DLLs in: {0}", loadPathInfo.FullName);
			
			foreach (FileInfo fileInfo in loadPathInfo.GetFiles ("*.dll")) {
				Logger.Info ("\tReading {0}", fileInfo.FullName);
				Assembly asm = null;
				try {
					asm = Assembly.LoadFile (fileInfo.FullName);
				} catch (Exception e) {
					Logger.Debug ("Exception loading {0}: {1}",
								  fileInfo.FullName,
								  e.Message);
					continue;
				}
				
				backends.AddRange (GetBackendsFromAssembly (asm));
			}
			
			foreach (IBackend backend in backends) {
				string typeId = backend.GetType ().ToString ();
				if (availableBackends.ContainsKey (typeId))
					continue;
				
				Logger.Debug ("Storing '{0}' = '{1}'", typeId, backend.Name);
				availableBackends [typeId] = backend;
			}
		}
		
		private List<IBackend> GetBackendsFromAssembly (Assembly asm)
		{
			List<IBackend> backends = new List<IBackend> ();
			
			Type[] types = null;
			
			try {
				types = asm.GetTypes ();
			} catch (Exception e) {
				Logger.Warn ("Exception reading types from assembly '{0}': {1}",
					asm.ToString (), e.Message);
				return backends;
			}
			foreach (Type type in types) {
				if (!type.IsClass) {
					continue; // Skip non-class types
				}
				if (type.GetInterface ("Tasque.Backends.IBackend") == null) {
					continue;
				}
				Logger.Debug ("Found Available Backend: {0}", type.ToString ());
				
				IBackend availableBackend = null;
				try {
					availableBackend = (IBackend)
						asm.CreateInstance (type.ToString ());
				} catch (Exception e) {
					Logger.Warn ("Could not instantiate {0}: {1}",
								 type.ToString (),
								 e.Message);
					continue;
				}
				
				if (availableBackend != null) {
					backends.Add (availableBackend);
				}
			}
			
			return backends;
		}

		private void SetBackend (IBackend value)
		{
			bool changingBackend = false;
			if (this.backend != null) {
				UnhookFromTooltipTaskGroupModels ();
				changingBackend = true;
				// Cleanup the old backend
				try {
					Logger.Debug ("Cleaning up backend: {0}",
					              this.backend.Name);
					this.backend.Cleanup ();
				} catch (Exception e) {
					Logger.Warn ("Exception cleaning up '{0}': {1}",
					             this.backend.Name,
					             e);
				}
			}
				
			// Initialize the new backend
			var oldBackend = backend;
			this.backend = value;
			if (this.backend == null) {
				if (trayIcon != null)
					trayIcon.RefreshTrayIconTooltip ();
				return;
			}
				
			Logger.Info ("Using backend: {0} ({1})",
			             this.backend.Name,
			             this.backend.GetType ().ToString ());
			this.backend.Initialize();
			
			if (!changingBackend) {
				TaskWindow.Reinitialize (!this.quietStart);
			} else {
				TaskWindow.Reinitialize (true);
			}

			RebuildTooltipTaskGroupModels ();
			if (trayIcon != null)
				trayIcon.RefreshTrayIconTooltip ();
			
			Logger.Debug("Configuration status: {0}",
			             this.backend.Configured.ToString());

			if (backend != oldBackend)
				OnBackendChanged ();
		}

		private bool InitializeIdle()
		{
			if (customBackend != null) {
				Application.Backend = customBackend;
			} else {
				// Check to see if the user has a preference of which backend
				// to use.  If so, use it, otherwise, pop open the preferences
				// dialog so they can choose one.
				string backendTypeString = Preferences.Get (Preferences.CurrentBackend);
				Logger.Debug ("CurrentBackend specified in Preferences: {0}", backendTypeString);
				if (backendTypeString != null
						&& availableBackends.ContainsKey (backendTypeString)) {
					Application.Backend = availableBackends [backendTypeString];
				}
			}
			
			trayIcon = GtkTray.CreateTray ();
			
			if (backend == null) {
				// Pop open the preferences dialog so the user can choose a
				// backend service to use.
				Application.ShowPreferences ();
			} else if (!quietStart) {
				TaskWindow.ShowWindow ();
			}
			if (backend == null || !backend.Configured){
				GLib.Timeout.Add(1000, new GLib.TimeoutHandler(RetryBackend));
			}

			nativeApp.InitializeIdle ();
			
			return false;
		}
		private bool RetryBackend(){
			try {
				if (backend != null && !backend.Configured) {
					backend.Cleanup();
					backend.Initialize();
				}
			} catch (Exception e) {
				Logger.Error("{0}", e.Message);
			}
			if (backend == null || !backend.Configured) {
				return true;
			} else {
				return false;
			}
		}
		
		private bool CheckForDaySwitch ()
		{
			if (DateTime.Today != currentDay) {
				Logger.Debug ("Day has changed, reloading tasks");
				currentDay = DateTime.Today;
				// Reinitialize window according to new date
				if (TaskWindow.IsOpen)
					TaskWindow.Reinitialize (true);
				
				UnhookFromTooltipTaskGroupModels ();
				RebuildTooltipTaskGroupModels ();
				if (trayIcon != null)
					trayIcon.RefreshTrayIconTooltip ();
			}
			
			return true;
		}

		private void UnhookFromTooltipTaskGroupModels ()
		{
			foreach (TaskGroupModel model in new TaskGroupModel[] { OverdueTasks, TodayTasks, TomorrowTasks })
			{
				if (model == null) {
					continue;
				}
				
				model.RowInserted -= OnTooltipModelChanged;
				model.RowChanged -= OnTooltipModelChanged;
				model.RowDeleted -= OnTooltipModelChanged;
			}
		}

		private void OnTooltipModelChanged (object o, EventArgs args)
		{
			if (trayIcon != null)
				trayIcon.RefreshTrayIconTooltip ();
		}

		private void RebuildTooltipTaskGroupModels ()
		{
			if (backend == null || backend.Tasks == null) {
				OverdueTasks = null;
				TodayTasks = null;
				TomorrowTasks = null;
				
				return;
			}

			OverdueTasks = TaskGroupModelFactory.CreateOverdueModel (backend.Tasks);
			TodayTasks = TaskGroupModelFactory.CreateTodayModel (backend.Tasks);
			TomorrowTasks = TaskGroupModelFactory.CreateTomorrowModel (backend.Tasks);

			foreach (TaskGroupModel model in new TaskGroupModel[] { OverdueTasks, TodayTasks, TomorrowTasks })
			{
				if (model == null) {
					continue;
				}
				
				model.RowInserted += OnTooltipModelChanged;
				model.RowChanged += OnTooltipModelChanged;
				model.RowDeleted += OnTooltipModelChanged;
			}
		}
		
		private void OnPreferencesDialogHidden (object sender, EventArgs args)
		{
			preferencesDialog.Destroy ();
			preferencesDialog = null;
		}
		
		public static void ShowPreferences ()
		{
			Logger.Info ("ShowPreferences called");
			var app = application;
			if (app.preferencesDialog == null) {
				app.preferencesDialog = new PreferencesDialog ();
				app.preferencesDialog.Hidden += app.OnPreferencesDialogHidden;
			}
			
			app.preferencesDialog.Present ();
		}

#if ENABLE_NOTIFY_SHARP
		public static void ShowAppNotification(Notification notification)
		{
			// TODO: Use this API for newer versions of notify-sharp
			//notification.AttachToStatusIcon(
			//		Tasque.Application.Instance.trayIcon);
			notification.Show();
		}
#endif

		public void StartMainLoop ()
		{
			nativeApp.StartMainLoop ();
		}

		public void Quit ()
		{
			Logger.Info ("Quit called - terminating application");
			if (backend != null) {
				UnhookFromTooltipTaskGroupModels ();
				backend.Cleanup ();
			}
			TaskWindow.SavePosition ();

			nativeApp.QuitMainLoop ();
		}

		public void Exit (int exitCode)
		{
			if (nativeApp != null)
				nativeApp.Exit (exitCode);
			else
				Environment.Exit (exitCode);
		}
	}
}
