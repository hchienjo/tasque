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
		private EventBox eb;
		private PreferencesDialog preferencesDialog;
		private bool quietStart = false;
		
		private DateTime currentDay = DateTime.Today;

		public static IBackend Backend
		{ 
			get { return Application.Instance.nativeApp.Backend; }
			set { Application.Instance.nativeApp.Backend = value; }
		}
		
		public static IList<IBackend> AvailableBackends
		{
			get {
				return Application.Instance.nativeApp.AvailableBackends;
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
			get { return Application.Instance.nativeApp.Preferences; }
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

			GLib.Timeout.Add (60000, CheckForDaySwitch);
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
				var gtkApp = (GtkApplicationBase)nativeApp;
				if (gtkApp.TrayIcon != null)
					gtkApp.TrayIcon.RefreshTrayIconTooltip ();
			}
			
			return true;
		}

		public void UnhookFromTooltipTaskGroupModels ()
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
			var gtkApp = (GtkApplicationBase)nativeApp;
			if (gtkApp.TrayIcon != null)
				gtkApp.TrayIcon.RefreshTrayIconTooltip ();
		}

		public void RebuildTooltipTaskGroupModels ()
		{
			if (nativeApp.Backend == null || nativeApp.Backend.Tasks == null) {
				OverdueTasks = null;
				TodayTasks = null;
				TomorrowTasks = null;
				
				return;
			}

			OverdueTasks = TaskGroupModelFactory.CreateOverdueModel (nativeApp.Backend.Tasks);
			TodayTasks = TaskGroupModelFactory.CreateTodayModel (nativeApp.Backend.Tasks);
			TomorrowTasks = TaskGroupModelFactory.CreateTomorrowModel (nativeApp.Backend.Tasks);

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
			if (nativeApp.Backend != null) {
				UnhookFromTooltipTaskGroupModels ();
				nativeApp.Backend.Cleanup ();
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
