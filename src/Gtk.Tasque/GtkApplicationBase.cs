// Permission is hereby granted, free of charge, to any person obtaining 
// a copy of this software and associated documentation files (the 
// "Software"), to deal in the Software without restriction, including 
// without limitation the rights to use, copy, modify, merge, publish, 
// distribute, sublicense, and/or sell copies of the Software, and to 
// permit persons to whom the Software is furnished to do so, subject to 
// the following conditions: 
//  
// The above copyright notice and this permission notice shall be 
// included in all copies or substantial portions of the Software. 
//  
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF 
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE 
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION 
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 
// 
// Copyright (c) 2008 Novell, Inc. (http://www.novell.com) 
// Copyright (c) 2012 Antonius Riha
// 
// Authors: 
//      Sandy Armstrong <sanfordarmstrong@gmail.com>
//      Antonius Riha <antoniusriha@gmail.com>
// 
using System;
using System.Diagnostics;
using System.IO;
using Mono.Unix;
using Gtk;
#if ENABLE_NOTIFY_SHARP
using Notifications;
#endif

namespace Tasque
{
	public abstract class GtkApplicationBase : NativeApplication
	{
		protected GtkApplicationBase ()
		{
			confDir = Path.Combine (Environment.GetFolderPath (
				Environment.SpecialFolder.ApplicationData), "tasque");
			if (!Directory.Exists (confDir))
				Directory.CreateDirectory (confDir);
		}
		
		public override string ConfDir { get { return confDir; } }

		protected override void OnInitialize ()
		{
			Catalog.Init ("tasque", Defines.LocaleDir);
			Gtk.Application.Init ();
			
			// add package icon path to default icon theme search paths
			IconTheme.Default.PrependSearchPath (Defines.IconsDir);

			Gtk.Application.Init ();
			GLib.Idle.Add (delegate {
				InitializeIdle ();
				return false;
			});

			GLib.Timeout.Add (60000, delegate {
				CheckForDaySwitch ();
				return true;
			});

			Gtk.Application.Run ();
			
			base.OnInitialize ();
		}
		
		protected override void OnInitializeIdle ()
		{
			trayIcon = GtkTray.CreateTray (this);
			
			if (Backend == null) {
				// Pop open the preferences dialog so the user can choose a
				// backend service to use.
				ShowPreferences ();
			} else if (!QuietStart)
				TaskWindow.ShowWindow (this);
			
			if (Backend == null || !Backend.Configured) {
				GLib.Timeout.Add (1000, new GLib.TimeoutHandler (delegate {
					RetryBackend ();
					return Backend == null || !Backend.Configured;
				}));
			}
			
			base.OnInitializeIdle ();
		}

#if ENABLE_NOTIFY_SHARP
		public override void ShowAppNotification (string summary, string body)
		{
			var notification = new Notification (
				summary, body, Utilities.GetIcon ("tasque", 48));
			// TODO: Use this API for newer versions of notify-sharp
			//notification.AttachToStatusIcon (
			//		Tasque.Application.Instance.trayIcon);
			notification.Show ();
		}
#endif
		
		public override void ShowPreferences ()
		{
			Logger.Info ("OnPreferences called");
			if (preferencesDialog == null) {
				preferencesDialog = new PreferencesDialog (this);
				preferencesDialog.Hidden += OnPreferencesDialogHidden;
			}
			
			preferencesDialog.Present ();
		}
		
		void OnPreferencesDialogHidden (object sender, EventArgs args)
		{
			preferencesDialog.Destroy ();
			preferencesDialog.Hidden -= OnPreferencesDialogHidden;
			preferencesDialog = null;
		}
		
		protected override void OnBackendChanged ()
		{
			if (backendWasNullBeforeChange)
				TaskWindow.Reinitialize (!QuietStart, this);
			else
				TaskWindow.Reinitialize (true, this);
			
			Debug.WriteLine ("Configuration status: {0}", Backend.Configured.ToString ());
			
			RebuildTooltipTaskGroupModels ();
			if (trayIcon != null)
				trayIcon.RefreshTrayIconTooltip ();
			
			base.OnBackendChanged ();
		}
		
		protected override void OnBackendChanging ()
		{
			if (Backend != null)
				UnhookFromTooltipTaskGroupModels ();
			
			backendWasNullBeforeChange = Backend == null;
			
			base.OnBackendChanging ();
		}

		protected override void OnDaySwitched ()
		{
			// Reinitialize window according to new date
			if (TaskWindow.IsOpen)
				TaskWindow.Reinitialize (true, this);
			
			UnhookFromTooltipTaskGroupModels ();
			RebuildTooltipTaskGroupModels ();
			if (trayIcon != null)
				trayIcon.RefreshTrayIconTooltip ();
		}

		protected override void OnExit (int exitCode)
		{
			if (Backend != null)
				UnhookFromTooltipTaskGroupModels ();
			TaskWindow.SavePosition (Preferences);
			Application.Quit ();
			base.OnExit (exitCode);
		}
		
		protected override void ShowMainWindow ()
		{
			TaskWindow.ShowWindow (this);
		}
		
		protected override event EventHandler RemoteInstanceKnocked;
		
		protected void OnRemoteInstanceKnocked ()
		{
			if (RemoteInstanceKnocked != null)
				RemoteInstanceKnocked (this, EventArgs.Empty);
		}

		void OnTooltipModelChanged (object o, EventArgs args)
		{
			if (trayIcon != null)
				trayIcon.RefreshTrayIconTooltip ();
		}

		void RebuildTooltipTaskGroupModels ()
		{
			if (Backend == null || Backend.Tasks == null) {
				OverdueTasks = null;
				TodayTasks = null;
				TomorrowTasks = null;
				
				return;
			}
			
			OverdueTasks = TaskGroupModelFactory.CreateOverdueModel (Backend.Tasks, Preferences);
			TodayTasks = TaskGroupModelFactory.CreateTodayModel (Backend.Tasks, Preferences);
			TomorrowTasks = TaskGroupModelFactory.CreateTomorrowModel (Backend.Tasks, Preferences);
			
			foreach (TaskGroupModel model in new TaskGroupModel[] { OverdueTasks, TodayTasks, TomorrowTasks })
			{
				if (model == null) {
					continue;
				}

				model.CollectionChanged += OnTooltipModelChanged;
			}
		}

		void UnhookFromTooltipTaskGroupModels ()
		{
			foreach (TaskGroupModel model in new TaskGroupModel[] { OverdueTasks, TodayTasks, TomorrowTasks })
			{
				if (model == null) {
					continue;
				}
				
				model.CollectionChanged -= OnTooltipModelChanged;
			}
		}
		
		internal GtkTray TrayIcon { get { return trayIcon; } }
		
		bool backendWasNullBeforeChange;
		string confDir;
		PreferencesDialog preferencesDialog;
		GtkTray trayIcon;
	}
}
