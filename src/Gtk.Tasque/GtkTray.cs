// 
// GtkTray.cs
//  
// Author:
//       Antonius Riha <antoniusriha@gmail.com>
// 
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
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Mono.Unix;
using Tasque;
using Gtk;

namespace Gtk.Tasque
{
	public abstract class GtkTray
	{
		public static GtkTray CreateTray (GtkApplicationBase application)
		{
			var desktopSession = Environment.GetEnvironmentVariable ("DESKTOP_SESSION");
			GtkTray tray;
			switch (desktopSession) {
			case "ubuntu":
			case "ubuntu-2d":
			case "gnome-classic":
			case "gnome-fallback":
#if APPINDICATOR
				tray = new AppIndicatorTray (application);
				break;
#endif
			default:
				tray = new StatusIconTray (application);
				break;
			}
			return tray;
		}

		protected GtkTray (GtkApplicationBase application)
		{
			if (application == null)
				throw new ArgumentNullException ("application");
			this.application = application;

			RegisterUIManager ();
			
			application.BackendManager.BackendChanging += delegate {
				SwitchBackendItems (false); };
			application.BackendManager.BackendInitialized += delegate {
				SwitchBackendItems (true); };
			((INotifyCollectionChanged)application.BackendManager.Tasks).CollectionChanged
				+= delegate { RefreshTrayIconTooltip (); };
			RefreshTrayIconTooltip ();
		}

		public void RefreshTrayIconTooltip ()
		{
			var oldTooltip = Tooltip;
			var sb = new StringBuilder ();
			var tasks = application.BackendManager.Tasks;

			var overdueStart = DateTime.MinValue;
			var overdueEnd = DateTime.Today.AddSeconds (-1);
			var overdueTasks = tasks.Where (
				t => t.DueDate > overdueStart && t.DueDate < overdueEnd);
			if (overdueTasks.Any ()) {
				var overdueCount = overdueTasks.Count ();
				sb.AppendFormat (Catalog.GetPluralString ("{0} task is Overdue",
				    "{0} tasks are Overdue", overdueCount), overdueCount);
				sb.AppendLine ();
			}

			var todayEnd = DateTime.Today.AddDays (1).AddSeconds (-1);
			var todayTasks = tasks.Where (
				t => t.DueDate > DateTime.Today && t.DueDate < todayEnd);
			if (todayTasks.Any ()) {
				var todayCount = todayTasks.Count ();
				sb.AppendFormat (Catalog.GetPluralString ("{0} task for Today",
				    "{0} tasks for Today", todayCount), todayCount);
				sb.AppendLine ();
			}

			var tomorrowStart = DateTime.Today.AddDays (1);
			var tomorrowEnd = DateTime.Today.AddDays (2).AddSeconds (-1);
			var tomorrowTasks = tasks.Where (
				t => t.DueDate > tomorrowStart && t.DueDate < tomorrowEnd);
			if (tomorrowTasks.Any ()) {
				var tomorrowCount = tomorrowTasks.Count ();
				sb.AppendFormat (Catalog.GetPluralString ("{0} task for Tomorrow",
				    "{0} tasks for Tomorrow", tomorrowCount), tomorrowCount);
				sb.AppendLine ();
			}

			if (sb.Length == 0) {
				// Translators: This is the status icon's tooltip. When no tasks are overdue, due today, or due tomorrow, it displays this fun message
				Tooltip = Catalog.GetString ("Tasque Rocks");
			} else
				Tooltip = sb.ToString ().TrimEnd (Environment.NewLine.ToCharArray ());

			if (Tooltip != oldTooltip)
				OnTooltipChanged ();
		}

		protected string IconName { get { return "tasque-panel"; } }
		
		protected Menu Menu {
			get { return (Menu)uiManager.GetWidget ("/TrayIconMenu"); }
		}
		
		protected Gtk.Action ToggleTaskWindowAction { get; private set; }

		protected string Tooltip { get; private set; }
		
		protected virtual void OnTooltipChanged () {}

		void OnAbout (object sender, EventArgs args)
		{
			var authors = Defines.Authors;
			var translators = Catalog.GetString ("translator-credits");
			if (translators == "translator-credits")
				translators = null;
			
			var about = new AboutDialog ();
			about.ProgramName = "Tasque";
			about.Version = Defines.Version;
			about.Logo = Utilities.GetIcon("tasque", 48);
			about.Copyright = Defines.CopyrightInfo;
			about.Comments = Catalog.GetString ("A Useful Task List");
			about.Website = Defines.Website;
			about.WebsiteLabel = Catalog.GetString("Tasque Project Homepage");
			about.Authors = authors;
			about.TranslatorCredits = translators;
			about.License = Defines.License;
			about.IconName = "tasque";
			about.Run ();
			about.Destroy ();
		}

		void RegisterUIManager ()
		{
			var newTaskAction = new ActionEntry ("NewTaskAction", Stock.New,
			    Catalog.GetString ("New Task ..."), null, null, delegate {
				// Show the TaskWindow and then cause a new task to be created
				TaskWindow.ShowWindow (application);
				TaskWindow.GrabNewTaskEntryFocus (application);
			});
			
			var refreshAction =	new ActionEntry ("RefreshAction", Stock.Execute,
			    Catalog.GetString ("Refresh Tasks ..."), null, null,
			    delegate { application.BackendManager.ReInitializeBackend ();
			});
			
			
			var trayActionGroup = new ActionGroup ("Tray");
			trayActionGroup.Add (new ActionEntry [] {
				newTaskAction,
				new ActionEntry ("AboutAction", Stock.About, OnAbout),
				new ActionEntry ("PreferencesAction", Stock.Preferences,
				                 delegate { application.ShowPreferences (); }),
				refreshAction,
				new ActionEntry ("QuitAction", Stock.Quit,
				                 delegate { application.Exit (); })
			});
			
			ToggleTaskWindowAction = new Gtk.Action ("ToggleTaskWindowAction", Catalog.GetString ("Toggle Task Window"));
			ToggleTaskWindowAction.ActionGroup = trayActionGroup;
			ToggleTaskWindowAction.Activated += delegate { TaskWindow.ToggleWindowVisible (application); };
			
			uiManager = new UIManager ();
			uiManager.AddUiFromString (MenuXml);
			uiManager.InsertActionGroup (trayActionGroup, 0);
			
			SwitchBackendItems (false);
		}

		void SwitchBackendItems (bool onOrOff) {
			uiManager.GetAction ("/TrayIconMenu/NewTaskAction").Sensitive = onOrOff;
			uiManager.GetAction ("/TrayIconMenu/RefreshAction").Sensitive = onOrOff;
		}

		GtkApplicationBase application;
		UIManager uiManager;
		const string MenuXml = @"
<ui>
	<popup name=""TrayIconMenu"">
		<menuitem action=""NewTaskAction""/>
		<separator/>
		<menuitem action=""PreferencesAction""/>
		<menuitem action=""AboutAction""/>
		<separator/>
		<menuitem action=""RefreshAction""/>
		<separator/>
		<menuitem action=""QuitAction""/>
	</popup>
</ui>
";
	}
}
