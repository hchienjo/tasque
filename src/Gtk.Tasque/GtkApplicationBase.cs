// 
// GtkApplicationBase.cs
// 
// Authors:
//       Sandy Armstrong <sanfordarmstrong@gmail.com>
//       Antonius Riha <antoniusriha@gmail.com>
// 
// Copyright (c) 2008 Novell, Inc. (http://www.novell.com) 
// Copyright (c) 2012-2013 Antonius Riha
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
using System.IO;
using System.Linq;
using Mono.Addins;
using Mono.Options;
using Mono.Unix;
using Tasque;
using Tasque.Core;
using Tasque.Utils;
using Gtk;
using System.Collections.Generic;

#if ENABLE_NOTIFY_SHARP
using Notifications;
#endif

namespace Gtk.Tasque
{
	public abstract class GtkApplicationBase : IDisposable
	{
		protected GtkApplicationBase (string[] args)
		{
			AddinManager.Initialize ();
			AddinManager.Registry.Update ();

			Catalog.Init ("tasque", Defines.LocaleDir);
			
			ConfDir = Path.Combine (Environment.GetFolderPath (
				Environment.SpecialFolder.ApplicationData), "tasque");
			if (!Directory.Exists (ConfDir))
				Directory.CreateDirectory (ConfDir);

			if (IsRemoteInstanceRunning ()) {
				Logger.Info ("Another instance of Tasque is already running.");
				Exit (0);
			}
			
			RemoteInstanceKnocked += delegate {
				TaskWindow.ShowWindow (this);
			};
			
			preferences = new Preferences (ConfDir);
			
			ParseArgs (args);
			
			backendManager = new BackendManager (preferences);
			backendManager.BackendConfigurationRequested += delegate {
				ShowPreferences ();
			};
			
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
		}
		
		public BackendManager BackendManager { get { return backendManager; } }
		
		public string ConfDir { get; private set; }
		
		public IPreferences Preferences { get { return preferences; } }

		protected void InitializeIdle ()
		{
			string backend = null;
			if (customBackendId != null)
				backend = customBackendId;
			else {
				var backendIdString = preferences.Get (PreferencesKeys.CurrentBackend);
				Logger.Debug ("CurrentBackend specified in Preferences: {0}",
				              backendIdString);
				var bs = backendManager.AvailableBackends.SingleOrDefault (
					b => b.Key == backendIdString).Key;
				if (!string.IsNullOrWhiteSpace (bs))
					backend = bs;
			}
			
			backendManager.SetBackend (backend);
			
			trayIcon = GtkTray.CreateTray (this);
			
			BackendManager.BackendChanging += delegate {
				backendWasNullBeforeChange = BackendManager.CurrentBackend == null;
			};
			
			BackendManager.BackendInitialized += delegate {
				if (backendWasNullBeforeChange)
					TaskWindow.Reinitialize (!quietStart, this);
				else
					TaskWindow.Reinitialize (true, this);
				
				if (trayIcon != null)
					trayIcon.RefreshTrayIconTooltip ();
			};
			
			if (backendManager.CurrentBackend == null) {
				// Pop open the preferences dialog so the user can choose a
				// backend service to use.
				ShowPreferences ();
			} else if (!quietStart)
				TaskWindow.ShowWindow (this);
			
			if (backendManager.CurrentBackend == null ||
			    !backendManager.IsBackendConfigured) {
				GLib.Timeout.Add (1000, new GLib.TimeoutHandler (delegate {
					try {
						if (backendManager.CurrentBackend != null &&
						    !backendManager.IsBackendConfigured) {
							backendManager.ReInitializeBackend ();
						}
					} catch (Exception e) {
						Logger.Error ("{0}", e.Message);
					}
					return backendManager.CurrentBackend == null ||
						!backendManager.IsBackendConfigured;
				}));
			}
			
			OnInitializeIdle ();
		}

		public void Exit (int exitcode = 0)
		{
			Logger.Info ("Exit called - terminating application");
			
			if (backendManager != null)
				backendManager.Dispose ();
			
			TaskWindow.SavePosition (Preferences);
			Application.Quit ();
			
			if (Exiting != null)
				Exiting (this, EventArgs.Empty);
			
			Environment.Exit (exitcode);
		}

		/// <summary>
		/// Releases all resource used by the <see cref="Gtk.Tasque.GtkApplicationBase"/> object.
		/// </summary>
		/// <remarks>
		/// Call <see cref="Dispose"/> when you are finished using the <see cref="Gtk.Tasque.GtkApplicationBase"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="Gtk.Tasque.GtkApplicationBase"/> in an unusable state. After
		/// calling <see cref="Dispose"/>, you must release all references to the <see cref="Gtk.Tasque.GtkApplicationBase"/>
		/// so the garbage collector can reclaim the memory that the <see cref="Gtk.Tasque.GtkApplicationBase"/> was occupying.
		/// </remarks>
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

#if ENABLE_NOTIFY_SHARP
		public void ShowAppNotification (string summary, string body)
		{
			var notification = new Notification (
				summary, body, Utilities.GetIcon ("tasque", 48));
			// TODO: Use this API for newer versions of notify-sharp
			//notification.AttachToStatusIcon (
			//		Tasque.Application.Instance.trayIcon);
			notification.Show ();
		}
#endif
		
		public void ShowPreferences ()
		{
			Logger.Info ("OnPreferences called");
			if (preferencesDialog == null) {
				preferencesDialog = new PreferencesDialog (this);
				preferencesDialog.Hidden += OnPreferencesDialogHidden;
			}
			
			preferencesDialog.Present ();
		}
		
		/// <summary>
		/// Dispose the <see cref="Tasque.GtkApplicationBase"/>.
		/// </summary>
		/// <param name='disposing'>
		/// If set to <c>true</c> this method has been invoked by the
		/// <see cref="Dispose"/> method. Otherwise it may have been invoked by
		/// a finalizer.
		/// </param>
		protected virtual void Dispose (bool disposing)
		{
			if (disposed)
				return;
			disposed = true;
			
			if (disposing)
				backendManager.Dispose ();
		}
		
		protected void OnRemoteInstanceKnocked ()
		{
			if (RemoteInstanceKnocked != null)
				RemoteInstanceKnocked (this, EventArgs.Empty);
		}

		protected abstract bool IsRemoteInstanceRunning ();
		protected virtual void OnInitializeIdle () {}

		public event EventHandler Exiting;
		protected event EventHandler RemoteInstanceKnocked;

		internal GtkTray TrayIcon { get { return trayIcon; } }

		void OnPreferencesDialogHidden (object sender, EventArgs args)
		{
			preferencesDialog.Destroy ();
			preferencesDialog.Hidden -= OnPreferencesDialogHidden;
			preferencesDialog = null;
		}
		
		void CheckForDaySwitch ()
		{
			if (DateTime.Today != currentDay) {
				Logger.Debug ("Day has changed, reloading tasks");
				currentDay = DateTime.Today;
				
				// Reinitialize window according to new date
//				if (TaskWindow.IsOpen)
//					TaskWindow.Reinitialize (true, this);
				
				if (trayIcon != null)
					trayIcon.RefreshTrayIconTooltip ();
			}
		}
		
		void ParseArgs (string[] args)
		{
			bool showHelp = false;
			var p = new OptionSet () {
				{ "q|quiet", "hide the Tasque window upon start.",
					v => quietStart = true },
				{ "b|backend=", "the name of the {BACKEND} to use.",
					v => customBackendId = v },
				{ "h|help",  "show this message and exit.",
					v => showHelp = v != null },
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
		
		IPreferences preferences;
		BackendManager backendManager;
		GtkTray trayIcon;
		PreferencesDialog preferencesDialog;
		DateTime currentDay;
		bool backendWasNullBeforeChange, quietStart, disposed;
		string customBackendId;
	}
}
