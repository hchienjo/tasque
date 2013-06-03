/***************************************************************************
 *  TargetWindow.cs
 *
 *  Copyright (C) 2007 Novell, Inc.
 *  Written by:
 *		Calvin Gaisford <calvinrg@gmail.com>
 *		Boyd Timothy <btimothy@gmail.com>
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
using System.Collections.Specialized;
using System.Linq;
using Gdk;
using Gtk;
using Mono.Unix;
using Tasque;
using Tasque.Core;
using Tasque.DateFormatters;

namespace Gtk.Tasque
{
	public class TaskWindow : Gtk.Window 
	{
		GtkApplicationBase application;

		private static TaskWindow taskWindow = null;
		private static int lastXPos;
		private static int lastYPos;
		private static Gdk.Pixbuf noteIcon;
		
		private ScrolledWindow scrolledWindow;
		
		private Entry addTaskEntry;
		private MenuToolButton addTaskButton;
		private Gtk.ComboBox taskListComboBox;
		private Gtk.VBox targetVBox;
		
		private TaskGroup overdueGroup;
		private TaskGroup todayGroup;
		private TaskGroup tomorrowGroup;
		private TaskGroup nextSevenDaysGroup;
		private TaskGroup futureGroup;
		private CompletedTaskGroup completedTaskGroup;
		private EventBox innerEb;

		private List<TaskGroup> taskGroups;
		
		private Dictionary<ITask, NoteDialog> noteDialogs;
		
		private Gtk.Statusbar statusbar;
		private uint statusContext;
		private uint currentStatusMessageId;
		private static uint ShowOriginalStatusId;
		private static string status;
		private static string lastLoadedTime;
		private const uint DWELL_TIME_MS = 8000;
		
		private ITask clickedTask;
		
		private Gtk.AccelGroup accelGroup;
		private GlobalKeybinder globalKeys;
		
		static TaskWindow ()
		{
			noteIcon = Utilities.GetIcon ("tasque-note", 16);
		}
		
		public TaskWindow (GtkApplicationBase application) : base (Gtk.WindowType.Toplevel)
		{
			if (application == null)
				throw new ArgumentNullException ("application");
			this.application = application;

			taskGroups = new List<TaskGroup> ();
			noteDialogs = new Dictionary<ITask, NoteDialog> ();
			InitWindow();
			
			Realized += OnRealized;
		}

		void InitWindow()
		{
			int height;
			int width;
			
			this.Icon = Utilities.GetIcon ("tasque", 48);
			// Update the window title
			Title = string.Format ("Tasque");	

			width = application.Preferences.GetInt("MainWindowWidth");
			height = application.Preferences.GetInt("MainWindowHeight");
			
			if(width == -1)
				width = 600;
			if(height == -1)
				height = 600;
			
			this.DefaultSize = new Gdk.Size( width, height);
			
			accelGroup = new AccelGroup ();
			AddAccelGroup (accelGroup);
			globalKeys = new GlobalKeybinder (accelGroup);

			VBox mainVBox = new VBox();
			mainVBox.BorderWidth = 0;
			mainVBox.Show ();
			this.Add (mainVBox);
			
			HBox topHBox = new HBox (false, 0);
			topHBox.BorderWidth = 4;
			
			taskListComboBox = new ComboBox ();
			taskListComboBox.Accessible.Description = "ITaskList Selection";
			taskListComboBox.WidthRequest = 150;
			taskListComboBox.WrapWidth = 1;
			taskListComboBox.Sensitive = false;
			CellRendererText comboBoxRenderer = new Gtk.CellRendererText ();
			comboBoxRenderer.WidthChars = 20;
			comboBoxRenderer.Ellipsize = Pango.EllipsizeMode.End;
			taskListComboBox.PackStart (comboBoxRenderer, true);
			taskListComboBox.SetCellDataFunc (comboBoxRenderer,
				new Gtk.CellLayoutDataFunc (TaskListComboBoxDataFunc));
			
			taskListComboBox.Show ();
			topHBox.PackStart (taskListComboBox, false, false, 0);
			
			// Space the addTaskButton and the taskListComboBox
			// far apart by using a blank label that expands
			Label spacer = new Label (string.Empty);
			spacer.Show ();
			topHBox.PackStart (spacer, true, true, 0);
			
			// The new task entry widget
			addTaskEntry = new Entry (Catalog.GetString ("New task..."));
			addTaskEntry.Sensitive = false;
			addTaskEntry.Focused += OnAddTaskEntryFocused;
			addTaskEntry.Changed += OnAddTaskEntryChanged;
			addTaskEntry.Activated += OnAddTaskEntryActivated;
			addTaskEntry.FocusInEvent += OnAddTaskEntryFocused;
			addTaskEntry.FocusOutEvent += OnAddTaskEntryUnfocused;
			addTaskEntry.DragDataReceived += OnAddTaskEntryDragDataReceived;
			addTaskEntry.Show ();
			topHBox.PackStart (addTaskEntry, true, true, 0);
			
			// Use a small add icon so the button isn't mammoth-sized
			HBox buttonHBox = new HBox (false, 6);
			Gtk.Image addImage = new Gtk.Image (Gtk.Stock.Add, IconSize.Menu);
			addImage.Show ();
			buttonHBox.PackStart (addImage, false, false, 0);
			Label l = new Label (Catalog.GetString ("_Add"));
			l.Show ();
			buttonHBox.PackStart (l, true, true, 0);
			buttonHBox.Show ();
			addTaskButton = new MenuToolButton (buttonHBox, Catalog.GetString ("_Add Task"));
			addTaskButton.UseUnderline = true;
			// Disactivate the button until the backend is initialized
			addTaskButton.Sensitive = false;
			Gtk.Menu addTaskMenu = new Gtk.Menu ();
			addTaskButton.Menu = addTaskMenu;
			addTaskButton.Clicked += OnAddTask;
			addTaskButton.Show ();
			topHBox.PackStart (addTaskButton, false, false, 0);
			
			globalKeys.AddAccelerator (OnGrabEntryFocus,
						(uint) Gdk.Key.n,
						Gdk.ModifierType.ControlMask,
						Gtk.AccelFlags.Visible);
			
			globalKeys.AddAccelerator (delegate (object sender, EventArgs e) {
				application.Exit (); },
						(uint) Gdk.Key.q,
						Gdk.ModifierType.ControlMask,
						Gtk.AccelFlags.Visible);

			this.KeyPressEvent += KeyPressed;
			
			topHBox.Show ();
			mainVBox.PackStart (topHBox, false, false, 0);

			scrolledWindow = new ScrolledWindow ();
			scrolledWindow.VscrollbarPolicy = PolicyType.Automatic;
			scrolledWindow.HscrollbarPolicy = PolicyType.Never;

			scrolledWindow.BorderWidth = 0;
			scrolledWindow.CanFocus = true;
			scrolledWindow.Show ();
			mainVBox.PackStart (scrolledWindow, true, true, 0);

			innerEb = new EventBox();
			innerEb.BorderWidth = 0;
			innerEb.ButtonPressEvent += OnTargetVBoxButtonPress;
			Gdk.Color backgroundColor = GetBackgroundColor ();
			innerEb.ModifyBg (StateType.Normal, backgroundColor);
			innerEb.ModifyBase (StateType.Normal, backgroundColor);
			
			targetVBox = new VBox();
			targetVBox.BorderWidth = 5;
			targetVBox.Show ();
			innerEb.Add(targetVBox);

			scrolledWindow.AddWithViewport(innerEb);
			
			statusbar = new Gtk.Statusbar ();
			statusbar.HasResizeGrip = true;
			statusbar.Show ();

			mainVBox.PackEnd (statusbar, false, false, 0);
			
			//
			// Delay adding in the TaskGroups until the backend is initialized
			//
			
			Shown += OnWindowShown;
			DeleteEvent += WindowDeleted;
			
			application.BackendManager.BackendInitialized += OnBackendInitialized;
			// FIXME: if the backend is already initialized, go ahead... initialize
			OnBackendInitialized (null, null);
			
			application.Preferences.SettingChanged += OnSettingChanged;
		}

		void PopulateWindow()
		{
			// Add in the groups
			
			//
			// Overdue Group
			//
			DateTime rangeStart;
			DateTime rangeEnd;
			
			rangeStart = DateTime.MinValue;
			rangeEnd = DateTime.Now.AddDays (-1);
			rangeEnd = new DateTime (rangeEnd.Year, rangeEnd.Month, rangeEnd.Day,
									 23, 59, 59);
			
			overdueGroup = new TaskGroup (Catalog.GetString ("Overdue"), rangeStart, rangeEnd,
										  application.BackendManager.Tasks, application);
			overdueGroup.RowActivated += OnRowActivated;
			overdueGroup.ButtonPressed += OnButtonPressed;
			overdueGroup.Show ();
			targetVBox.PackStart (overdueGroup, false, false, 0);
			taskGroups.Add(overdueGroup);
			
			//
			// Today Group
			//
			rangeStart = DateTime.Now;
			rangeStart = new DateTime (rangeStart.Year, rangeStart.Month,
									   rangeStart.Day, 0, 0, 0);
			rangeEnd = DateTime.Now;
			rangeEnd = new DateTime (rangeEnd.Year, rangeEnd.Month,
									 rangeEnd.Day, 23, 59, 59);
			todayGroup = new TaskGroup (Catalog.GetString ("Today"), rangeStart, rangeEnd,
										application.BackendManager.Tasks, application);
			todayGroup.RowActivated += OnRowActivated;
			todayGroup.ButtonPressed += OnButtonPressed;
			todayGroup.Show ();
			targetVBox.PackStart (todayGroup, false, false, 0);
			taskGroups.Add (todayGroup);
			
			//
			// Tomorrow Group
			//
			rangeStart = DateTime.Now.AddDays (1);
			rangeStart = new DateTime (rangeStart.Year, rangeStart.Month,
									   rangeStart.Day, 0, 0, 0);
			rangeEnd = DateTime.Now.AddDays (1);
			rangeEnd = new DateTime (rangeEnd.Year, rangeEnd.Month,
									 rangeEnd.Day, 23, 59, 59);
			tomorrowGroup = new TaskGroup (Catalog.GetString ("Tomorrow"), rangeStart, rangeEnd,
										   application.BackendManager.Tasks, application);
			tomorrowGroup.RowActivated += OnRowActivated;
			tomorrowGroup.ButtonPressed += OnButtonPressed;
			tomorrowGroup.Show ();
			targetVBox.PackStart (tomorrowGroup, false, false, 0);
			taskGroups.Add (tomorrowGroup);
			
			//
			// Next 7 Days Group
			//
			rangeStart = DateTime.Now.AddDays (2);
			rangeStart = new DateTime (rangeStart.Year, rangeStart.Month,
									   rangeStart.Day, 0, 0, 0);
			rangeEnd = DateTime.Now.AddDays (6);
			rangeEnd = new DateTime (rangeEnd.Year, rangeEnd.Month,
									 rangeEnd.Day, 23, 59, 59);
			nextSevenDaysGroup = new TaskGroup (Catalog.GetString ("Next 7 Days"), rangeStart,
			                                    rangeEnd, application.BackendManager.Tasks,
			                                    application);
			nextSevenDaysGroup.RowActivated += OnRowActivated;
			nextSevenDaysGroup.ButtonPressed += OnButtonPressed;
			nextSevenDaysGroup.Show ();
			targetVBox.PackStart (nextSevenDaysGroup, false, false, 0);
			taskGroups.Add (nextSevenDaysGroup);
			
			//
			// Future Group
			//
			rangeStart = DateTime.Now.AddDays (7);
			rangeStart = new DateTime (rangeStart.Year, rangeStart.Month,
									   rangeStart.Day, 0, 0, 0);
			rangeEnd = DateTime.MaxValue;
			futureGroup = new TaskGroup (Catalog.GetString ("Future"), rangeStart, rangeEnd,
										 application.BackendManager.Tasks, application);
			futureGroup.RowActivated += OnRowActivated;
			futureGroup.ButtonPressed += OnButtonPressed;
			futureGroup.Show ();
			targetVBox.PackStart (futureGroup, false, false, 0);
			taskGroups.Add (futureGroup);
			
			//
			// Completed Tasks Group
			//
			rangeStart = DateTime.MinValue;
			rangeEnd = DateTime.MaxValue;
			completedTaskGroup = new CompletedTaskGroup (Catalog.GetString ("Completed"),
			                                             rangeStart, rangeEnd,
			                                             application.BackendManager.Tasks,
			                                             application);
			completedTaskGroup.RowActivated += OnRowActivated;
			completedTaskGroup.ButtonPressed += OnButtonPressed;
			completedTaskGroup.Show ();
			targetVBox.PackStart (completedTaskGroup, false, false, 0);
			taskGroups.Add (completedTaskGroup);
			

			//manualTarget = new TargetService();
			//manualTarget.Show ();
			//mainVBox.PackStart(manualTarget, false, false, 0);
			
			
			// Set up the combo box (after the above to set the current filter)

			var taskListComboStore = new ListStore (typeof(ITaskList));
			foreach (var item in application.BackendManager.TaskLists) {
				taskListComboStore.AppendValues (item);
			}
			
			taskListComboBox.Model = taskListComboStore;

			// Read preferences for the last-selected taskList and select it
			string selectedTaskListName =
				application.Preferences.Get (PreferencesKeys.SelectedTaskListKey);
			
			taskListComboBox.Changed += OnTaskListChanged;
			
			SelectTaskList (selectedTaskListName);
		}
		
		#region Public Methods
		/// <summary>
		/// Method to allow other classes to "click" on the "Add ITask" button.
		/// </summary>
		public static void AddTask (GtkApplicationBase application)
		{
			if (taskWindow == null)
				TaskWindow.ShowWindow (application);
			
			taskWindow.OnAddTask (null, EventArgs.Empty);
		}

		public static void SavePosition (IPreferences preferences)
		{
			if(taskWindow != null) {
				int x;
				int y;
				int width;
				int height;

				taskWindow.GetPosition(out x, out y);
				taskWindow.GetSize(out width, out height);

				lastXPos = x;
				lastYPos = y;
				
				preferences.SetInt("MainWindowLastXPos", lastXPos);
				preferences.SetInt("MainWindowLastYPos", lastYPos);
				preferences.SetInt("MainWindowWidth", width);
				preferences.SetInt("MainWindowHeight", height);	
			}

		}
		
		public static void ShowWindow (GtkApplicationBase application)
		{
			ShowWindow (false, application);
		}
		
		public static void ToggleWindowVisible (GtkApplicationBase application)
		{
			ShowWindow (true, application);
		}
		
		private static void ShowWindow (bool supportToggle, GtkApplicationBase application)
		{
			if(taskWindow != null) {
				if(taskWindow.IsActive && supportToggle) {
					int x;
					int y;

					taskWindow.GetPosition(out x, out y);

					lastXPos = x;
					lastYPos = y;

					taskWindow.Hide();
				} else {
					if(!taskWindow.Visible) {
						int x = lastXPos;
						int y = lastYPos;

						if (x >= 0 && y >= 0)
							taskWindow.Move(x, y);						
					}
					taskWindow.Present();
				}
			} else if (application.BackendManager.CurrentBackend != null) {
				taskWindow = new TaskWindow (application);
				if(lastXPos == 0 || lastYPos == 0)
				{
					lastXPos = application.Preferences.GetInt("MainWindowLastXPos");
					lastYPos = application.Preferences.GetInt("MainWindowLastYPos");				
				}

				int x = lastXPos;
				int y = lastYPos;

				if (x >= 0 && y >= 0)
					taskWindow.Move(x, y);						

				taskWindow.ShowAll();
			}
		}
		
		public static void GrabNewTaskEntryFocus (GtkApplicationBase application)
		{
			if (taskWindow == null)
				TaskWindow.ShowWindow (application);
			
			taskWindow.addTaskEntry.GrabFocus ();
		}
		
		public static void SelectAndEdit (ITask task, GtkApplicationBase application)
		{
			ShowWindow (application);
			taskWindow.EnterEditMode (task, true);
			taskWindow.Present ();
		}

		public static bool ShowOriginalStatus ()
		{
			// Translators: This status shows the date and time when the task list was last refreshed
			status = string.Format (Catalog.GetString ("Tasks loaded: {0}"),
			                        TaskWindow.lastLoadedTime);
			TaskWindow.ShowStatus (status);
			return false;
		}
		
		public static void ShowStatus (string statusText)
		{
			// By default show the new status for 8 seconds
			ShowStatus (statusText, DWELL_TIME_MS);
		}

		public static void ShowStatus (string statusText, uint dwellTime)
		{
			if (taskWindow == null) {
				Logger.Warn ("Cannot set status when taskWindow is null");
				return;
			}

			// remove old timer to show original status and then start another one
			if (ShowOriginalStatusId > 0)
				GLib.Source.Remove (ShowOriginalStatusId);
			// any status will dwell for <dwellTime> seconds and then the original
			//status will be shown
			ShowOriginalStatusId = GLib.Timeout.Add (dwellTime, ShowOriginalStatus);
			
			if (taskWindow.currentStatusMessageId != 0) {
				// Pop the old message
				taskWindow.statusbar.Remove (taskWindow.statusContext,
								taskWindow.currentStatusMessageId);
				taskWindow.currentStatusMessageId = 0;
			}
			
			taskWindow.currentStatusMessageId =
				taskWindow.statusbar.Push (taskWindow.statusContext,
								statusText);
		}

		public static bool IsOpen
		{
			get {
				return taskWindow != null && taskWindow.IsRealized;
			}
		}
		
		/// <summary>
		/// This should be called after a new IBackend has been set
		/// </summary>
		public static void Reinitialize (bool show, GtkApplicationBase application)
		{
			if (TaskWindow.taskWindow != null) {
				TaskWindow.taskWindow.Hide ();
				TaskWindow.taskWindow.Destroy ();
				TaskWindow.taskWindow = null;
			}

			if (show)
				TaskWindow.ShowWindow (application);
		}
		
		public void HighlightTask (ITask task)
		{
			Gtk.TreeIter iter;
			
			// Make sure we've waited around for the new task to fully
			// be added to the TreeModel before continuing.  Some
			// backends might be threaded and will have used something
			// like Gtk.Idle.Add () to actually store the new ITask in
			// their TreeModel.
			while (Gtk.Application.EventsPending ())
				Gtk.Application.RunIteration ();

			foreach (TaskGroup taskGroup in taskGroups) {
				if (taskGroup.ContainsTask (task, out iter)) {
					taskGroup.TaskView.TreeView.Selection.SelectIter (iter);
					break;
				}
			}
		}
		
		/// <summary>
		/// Search through the TaskGroups looking for the specified task and
		/// adjust the window so the new task is showing.
		/// </summary>
		/// <param name="task">
		/// A <see cref="ITask"/>
		/// </param>
		public void ScrollToTask (ITask task)
		{
			// TODO: NEED to add something to NOT scroll the window if the new
			// task is already showing in the window!
			
			// Make sure we've waited around for the new task to fully
			// be added to the TreeModel before continuing.  Some
			// backends might be threaded and will have used something
			// like Gtk.Idle.Add () to actually store the new ITask in
			// their TreeModel.
			while (Gtk.Application.EventsPending ())
				Gtk.Application.RunIteration ();
			
			Gtk.TreeIter iter;
			
			// Make sure we've waited around for the new task to fully
			// be added to the TreeModel before continuing.  Some
			// backends might be threaded and will have used something
			// like Gtk.Idle.Add () to actually store the new ITask in
			// their TreeModel.
			while (Gtk.Application.EventsPending ())
				Gtk.Application.RunIteration ();

			int taskGroupHeights = 0;
			
			foreach (TaskGroup taskGroup in taskGroups) {
				
				//Logger.Debug("taskGroupHeights: {0}", taskGroupHeights);
				TreePath start;
				TreePath end;
				if (taskGroup.TaskView.TreeView.GetVisibleRange (out start, out end)) {
					Logger.Debug ("TaskGroup '{0}' range: {1} - {2}",
						taskGroup.DisplayName,
						start.ToString (),
						end.ToString ());
				} else {
					Logger.Debug ("TaskGroup range not visible: {0}", taskGroup.DisplayName);
				}
				
				if (taskGroup.ContainsTask (task, out iter)) {
					Logger.Debug ("Found new task group: {0}", taskGroup.DisplayName);
					
					// Get the header height
					int headerHeight = taskGroup.HeaderHeight;
				
					// Get the total number items in the TaskGroup
					int nChildren = taskGroup.GetNChildren(iter);
					//Logger.Debug("n children: {0}", nChildren);

					// Calculate size of each item
					double itemSize = (double)(taskGroup.Requisition.Height-headerHeight) / nChildren;
					//Logger.Debug("item size: {0}", itemSize);
				
					// Get the index of the new item within the TaskGroup
					int newTaskIndex = taskGroup.GetIterIndex (iter);
					//Logger.Debug("new task index: {0}", newTaskIndex);
						
					// Calculate the scrolling distance
					double scrollDistance = (itemSize*newTaskIndex)+taskGroupHeights;
					//Logger.Debug("Scroll distance = ({0}*{1})+{2}+{3}: {4}", itemSize, newTaskIndex, taskGroupHeights, headerHeight, scrollDistance);
	
					//scroll to the new task
					scrolledWindow.Vadjustment.Value = scrollDistance;
					taskGroup.TaskView.TreeView.Selection.SelectIter (iter);
				}
				if (taskGroup.Visible) {
					taskGroupHeights += taskGroup.Requisition.Height;
				}
			}
		}
		#endregion // Public Methods
		
		#region Private Methods
		void TaskListComboBoxDataFunc (Gtk.CellLayout layout,
									   Gtk.CellRenderer renderer,
									   Gtk.TreeModel model,
									   Gtk.TreeIter iter)
		{
			Gtk.CellRendererText crt = renderer as Gtk.CellRendererText;
			ITaskList taskList = model.GetValue (iter, 0) as ITaskList;

			// CRG: What?  I added this check for null and we don't crash
			// but I never see anything called unknown
			if(taskList != null && taskList.Name != null) {
				crt.Text =
					string.Format ("{0} ({1})",
								   taskList.Name,
								   GetTaskCountInTaskList (taskList));
			} else
				crt.Text = "unknown";
		}
		
		// TODO: Move this method into a property of ITaskList.TaskCount
		private int GetTaskCountInTaskList (ITaskList taskList)
		{
			// This is disgustingly inefficient, but, oh well
			int count = 0;
			var model = application.BackendManager.Tasks;
			count = model.Count (t => t != null &&
			                     t.State == TaskState.Active &&
			                     taskList.Contains (t));
			return count;
		}
		
		/// <summary>
		/// Search through the TaskGroups looking for the specified task and:
		/// 1) scroll the window to its location, 2) enter directly into edit
		/// mode.  This method should be called right after a new task is
		/// created.
		/// </summary>
		/// <param name="task">
		/// A <see cref="ITask"/>
		/// </param>
		/// <param name="adjustScrolledWindow">
		/// A <see cref="bool"/> which indicates whether the task should be
		/// scrolled to.
		/// </param>
		private void EnterEditMode (ITask task, bool adjustScrolledWindow)
		{
			// Make sure we've waited around for the new task to fully
			// be added to the TreeModel before continuing.  Some
			// backends might be threaded and will have used something
			// like Gtk.Idle.Add () to actually store the new ITask in
			// their TreeModel.
			while (Gtk.Application.EventsPending ())
				Gtk.Application.RunIteration ();
			
			if (adjustScrolledWindow)
				ScrollToTask (task);
			
			
			Gtk.TreeIter iter;
			foreach (TaskGroup taskGroup in taskGroups) {
				if (taskGroup.ContainsTask (task, out iter)) {
					Logger.Debug ("Found new task group: {0}", taskGroup.DisplayName);
					
					// Get the header height
					taskGroup.EnterEditMode (task, iter);
					return;
				}
			}
		}
		
		private void RebuildAddTaskMenu (ICollection<ITaskList> taskListsModel)
		{
			Gtk.Menu menu = new Menu ();
			
			foreach (var cat in taskListsModel) {
				if (cat.ListType == TaskListType.Smart)
					continue;
				var item = new TaskListMenuItem (cat);
				item.Activated += OnNewTaskByTaskList;
				item.ShowAll ();
				menu.Add (item);
			}
			
			addTaskButton.Menu = menu;
		}
		
		private void SelectTaskList (string taskListName)
		{
			Gtk.TreeIter iter;
			Gtk.TreeModel model = taskListComboBox.Model;
			bool taskListWasSelected = false;

			if (taskListName != null) {
				// Iterate through (yeah, I know this is gross!) and find the
				// matching taskList
				if (model.GetIterFirst (out iter)) {
					do {
						ITaskList cat = model.GetValue (iter, 0) as ITaskList;
						if (cat == null)
							continue; // Needed for some reason to prevent crashes from some backends
						if (cat.Name.CompareTo (taskListName) == 0) {
							taskListComboBox.SetActiveIter (iter);
							taskListWasSelected = true;
							break;
						}
					} while (model.IterNext (ref iter));
				}
			}
			
			if (!taskListWasSelected) {
				// Select the first item in the list (which should be the "All"
				// taskList.
				if (model.GetIterFirst (out iter)) {
					// Make sure we can actually get a taskList
					ITaskList cat = model.GetValue (iter, 0) as ITaskList;
					if (cat != null)
						taskListComboBox.SetActiveIter (iter);
				}
			}
		}
		
		private void ShowTaskNotes (ITask task)
		{
			NoteDialog dialog = null;
			if (!noteDialogs.ContainsKey (task)) {
				dialog = new NoteDialog (this, task);
				dialog.Hidden += OnNoteDialogHidden;
				noteDialogs [task] = dialog;
			} else {
				dialog = noteDialogs [task];
			}
			
			if (!task.HasNotes) {
				dialog.CreateNewNote();
			}
			dialog.Present ();
		}
		
		private ITask CreateTask (string taskText, ITaskList taskList)
		{
			var task = taskList.CreateTask (taskText);
			
			if (task == null) {
				Logger.Debug ("Error creating a new task!");
				// Show error status
				status = Catalog.GetString ("Error creating a new task");
				TaskWindow.ShowStatus (status);
			} else {
				// Show successful status
				status = Catalog.GetString ("Task created successfully");
				TaskWindow.ShowStatus (status);
				// Clear out the entry
				addTaskEntry.Text = string.Empty;
				addTaskEntry.GrabFocus ();
			}
			
			return task;
		}
		
		/// <summary>
		/// This returns the current input widget color from the GTK theme
		/// </summary>
		/// <returns>
		/// A Gdk.Color
		/// </returns>
		private Gdk.Color GetBackgroundColor ()
		{
			using (Gtk.Style style = Gtk.Rc.GetStyle (this)) 
				return style.Base (StateType.Normal);
		}
		
		#endregion // Private Methods

		#region Event Handlers
		protected override void OnStyleSet (Gtk.Style previous_style)
		{
			base.OnStyleSet (previous_style);
			Gdk.Color backgroundColor = GetBackgroundColor ();
			innerEb.ModifyBg (StateType.Normal, backgroundColor);
			innerEb.ModifyBase (StateType.Normal, backgroundColor);
			
			if (addTaskEntry.Text == Catalog.GetString ("New task...")) {
				Gdk.Color insensitiveColor =
					addTaskEntry.Style.Text (Gtk.StateType.Insensitive);
				addTaskEntry.ModifyText (Gtk.StateType.Normal, insensitiveColor);
			}

		}
		
		private void OnRealized (object sender, EventArgs args)
		{
			addTaskEntry.GrabFocus ();
		}
		
		private void WindowDeleted (object sender, DeleteEventArgs args)
		{
			int x;
			int y;
			int width;
			int height;

			this.GetPosition(out x, out y);
			this.GetSize(out width, out height);

			lastXPos = x;
			lastYPos = y;
			
			application.Preferences.SetInt("MainWindowLastXPos", lastXPos);
			application.Preferences.SetInt("MainWindowLastYPos", lastYPos);
			application.Preferences.SetInt("MainWindowWidth", width);
			application.Preferences.SetInt("MainWindowHeight", height);

			Logger.Debug("WindowDeleted was called");
			taskWindow = null;
		}

		private void OnWindowShown (object sender, EventArgs args)
		{

		}
		
		void OnSettingChanged (IPreferences preferences, string settingKey)
		{
			if (settingKey.CompareTo (PreferencesKeys.HideInAllTaskList) != 0)
				return;
			
			OnTaskListChanged (this, EventArgs.Empty);
		}
		
		void OnGrabEntryFocus (object sender, EventArgs args)
		{
			addTaskEntry.GrabFocus ();
		}
		
		void OnAddTaskEntryFocused (object sender, EventArgs args)
		{
			// Clear the entry if it contains the default text
			if (addTaskEntry.Text == Catalog.GetString ("New task...")) {
				addTaskEntry.Text = string.Empty;
				addTaskEntry.ModifyText (Gtk.StateType.Normal);
			}
		}
		
		void OnAddTaskEntryUnfocused (object sender, EventArgs args)
		{
			// Restore the default text if nothing is entered
			if (addTaskEntry.Text == string.Empty) {
				addTaskEntry.Text = Catalog.GetString ("New task...");
				Gdk.Color insensitiveColor =
					addTaskEntry.Style.Text (Gtk.StateType.Insensitive);
				addTaskEntry.ModifyText (Gtk.StateType.Normal, insensitiveColor);
			}
		}
		
		void OnAddTaskEntryChanged (object sender, EventArgs args)
		{
			string text = addTaskEntry.Text.Trim ();
			if (text.Length == 0
					|| text.CompareTo (Catalog.GetString ("New task...")) == 0) {
				addTaskButton.Sensitive = false;
			} else {
				addTaskButton.Sensitive = true;
			}
		}
		
		void OnAddTaskEntryActivated (object sender, EventArgs args)
		{
			string newTaskText = addTaskEntry.Text.Trim ();
			if (newTaskText.Length == 0)
				return;
			
			OnAddTask (sender, args);
		}

		void OnAddTaskEntryDragDataReceived(object sender, DragDataReceivedArgs args)
		{
			// Change the text directly to the dropped text
			addTaskEntry.Text = args.SelectionData.Text;
			addTaskEntry.ModifyText (Gtk.StateType.Normal);
		}

		void OnAddTask (object sender, EventArgs args)
		{
			string enteredTaskText = addTaskEntry.Text.Trim ();
			if (enteredTaskText.Length == 0)
				return;
			
			Gtk.TreeIter iter;
			if (!taskListComboBox.GetActiveIter (out iter))
				return;
			
			ITaskList taskList =
				taskListComboBox.Model.GetValue (iter, 0) as ITaskList;
		
			// If enabled, attempt to parse due date information
			// out of the entered task text.
			DateTime taskDueDate = DateTime.MinValue;
			string taskName;
			if (application.Preferences.GetBool (PreferencesKeys.ParseDateEnabledKey))
				TaskParser.Instance.TryParse (
				                         enteredTaskText,
				                         out taskName,
				                         out taskDueDate);
			else
				taskName = enteredTaskText;
			
			ITask task = CreateTask (taskName, taskList);
			if (task == null)
				return; // TODO: Explain error to user!
			
			if (taskDueDate != DateTime.MinValue)
				task.DueDate = taskDueDate;
			
			HighlightTask (task);
		}
		
		void OnNewTaskByTaskList (object sender, EventArgs args)
		{
			string newTaskText = addTaskEntry.Text.Trim ();
			if (newTaskText.Length == 0)
				return;
			
			TaskListMenuItem item = sender as TaskListMenuItem;
			if (item == null)
				return;
			
			// Determine if the selected taskList is currently shown in the
			// task window.  If we're in a specific cateogory or on the All
			// taskList and the selected taskList is not showing, we've got
			// to switch the taskList first so the user will be able to edit
			// the title of the task.
			Gtk.TreeIter iter;
			if (taskListComboBox.GetActiveIter (out iter)) {
				ITaskList selectedTaskList =
					taskListComboBox.Model.GetValue (iter, 0) as ITaskList;
				
				// Check to see if "All" is selected
				if (selectedTaskList.ListType == TaskListType.Smart) {
					// See if the item.ITaskList is currently being shown in
					// the "All" taskList and if not, select the taskList
					// specifically.
					List<string> taskListsToHide =
						application.Preferences.GetStringList (
							PreferencesKeys.HideInAllTaskList);
					if (taskListsToHide != null && taskListsToHide.Contains (item.ITaskList.Name)) {
						SelectTaskList (item.ITaskList.Name);
					}
				} else if (selectedTaskList.Name.CompareTo (item.ITaskList.Name) != 0) {
					SelectTaskList (item.ITaskList.Name);
				}
			}
			
			ITask task = CreateTask (newTaskText, item.ITaskList);
			
			HighlightTask (task);
		}
		
		void OnTaskListChanged (object sender, EventArgs args)
		{
			Gtk.TreeIter iter;
			if (!taskListComboBox.GetActiveIter (out iter))
				return;
			
			ITaskList taskList =
				taskListComboBox.Model.GetValue (iter, 0) as ITaskList;
				
			// Update the TaskGroups so they can filter accordingly
			overdueGroup.Refilter (taskList);
			todayGroup.Refilter (taskList);
			tomorrowGroup.Refilter (taskList);
			nextSevenDaysGroup.Refilter (taskList);
			futureGroup.Refilter (taskList);
			completedTaskGroup.Refilter (taskList);
			
			// Save the selected taskList in preferences
			application.Preferences.Set (PreferencesKeys.SelectedTaskListKey,
										 taskList.Name);
		}
		
		void OnRowActivated (object sender, Gtk.RowActivatedArgs args)
		{
			// Check to see if a note dialog is already open for the activated
			// task.  If so, just bring it forward.  Otherwise, open a new one.
			Gtk.TreeView tv = sender as Gtk.TreeView;
			if (tv == null)
				return;
			
			Gtk.TreeModel model = tv.Model;
			
			Gtk.TreeIter iter;
			
			if (!model.GetIter (out iter, args.Path))
				return;
			
			ITask task = model.GetValue (iter, 0) as ITask;
			if (task == null)
				return;
			
			ShowTaskNotes (task);
		}
		

		[GLib.ConnectBefore]
		void OnButtonPressed (object sender, Gtk.ButtonPressEventArgs args)
		{
	        switch (args.Event.Button) {
	            case 3: // third mouse button (right-click)
		            clickedTask = null;

					Gtk.TreeView tv = sender as Gtk.TreeView;
					if (tv == null)
						return;
					
					Gtk.TreeModel model = tv.Model;
					
					Gtk.TreeIter iter;
					Gtk.TreePath path;
					Gtk.TreeViewColumn column = null;

					if (!tv.GetPathAtPos ((int) args.Event.X,
									(int) args.Event.Y, out path, out column))
						return;

					if (!model.GetIter (out iter, path))
						return;
					
					clickedTask = model.GetValue (iter, 0) as ITask;
					if (clickedTask == null)
						return;
					
					Menu popupMenu = new Menu ();
					ImageMenuItem item;
					
					item = new ImageMenuItem (Catalog.GetString ("_Notes..."));
					item.Image = new Gtk.Image (noteIcon);
					item.Activated += OnShowTaskNotes;
					popupMenu.Add (item);
					
					popupMenu.Add (new SeparatorMenuItem ());

					item = new ImageMenuItem (Catalog.GetString ("_Delete task"));
					item.Image = new Gtk.Image(Gtk.Stock.Delete, IconSize.Menu);
					item.Activated += OnDeleteTask;
					popupMenu.Add (item);

					item = new ImageMenuItem(Catalog.GetString ("_Edit task"));
					item.Image = new Gtk.Image(Gtk.Stock.Edit, IconSize.Menu);
					item.Activated += OnEditTask;
					popupMenu.Add (item);

					/*
					 * Depending on the currently selected task's taskList, we create a context popup
					 * here in order to enable changing taskLists. The list of available taskLists
					 * is pre-filtered as to not contain the current taskList and the AllTaskList.
					 */

				    var filteredTaskLists = new ListStore (typeof (ITaskList));
				    foreach (var cat in application.BackendManager.TaskLists) {
					    if (cat != null && !(cat.ListType == TaskListType.Smart)
					    && !cat.Contains (clickedTask))
						    filteredTaskLists.AppendValues (cat);
		        	}

					// The taskLists submenu is only created in case we actually provide at least one taskList.
					if (filteredTaskLists.GetIterFirst(out iter))
					{
						Menu taskListMenu = new Menu();
						TaskListMenuItem taskListItem;

						filteredTaskLists.Foreach(delegate(TreeModel t, TreePath p, TreeIter i) {
							taskListItem = new TaskListMenuItem((ITaskList)t.GetValue(i, 0));
							taskListItem.Activated += OnChangeTaskList;
							taskListMenu.Add(taskListItem);
							return false;
						});
					
						// TODO Needs translation.
						item = new ImageMenuItem(Catalog.GetString("_Change list"));
						item.Image = new Gtk.Image(Gtk.Stock.Convert, IconSize.Menu);
						item.Submenu = taskListMenu;
						popupMenu.Add(item);
					}
				
					popupMenu.ShowAll();
					popupMenu.Popup ();
				
					// Logger.Debug ("Right clicked on task: " + task.Name);
					break;
			}
		}
		
		void OnTargetVBoxButtonPress (object sender, Gtk.ButtonPressEventArgs args)
		{
			if (args.Event.Button == 1) {
				Gtk.TreeIter iter;
				if (!taskListComboBox.GetActiveIter (out iter))
					return;

				ITaskList taskList =
					taskListComboBox.Model.GetValue (iter, 0) as ITaskList;

				TaskView tree = futureGroup.TaskView as TaskView;

				// Don't add a new empty task if we're still editing a task
				if (tree.IsTaskBeingEdited)
					return;

				ITask task = CreateTask (String.Empty, taskList);
				if (task == null)
					return; // TODO: explain error to user

				// Since we added an empty task, it'll always be on top
				// Looks like a hack
				Gtk.TreePath path = new Gtk.TreePath ("0");
				tree.TreeView.SetCursor (path, tree.TreeView.GetColumn (2), true);
			}
		}

		private void OnShowTaskNotes (object sender, EventArgs args)
		{
			if (clickedTask == null)
				return;
			
			ShowTaskNotes (clickedTask);
		}
		
		private void OnDeleteTask (object sender, EventArgs args)
		{
			if (clickedTask == null)
				return;
		
			var taskList = application.BackendManager.TaskLists.First (
				l => !(l.ListType == TaskListType.Smart) && l.Contains (clickedTask));
			taskList.Remove (clickedTask);
			
			status = Catalog.GetString ("Task deleted");
			TaskWindow.ShowStatus (status);
		}


		private void OnEditTask (object sender, EventArgs args)
		{
			if (clickedTask == null)
				return;
			
			EnterEditMode (clickedTask, false);
		}
		
		
		void OnNoteDialogHidden (object sender, EventArgs args)
		{
			NoteDialog dialog = sender as NoteDialog;
			if (dialog == null) {
				Logger.Warn ("OnNoteDialogHidden (), sender is not NoteDialog, it's: {0}", sender.GetType ().ToString ());
				return;
			}
			
			if (!noteDialogs.ContainsKey (dialog.ITask)) {
				Logger.Warn ("Closed NoteDialog not found in noteDialogs");
				return;
			}
			
			Logger.Debug ("Removing NoteDialog from noteDialogs");
			noteDialogs.Remove (dialog.ITask);
			
			dialog.Destroy ();
		}
		
		private void OnBackendInitialized (object sender, EventArgs e)
		{
			PopulateWindow();
			string now = DateTime.Now.ToString ();
			// Translators: This status shows the date and time when the task list was last refreshed
			status = string.Format (Catalog.GetString ("Tasks loaded: {0}"), now);
			TaskWindow.lastLoadedTime = now;
			TaskWindow.ShowStatus (status);
			RebuildAddTaskMenu (application.BackendManager.TaskLists);
			addTaskEntry.Sensitive = true;
			taskListComboBox.Sensitive = true;
			// Keep insensitive text color
			Gdk.Color insensitiveColor =
				addTaskEntry.Style.Text (Gtk.StateType.Insensitive);
			addTaskEntry.ModifyText (Gtk.StateType.Normal, insensitiveColor);
		}

		void KeyPressed (object sender, Gtk.KeyPressEventArgs args)
		{
			args.RetVal = true;
			if (args.Event.Key == Gdk.Key.Escape) {
				if ((GdkWindow.State & Gdk.WindowState.Maximized) > 0)
					Unmaximize ();
				Hide ();
				return;
			}
			args.RetVal = false;
		}

		private void OnChangeTaskList(object sender, EventArgs args)
		{
			if (clickedTask == null)
				return;

			// NOTE: the previous data model had a one taskList to many tasks
			// relationship. Now it's many-to-many. However, we stick to the
			// old model until a general overhaul.
			var prevList = application.BackendManager.TaskLists.FirstOrDefault (
				c => !(c.ListType == TaskListType.Smart) && c.Contains (clickedTask));
			prevList.Remove (clickedTask);
			var list = ((TaskListMenuItem)sender).ITaskList;
			list.Add (clickedTask);
		}
		#endregion // Event Handlers
		
		#region Private Classes
		class TaskListMenuItem : Gtk.MenuItem
		{
			private ITaskList cat;
			
			public TaskListMenuItem (ITaskList taskList) : base (taskList.Name)
			{
				cat = taskList;
			}
			
			public ITaskList ITaskList
			{
				get { return cat; }
			}
		}
		#endregion // Private Classes
	}
	
	/// <summary>
	/// Provide keybindings via a fake Gtk.Menu.
	/// </summary>
	public class GlobalKeybinder
	{
		Gtk.AccelGroup accel_group;
		Gtk.Menu fake_menu;

		/// <summary>
		/// Create a global keybinder for the given Gtk.AccelGroup.
		/// </summary>
		/// </param>
		public GlobalKeybinder (Gtk.AccelGroup accel_group)
		{
			this.accel_group = accel_group;

			fake_menu = new Gtk.Menu ();
			fake_menu.AccelGroup = accel_group;
		}

		/// <summary>
		/// Add a keybinding for this keybinder's AccelGroup.
		/// </summary>
		/// <param name="handler">
		/// A <see cref="EventHandler"/> for when the keybinding is
		/// activated.
		/// </param>
		/// <param name="key">
		/// A <see cref="System.UInt32"/> specifying the key that will
		/// be bound (see the Gdk.Key enumeration for common values).
		/// </param>
		/// <param name="modifiers">
		/// The <see cref="Gdk.ModifierType"/> to be used on key
		/// for this binding.
		/// </param>
		/// <param name="flags">
		/// The <see cref="Gtk.AccelFlags"/> for this binding.
		/// </param>
		public void AddAccelerator (EventHandler handler,
		                            uint key,
		                            Gdk.ModifierType modifiers,
		                            Gtk.AccelFlags flags)
		{
			Gtk.MenuItem foo = new Gtk.MenuItem ();
			foo.Activated += handler;
			foo.AddAccelerator ("activate",
			                    accel_group,
			                    key,
			                    modifiers,
			                    flags);
			foo.Show ();

			fake_menu.Append (foo);
		}
	}
}
