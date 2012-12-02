// RtmPreferencesWidget.cs created with MonoDevelop
// User: boyd at 11:29 PM 2/18/2008
using System;
using System.Diagnostics;
using Mono.Unix;
using Gdk;
using Gtk;
using Tasque;
using Tasque.Backends;

namespace Tasque.Backends.Rtm
{
	public class RtmPreferencesWidget : Gtk.EventBox, IBackendPreferences
	{
		Preferences preferences;
		RtmBackend backend;

 		private LinkButton		authButton;
		
		private Label			statusLabel;
		private Gtk.Image		image;
		private bool			authRequested;
		private bool			isAuthorized;
		
		private static Gdk.Pixbuf normalPixbuf;
		
		static RtmPreferencesWidget ()
		{
			normalPixbuf = GetIcon ("tasque-rtm-logo", 128);
		}
		
		public RtmPreferencesWidget (RtmBackend backend, Preferences preferences) : base ()
		{
			if (backend == null)
				throw new ArgumentNullException ("backend");
			if (preferences == null)
				throw new ArgumentNullException ("preferences");
			this.backend = backend;
			this.preferences = preferences;

			LoadPreferences ();
			
			BorderWidth = 0;
			
			// We're using an event box so we can paint the background white
			EventBox imageEb = new EventBox ();
			imageEb.BorderWidth = 0;
			imageEb.ModifyBg(StateType.Normal, new Gdk.Color(255,255,255));
			imageEb.ModifyBase(StateType.Normal, new Gdk.Color(255,255,255)); 
			imageEb.Show ();
			
			VBox mainVBox = new VBox(false, 0);
			mainVBox.BorderWidth = 10;
			mainVBox.Show();
			Add(mainVBox);

			// Add the rtm logo
			image = new Gtk.Image (normalPixbuf);
			image.Show();
			//make the dialog box look pretty without hard coding total size and
			//therefore clipping displays with large fonts.
			Alignment spacer = new Alignment((float)0.5, 0, 0, 0);
			spacer.SetPadding(0, 0, 125, 125);
			spacer.Add(image);
			spacer.Show();
			imageEb.Add (spacer);
			mainVBox.PackStart(imageEb, true, true, 0);

			// Status message label
			statusLabel = new Label();
			statusLabel.Justify = Gtk.Justification.Center;
			statusLabel.Wrap = true;
			statusLabel.LineWrap = true;
			statusLabel.Show();
			statusLabel.UseMarkup = true;
			statusLabel.UseUnderline = false;

			authButton = new LinkButton (Catalog.GetString ("Click Here to Connect"));
			
			authButton.Clicked += OnAuthButtonClicked;
			
			if ( isAuthorized ) {
				statusLabel.Text = "\n\n" +
					Catalog.GetString ("You are currently connected");
				string userName = preferences.Get (Preferences.UserNameKey);
				if (userName != null && userName.Trim () != string.Empty)
					statusLabel.Text = "\n\n" +
						Catalog.GetString ("You are currently connected as") +
						"\n" + userName.Trim();
			} else {
				statusLabel.Text = "\n\n" +
					Catalog.GetString ("You are not connected");
				authButton.Show();
			}
			mainVBox.PackStart(statusLabel, false, false, 0);
			mainVBox.PackStart(authButton, false, false, 0);

			Label blankLabel = new Label("\n");
			blankLabel.Show();
			mainVBox.PackStart(blankLabel, false, false, 0);
		}
		
		private void LoadPreferences ()
		{
			string authToken = preferences.Get(Preferences.AuthTokenKey);
			if (authToken == null || authToken.Trim() == "") {
				Logger.Debug("Rtm: Not authorized");
				isAuthorized = false;
			} else {
				Logger.Debug("Rtm: Authorized");
				isAuthorized = true;
			}
		}
		
		private void OnAuthButtonClicked (object sender, EventArgs args)
		{
			if (!isAuthorized && !authRequested) {
				string url = string.Empty;
				try {
					url = backend.GetAuthUrl ();
				} catch (Exception) {
					Logger.Debug ("Failed to get auth URL from Remember the Milk. Try again later.");
					authButton.Label = Catalog.GetString ("Remember the Milk not responding. Try again later.");
					return;
				}
				Logger.Debug ("Launching browser to authorize with Remember the Milk");
				try {
					Process.Start (url);
					authRequested = true;
					authButton.Label = Catalog.GetString ("Click Here After Authorizing");
				} catch (Exception ex) {
					Logger.Error ("Exception opening URL: {0}", ex.Message);
					authButton.Label = Catalog.GetString ("Set the default browser and try again");
				}
			} else if (!isAuthorized && authRequested) {
				authButton.Label = Catalog.GetString ("Processing...");
				try {
					backend.FinishedAuth ();
					Logger.Debug ("Successfully authorized with Remember the Milk");
					isAuthorized = true;
					authRequested = false;
				} catch (RtmNet.RtmApiException) {
					Logger.Debug ("Failed to authorize with Remember the Milk");
					isAuthorized = false;
					authRequested = true;
					authButton.Label = Catalog.GetString ("Failed, Try Again");
				}
			}
			if (isAuthorized) {
				authButton.Label = Catalog.GetString ("Thank You");
				authButton.Sensitive = false;
				statusLabel.Text = "\n\n" +
					Catalog.GetString ("You are currently connected");
				string userName = preferences.Get (Preferences.UserNameKey);
				if (userName != null && userName.Trim () != string.Empty)
					statusLabel.Text = "\n\n" +
						Catalog.GetString ("You are currently connected as") +
						"\n" + userName.Trim ();
			}
		}

		static Pixbuf GetIcon (string iconName, int size)
		{
			try {
				return IconTheme.Default.LoadIcon (iconName, size, 0);
			} catch (GLib.GException) {}
			
			try {
				var ret = new Pixbuf (null, iconName + ".png");
				return ret.ScaleSimple (size, size, InterpType.Bilinear);
			} catch (ArgumentException) {}
			
			// TODO: This is a temporary fix to allow installing all icons as assembly
			//       resources. The proper thing to do is to ship the actual icons,
			//       and append to the default gtk+ icon theme path. See Tomboy.
			try {
				var ret = new Pixbuf (null, string.Format ("{0}-{1}.png", iconName, size));
				return ret.ScaleSimple (size, size, InterpType.Bilinear);
			} catch (ArgumentException) {}
			
			Logger.Debug ("Unable to load icon '{0}'.", iconName);
			return null;
		}
	}
}
