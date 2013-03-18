/***************************************************************************
 *  PreferencesDialog.cs
 *
 *  Copyright (C) 2008 Novell, Inc.
 *  Written by Scott Reeves <sreeves@gmail.com>
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
using Gtk;
using Mono.Unix;
using Tasque;
using Tasque.Core;

namespace Gtk.Tasque
{

	public class PreferencesDialog : Gtk.Dialog
	{
//		private CheckButton		showCompletedTasksCheck;
		GtkApplicationBase application;
		
		Gtk.Notebook			notebook;
		
		//
		// General Page Widgets
		//
		Gtk.Widget				generalPage;
		int						generalPageId;
		Gtk.ComboBox			backendComboBox;
		Dictionary<int, string> backendComboMap; // track backends
		int 					selectedBackend;
		Gtk.CheckButton			showCompletedTasksCheckButton;
		Gtk.ListStore   		filteredTaskLists;
		List<string>			taskListsToHide;
		Gtk.TreeView			taskListsTree;

		//
		// Appearance Page Widgets
		//
		Gtk.Widget appearancePage;
		Gtk.Entry txtTodaysTaskColor;
		Gtk.ColorButton btnChangeTodaysTaskColor;
		Gtk.Entry txtOverdueTaskColor;
		Gtk.ColorButton btnChangeOverdueTaskColor;
		
		//
		// Backend Page Widgets
		//
		Gtk.Widget				backendPage;
		int						backendPageId;

		public PreferencesDialog (GtkApplicationBase application) : base ()
		{
			if (application == null)
				throw new ArgumentNullException ("application");
			this.application = application;

			LoadPreferences();
			Init();
			ConnectEvents();
			
			Shown += OnShown;
			
			this.WidthRequest = 400;
			this.HeightRequest = 350;
		}
		
		protected override void OnResponse (ResponseType response_id)
		{
			base.OnResponse (response_id);
			
			Hide ();
		}


		private void Init()
		{
			Logger.Debug("Called Preferences Init");
			this.Icon = Utilities.GetIcon ("tasque", 16);
			// Update the window title
			this.Title = string.Format (Catalog.GetString ("Tasque Preferences"));
			
			this.VBox.Spacing = 0;
			this.VBox.BorderWidth = 0;
			this.Resizable = false;
		
			this.AddButton(Stock.Close, Gtk.ResponseType.Ok);
			this.DefaultResponse = ResponseType.Ok;
			
			notebook = new Gtk.Notebook ();
			notebook.ShowTabs = true;
			
			//
			// General Page
			//
			generalPage = MakeGeneralPage ();
			generalPage.Show ();
			generalPageId =
				notebook.AppendPage (generalPage,
									 new Label (Catalog.GetString ("General")));

			//
			// Appearance Page
			//
			appearancePage = MakeAppearancePage ();
			appearancePage.Show ();
			notebook.AppendPage (appearancePage,
			                     new Label (Catalog.GetString ("Appearance")));

			//
			// Backend Page
			//
			backendPage = null;
			backendPageId = -1;
			
			var backendType = application.BackendManager.CurrentBackend;
			if (backendType != null) {
				backendPage = (Widget)application.BackendManager.GetBackendPreferencesWidget ();
				if (backendPage != null) {
					backendPage.Show ();
					var l = new Label (GLib.Markup.EscapeText (
						application.BackendManager.AvailableBackends [backendType]));
					l.UseMarkup = false;
					l.UseUnderline = false;
					l.Show ();
					backendPageId = notebook.AppendPage (backendPage, l);
				}
			}
			
			notebook.Show ();
			this.VBox.PackStart (notebook, true, true, 0);

			DeleteEvent += WindowDeleted;
		}

		private Gtk.Widget MakeAppearancePage ()
		{
			VBox vbox = new VBox (false, 6);
			vbox.BorderWidth = 10;

			VBox sectionVBox = new VBox (false, 4);
			Label l = new Label ();
			l.Markup = string.Format ("<span size=\"large\" weight=\"bold\">{0}</span>",
			                          Catalog.GetString ("Color Management"));
			l.UseUnderline = false;
			l.UseMarkup = true;
			l.Wrap = false;
			l.Xalign = 0;

			l.Show ();
			sectionVBox.PackStart (l, false, false, 0);

			HBox hbox = new HBox (false, 6);
			Label lblTodaysTaskColor = new Label ();
			lblTodaysTaskColor.Text = Catalog.GetString ("Today:");
			lblTodaysTaskColor.Xalign = 0;
			lblTodaysTaskColor.WidthRequest = 75;
			lblTodaysTaskColor.Show ();

			IPreferences prefs = application.Preferences;
			txtTodaysTaskColor = new Entry();
			txtTodaysTaskColor.Text = prefs.Get (PreferencesKeys.TodayTaskTextColor);
			txtTodaysTaskColor.Changed += OnTxtTodaysTaskColorChanged;
			txtTodaysTaskColor.Show ();

			btnChangeTodaysTaskColor = new ColorButton();
			string todayTasksColor = prefs.Get (PreferencesKeys.TodayTaskTextColor);
			Gdk.Color currentColor = new Gdk.Color();
			Gdk.Color.Parse (todayTasksColor, ref currentColor);
			btnChangeTodaysTaskColor.Color = currentColor;

			btnChangeTodaysTaskColor.ColorSet += OnBtnChangeTodaysTaskColorColorSet;
			btnChangeTodaysTaskColor.Show ();

			hbox.PackStart (lblTodaysTaskColor, false, false, 0);
			hbox.PackStart (txtTodaysTaskColor, false, false, 0);
			hbox.PackStart (btnChangeTodaysTaskColor, false, false, 0);
			hbox.Show ();

			HBox hbox2 = new HBox (false, 6);

			Label lblOverdueTaskColor = new Label ();
			lblOverdueTaskColor.Text = Catalog.GetString ("Overdue:");
			lblOverdueTaskColor.WidthRequest = 75;
			lblOverdueTaskColor.Xalign = 0;
			lblOverdueTaskColor.Show ();

			txtOverdueTaskColor = new Entry();
			txtOverdueTaskColor.Text = prefs.Get (PreferencesKeys.OverdueTaskTextColor);
			txtOverdueTaskColor.Changed += OnTxtOverdueTaskColorChanged;
			txtOverdueTaskColor.Show ();

			btnChangeOverdueTaskColor = new ColorButton();
			string overdueTasksColor = prefs.Get (PreferencesKeys.OverdueTaskTextColor);
			Gdk.Color overdueColor = new Gdk.Color();
			Gdk.Color.Parse (overdueTasksColor, ref overdueColor);
			btnChangeOverdueTaskColor.Color = overdueColor;

			btnChangeOverdueTaskColor.ColorSet += OnBtnChangeOverdueTaskColorColorSet;
			btnChangeOverdueTaskColor.Show();

			hbox2.PackStart (lblOverdueTaskColor, false, false, 0);
			hbox2.PackStart (txtOverdueTaskColor, false, false, 0);
			hbox2.PackStart (btnChangeOverdueTaskColor, false, false, 0);
			hbox2.Show ();

			sectionVBox.PackStart (hbox, false, false, 0);
			sectionVBox.PackStart (hbox2, false, false, 0);
			sectionVBox.Show();

			vbox.PackStart (sectionVBox, false, false, 0);

			return vbox;
		}

		private Gtk.Widget MakeGeneralPage ()
		{
			VBox vbox = new VBox (false, 6);
			vbox.BorderWidth = 10;
			
			//
			// ITask Management System
			//
			VBox sectionVBox = new VBox (false, 4);
			Label l = new Label ();
			l.Markup = string.Format ("<span size=\"large\" weight=\"bold\">{0}</span>",
									  Catalog.GetString ("ITask Management System"));
			l.UseUnderline = false;
			l.UseMarkup = true;
			l.Wrap = false;
			l.Xalign = 0;
			
			l.Show ();
			sectionVBox.PackStart (l, false, false, 0);
			
			backendComboBox = ComboBox.NewText ();
			backendComboMap = new Dictionary<int, string> ();
			// Fill out the ComboBox
			int i = 0;
			selectedBackend = -1;
			foreach (var backend in application.BackendManager.AvailableBackends) {
				backendComboBox.AppendText (backend.Value);
				backendComboMap [i] = backend.Key;
				if (backend.Key == application.BackendManager.CurrentBackend)
					selectedBackend = i;
				i++;
			}
			if (selectedBackend >= 0)
				backendComboBox.Active = selectedBackend;
			backendComboBox.Changed += OnBackendComboBoxChanged;
			backendComboBox.Show ();
			
			HBox hbox = new HBox (false, 6);
			l = new Label (string.Empty); // spacer
			l.Show ();
			hbox.PackStart (l, false, false, 0);
			hbox.PackStart (backendComboBox, false, false, 0);
			hbox.Show ();
			sectionVBox.PackStart (hbox, false, false, 0);
			sectionVBox.Show ();
			vbox.PackStart (sectionVBox, false, false, 0);
			
			//
			// ITask Filtering
			//
			sectionVBox = new VBox (false, 4);
			l = new Label ();
			l.Markup = string.Format ("<span size=\"large\" weight=\"bold\">{0}</span>",
									  Catalog.GetString ("ITask Filtering"));
			l.UseUnderline = false;
			l.UseMarkup = true;
			l.Wrap = false;
			l.Xalign = 0;
			
			l.Show ();
			sectionVBox.PackStart (l, false, false, 0);
			
			HBox sectionHBox = new HBox (false, 6);
			l = new Label (string.Empty); // spacer
			l.Show ();
			sectionHBox.PackStart (l, false, false, 0);
			VBox innerSectionVBox = new VBox (false, 6);
			hbox = new HBox (false, 6);
			
			bool showCompletedTasks = application.Preferences.GetBool (
				PreferencesKeys.ShowCompletedTasksKey);
			showCompletedTasksCheckButton =
				new CheckButton (Catalog.GetString ("Sh_ow completed tasks"));
			showCompletedTasksCheckButton.UseUnderline = true;
			showCompletedTasksCheckButton.Active = showCompletedTasks;
			showCompletedTasksCheckButton.Show ();
			hbox.PackStart (showCompletedTasksCheckButton, true, true, 0);
			hbox.Show ();
			innerSectionVBox.PackStart (hbox, false, false, 0);
			
			// TaskLists TreeView
			l = new Label (Catalog.GetString ("Only _show these lists when \"All\" is selected:"));
			l.UseUnderline = true;
			l.Xalign = 0;
			l.Show ();
			innerSectionVBox.PackStart (l, false, false, 0);
			
			ScrolledWindow sw = new ScrolledWindow ();
			sw.HscrollbarPolicy = PolicyType.Automatic;
			sw.VscrollbarPolicy = PolicyType.Automatic;
			sw.ShadowType = ShadowType.EtchedIn;
			
			taskListsTree = new TreeView ();
			taskListsTree.Selection.Mode = SelectionMode.None;
			taskListsTree.RulesHint = false;
			taskListsTree.HeadersVisible = false;
			l.MnemonicWidget = taskListsTree;
			
			Gtk.TreeViewColumn column = new Gtk.TreeViewColumn ();
			column.Title = Catalog.GetString ("ITask List");
			column.Sizing = Gtk.TreeViewColumnSizing.Autosize;
			column.Resizable = false;
			
			Gtk.CellRendererToggle toggleCr = new CellRendererToggle ();
			toggleCr.Toggled += OnTaskListToggled;
			column.PackStart (toggleCr, false);
			column.SetCellDataFunc (toggleCr,
						new Gtk.TreeCellDataFunc (ToggleCellDataFunc));
			
			Gtk.CellRendererText textCr = new CellRendererText ();
			column.PackStart (textCr, true);
			column.SetCellDataFunc (textCr,
						new Gtk.TreeCellDataFunc (TextCellDataFunc));
			
			taskListsTree.AppendColumn (column);
			
			taskListsTree.Show ();
			sw.Add (taskListsTree);
			sw.Show ();
			innerSectionVBox.PackStart (sw, true, true, 0);
			innerSectionVBox.Show ();
			
			sectionHBox.PackStart (innerSectionVBox, true, true, 0);
			sectionHBox.Show ();
			sectionVBox.PackStart (sectionHBox, true, true, 0);
			sectionVBox.Show ();
			vbox.PackStart (sectionVBox, true, true, 0);
			
			return vbox;
		}

		///<summary>
		///	WindowDeleted
		/// Cleans up the conversation object with the ConversationManager
		///</summary>	
		private void WindowDeleted (object sender, DeleteEventArgs args)
		{
			// Save preferences

		}


		private void LoadPreferences()
		{
			Logger.Debug("Loading preferences");
			taskListsToHide =
				application.Preferences.GetStringList (PreferencesKeys.HideInAllTaskList);
			//if (taskListsToHide == null || taskListsToHide.Count == 0)
			//	taskListsToHide = BuildNewTaskListList ();
		}

		private void ConnectEvents()
		{
			// showCompletedTasksCheckbox delegate
			showCompletedTasksCheckButton.Toggled += delegate {
				application.Preferences.SetBool (
					PreferencesKeys.ShowCompletedTasksKey,
					showCompletedTasksCheckButton.Active);
			};
		}

		private void OnBtnChangeTodaysTaskColorColorSet (object sender, EventArgs args)
		{
			txtTodaysTaskColor.Text =
				Utilities.ColorGetHex (btnChangeTodaysTaskColor.Color).ToUpper ();
		}

		private void OnTxtTodaysTaskColorChanged (object sender, EventArgs args)
		{
			// Save the user preference
			application.Preferences.Set (PreferencesKeys.TodayTaskTextColor,
			                             ((Entry) sender).Text);
		}

		private void OnBtnChangeOverdueTaskColorColorSet(object sender, EventArgs args)
		{
			txtOverdueTaskColor.Text =
				Utilities.ColorGetHex (btnChangeOverdueTaskColor.Color).ToUpper ();
		}

		private void OnTxtOverdueTaskColorChanged (object sender, EventArgs args)
		{
			// Save the user preference
			application.Preferences.Set (PreferencesKeys.OverdueTaskTextColor,
			                             ((Entry) sender).Text);
		}

		private void OnBackendComboBoxChanged (object sender, EventArgs args)
		{
			if (selectedBackend >= 0) {
				// TODO: Prompt the user and make sure they really want to change
				// which backend they are using.
				
				// Remove the existing backend's preference page
				if (backendPageId >= 0) {
					notebook.RemovePage (backendPageId);
					backendPageId = -1;
					backendPage = null;
				}
				
				// if yes (replace backend)
				if (backendComboMap.ContainsKey (selectedBackend))
					selectedBackend = -1;
			}
			
			string newBackend = null;
			if (backendComboMap.ContainsKey (backendComboBox.Active))
				newBackend = backendComboMap [backendComboBox.Active];
			application.BackendManager.SetBackend (newBackend);
			
			if (newBackend == null)
				return;
			
			selectedBackend = backendComboBox.Active;
			
			// Add a backend prefs page if one exists
			backendPage = (Widget)application.BackendManager.GetBackendPreferencesWidget ();
			if (backendPage != null) {
				backendPage.Show ();
				var l = new Label (GLib.Markup.EscapeText (
					application.BackendManager.AvailableBackends [newBackend]));
				l.UseMarkup = false;
				l.UseUnderline = false;
				l.Show ();
				backendPageId = notebook.AppendPage (backendPage, l);

				// TODO: If the new backend is not configured, automatically switch
				// to the backend's preferences page
			}
			
			// Save the user preference
			application.Preferences.Set (PreferencesKeys.CurrentBackend,
										 newBackend.GetType ().ToString ());
			
			//taskListsToHide = BuildNewTaskListList ();
			//Application.Preferences.SetStringList (IPreferences.HideInAllTaskList,
			//									   taskListsToHide);
			RebuildTaskListTree ();
		}
		
		private void ToggleCellDataFunc (Gtk.TreeViewColumn column,
											 Gtk.CellRenderer cell,
											 Gtk.TreeModel model,
											 Gtk.TreeIter iter)
		{
			Gtk.CellRendererToggle crt = cell as Gtk.CellRendererToggle;
			ITaskList taskList = model.GetValue (iter, 0) as ITaskList;
			if (taskList == null) {
				crt.Active = true;
				return;
			}
			
			// If the setting is null or empty, show all taskLists
			if (taskListsToHide == null || taskListsToHide.Count == 0) {
				crt.Active = true;
				return;
			}
			
			// Check to see if the taskList is specified in the list
			if (taskListsToHide.Contains (taskList.Name)) {
				crt.Active = false;
				return;
			}
			
			crt.Active = true;
		}
		
		private void TextCellDataFunc (Gtk.TreeViewColumn treeColumn,
				Gtk.CellRenderer renderer, Gtk.TreeModel model,
				Gtk.TreeIter iter)
		{
			Gtk.CellRendererText crt = renderer as Gtk.CellRendererText;
			crt.Ellipsize = Pango.EllipsizeMode.End;
			ITaskList taskList = model.GetValue (iter, 0) as ITaskList;
			if (taskList == null) {
				crt.Text = string.Empty;
				return;
			}
			
			crt.Text = GLib.Markup.EscapeText (taskList.Name);
		}
		
		void OnTaskListToggled (object sender, Gtk.ToggledArgs args)
		{
			Logger.Debug ("OnTaskListToggled");
			Gtk.TreeIter iter;
			Gtk.TreePath path = new Gtk.TreePath (args.Path);
			if (!taskListsTree.Model.GetIter (out iter, path))
				return; // Do nothing
			
			ITaskList taskList = taskListsTree.Model.GetValue (iter, 0) as ITaskList;
			if (taskList == null)
				return;
			
			//if (taskListsToHide == null)
			//	taskListsToHide = BuildNewTaskListList ();
			
			if (taskListsToHide.Contains (taskList.Name))
				taskListsToHide.Remove (taskList.Name);
			else
				taskListsToHide.Add (taskList.Name);
			
			application.Preferences.SetStringList (PreferencesKeys.HideInAllTaskList,
												   taskListsToHide);
		}
		
/*
		/// <summary>
		/// Build a new taskList list setting from all the taskLists
		/// </summary>
		/// <param name="?">
		/// A <see cref="System.String"/>
		/// </param>
		List<string> BuildNewTaskListList ()
		{
			List<string> list = new List<string> ();
			TreeModel model;
			IBackend backend = Application.Backend;
			if (backend == null)
				return list;
			
			model = backend.TaskLists;
			Gtk.TreeIter iter;
			if (model.GetIterFirst (out iter) == false)
				return list;
			
			do {
				ITaskList cat = model.GetValue (iter, 0) as ITaskList;
				if (cat == null || cat is AllTaskList)
					continue;

				list.Add (cat.Name);
			} while (model.IterNext (ref iter) == true);
			
			return list;
		}
*/
		
		void RebuildTaskListTree ()
		{
			if (!backendComboMap.ContainsKey (selectedBackend)) {
				taskListsTree.Model = null;
				return;
			}
			
			filteredTaskLists = new ListStore (typeof (ITaskList));
			foreach (var item in application.BackendManager.TaskLists) {
				if (!(item == null || item.ListType == TaskListType.Smart))
					filteredTaskLists.AppendValues (item);
			}
			taskListsTree.Model = filteredTaskLists;
		}
		
		void OnShown (object sender, EventArgs args)
		{
			RebuildTaskListTree ();
		}
	}
}
