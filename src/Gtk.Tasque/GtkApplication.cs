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
using System.IO;

namespace Tasque
{
	public class GtkApplication : NativeApplication
	{
		public GtkApplication ()
		{
			confDir = Path.Combine (
				Environment.GetFolderPath (
				Environment.SpecialFolder.ApplicationData),
				"tasque");
			if (!Directory.Exists (confDir))
				Directory.CreateDirectory (confDir);
		}

		public override void Initialize (string[] args)
		{
			Mono.Unix.Catalog.Init ("tasque", Defines.LocaleDir);

			Gtk.Application.Init ();
			
			base.Initialize (args);

			// add package icon path to default icon theme search paths
			Gtk.IconTheme.Default.PrependSearchPath (Defines.IconsDir);
#if WIN && DEBUG
			Gtk.IconTheme.Default.PrependSearchPath (Defines.DataDir + "\\icons");
#endif
		}
		
		public override void StartMainLoop ()
		{
			Gtk.Application.Run ();
		}
		
		public override void QuitMainLoop ()
		{
			Gtk.Application.Quit ();
		}

		public override string ConfDir
		{
			get
			{
				return confDir;
			}
		}

		public override void OpenUrl (string url)
		{
			try {
				System.Diagnostics.Process.Start (url);
			} catch (Exception e) {
				Logger.Error ("Error opening url [{0}]:\n{1}", url, e.ToString ());
			}
		}

		protected override bool IsRemoteInstanceRunning ()
		{
#if LINUX
			// Register Tasque RemoteControl
			try {
				remoteInstance = RemoteControl.Register ();
				if (remoteInstance != null) {
					remoteInstance.RemoteInstanceKnocked = HandleRemoteInstanceKnocked;
					Logger.Debug ("Tasque remote control active.");
				} else {
					// If Tasque is already running, open the tasks window
					// so the user gets some sort of feedback when they
					// attempt to run Tasque again.
					RemoteControl remote = null;
					try {
						remote = RemoteControl.GetInstance ();
						remote.KnockKnock ();
					} catch {}

					Logger.Debug ("Tasque is already running.  Exiting...");
					return true;
				}
			} catch (Exception e) {
				Logger.Debug ("Tasque remote control disabled (DBus exception): {0}", e.Message);
			}
			return false;
#endif
		}

		protected override void ShowMainWindow ()
		{
			TaskWindow.ShowWindow ();
		}
		
		protected override event EventHandler RemoteInstanceKnocked;
		
		void HandleRemoteInstanceKnocked ()
		{
			if (RemoteInstanceKnocked != null)
				RemoteInstanceKnocked (this, EventArgs.Empty);
		}
		
		protected override void Dispose (bool disposing)
		{
			if (disposing)
				remoteInstance.RemoteInstanceKnocked = null;
		}

		string confDir;
#if LINUX
		RemoteControl remoteInstance;
#endif
	}
}
