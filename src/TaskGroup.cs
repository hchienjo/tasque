// TaskGroup.cs created with MonoDevelop
// User: boyd at 7:50 PM 2/11/2008

using System;

namespace Tasque
{
	/// <summary>
	/// A TaskGroup is a Widget that represents a grouping of tasks that
	/// are shown in the TaskWindow.  For example, "Overdue", "Today",
	/// "Tomorrow", etc.
	/// </summary>
	public class TaskGroup : Gtk.VBox
	{
		DateTime timeRangeStart;
		DateTime timeRangeEnd;
		
		Gtk.Label header;
		TaskTreeView treeView;
		Gtk.TreeModelFilter filteredTasks;
		Gtk.HBox extraWidgetHBox;
		Gtk.Widget extraWidget;
		
		bool hideWhenEmpty;
		
		protected bool showCompletedTasks;
		
		#region Constructor
		public TaskGroup (string groupName, DateTime rangeStart,
						  DateTime rangeEnd, Gtk.TreeModel tasks)
		{
			this.timeRangeStart = rangeStart;
			this.timeRangeEnd = rangeEnd;
			
			hideWhenEmpty = true;
						
			showCompletedTasks = 
				Application.Preferences.GetBool (
					Preferences.ShowCompletedTasksKey);
			Application.Preferences.SettingChanged += OnSettingChanged;
			
			// TODO: Add a date time event watcher so that when we rollover to
			// a new day, we can update the rangeStart and rangeEnd times.  The
			// ranges will be used to determine whether tasks fit into certain
			// groups in the main TaskWindow.  Reference Tomboy's NoteOfTheDay
			// add-in for code that reacts on day changes.
			
			filteredTasks = new Gtk.TreeModelFilter (tasks, null);
			filteredTasks.VisibleFunc = FilterTasks;
			
			// TODO: Add something to watch events so that the group will
			// automatically refilter and display/hide itself accordingly.
			
			//
			// Build the UI
			//
			
			//
			// Group Header
			//
//			Gtk.EventBox eb = new Gtk.EventBox();
//			eb.Show();
//			eb.BorderWidth = 0;
//			eb.ModifyBg(Gtk.StateType.Normal, new Gdk.Color(211,215,199));
//			eb.ModifyBase(Gtk.StateType.Normal, new Gdk.Color(211,215,199));
			Gtk.HBox headerHBox = new Gtk.HBox (false, 0);

			header = new Gtk.Label ();
			header.UseMarkup = true;
			header.UseUnderline = false;
			header.Markup =
				string.Format ("<span size=\"x-large\" foreground=\"#9eb96e\" weight=\"bold\">{0}</span>",
							   groupName);
			header.Xalign = 0;
			
			header.Show ();
			
//			eb.Add(header);
//			PackStart (eb, false, false, 0);
			headerHBox.PackStart (header, false, false, 0);
			
			// spacer
			Gtk.Label spacerLabel = new Gtk.Label (string.Empty);
			spacerLabel.Show ();
			headerHBox.PackStart (spacerLabel, true, true, 0);
			
			extraWidgetHBox = new Gtk.HBox (false, 0);
			extraWidgetHBox.Show ();
			headerHBox.PackStart (extraWidgetHBox, false, false, 0);
			headerHBox.Show ();
			PackStart (headerHBox, false, false, 5);
			
			//
			// Group TreeView
			//
			treeView = new TaskTreeView (filteredTasks);
			treeView.Show ();
			PackStart (treeView, true, true, 0);
			
			treeView.NumberOfTasksChanged += OnNumberOfTasksChanged;
			treeView.RowActivated += OnRowActivated;
			treeView.ButtonPressEvent += OnButtonPressed;
		}
		#endregion // Constructor
		
		#region Events
		public event Gtk.RowActivatedHandler RowActivated;
		public event Gtk.ButtonPressEventHandler ButtonPressed;
		#endregion // Events
		
		#region Public Properties
		public string DisplayName
		{
			get { return header.Text; }
		}
		
		public int HeaderHeight
		{
			get { return header.Requisition.Height; }
		}
		
		/// <value>
		/// Use this to set an Extra Widget.  The extra widget will be placed
		/// on the right-hand side of the TaskGroup header.
		/// </value>
		public Gtk.Widget ExtraWidget
		{
			get { return extraWidget; }
			set {
				// Remove and destroy an existing extraWidget
				if (extraWidget != null) {
					extraWidgetHBox.Remove (extraWidget);
					extraWidget.Destroy ();
					extraWidget = null;
				}
				
				extraWidget = value;
				
				if (extraWidget == null)
					return;
				
				extraWidget.Show ();
				extraWidgetHBox.PackStart (extraWidget, true, true, 0);
			}
		}
		
		/// <value>
		/// If true, the entire task group will automatically hide itself when
		/// there are no tasks to show.  Default value is true.  Special task
		/// groups like CompletedTaskGroup may want to set this to false so the
		/// range slider (HScale) widget can control how many completed tasks to
		/// show.
		/// </value>
		public bool HideWhenEmpty
		{
			get { return hideWhenEmpty; }
			set {
				if (hideWhenEmpty == value)
					return; // Don't do anything if the values are the same
				
				hideWhenEmpty = value;
			}
		}
		
		/// <value>
		/// Get and set the minimum date for the group
		/// </value>
		public DateTime TimeRangeStart
		{
			get { return timeRangeStart; }
			set {
				if (value == timeRangeStart)
					return;
				
				timeRangeStart = value;
				Refilter ();
			}
		}
		
		/// <value>
		/// Get and set the maxiumum date for the group
		/// </value>
		public DateTime TimeRangeEnd
		{
			get { return timeRangeEnd; }
			set {
				if (value == timeRangeEnd)
					return;
				
				timeRangeEnd = value;
				Refilter ();
			}
		}
		
		public Gtk.TreeView TreeView
		{
			get { return this.treeView; }
		}
		#endregion // Public Properties
		
		#region Public Methods
		public void Refilter (Category selectedCategory)
		{
			filteredTasks.Refilter ();
			treeView.Refilter (selectedCategory);
		}
		
		/// <summary>
		/// Convenience method to determine whether the specified task is
		/// currently shown in this TaskGroup.
		/// </summary>
		/// <param name="task">
		/// A <see cref="Task"/>
		/// </param>
		/// <param name="iter">
		/// A <see cref="Gtk.TreeIter"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/> True if the specified <see
		/// cref="Task">task</see> is currently shown inside this TaskGroup.
		/// Additionally, if true, the <see cref="Gtk.TreeIter">iter</see> will
		/// point to the specified <see cref="Task">task</see>.
		/// </returns>
		public bool ContainsTask (Task task, out Gtk.TreeIter iter)
		{
			Gtk.TreeIter tempIter;
			Gtk.TreeModel model = treeView.Model;
			
			iter = Gtk.TreeIter.Zero;
			
			if (model.GetIterFirst (out tempIter) == false)
				return false;
			
			// Loop through the model looking for a matching task
			do {
				Task tempTask = model.GetValue (tempIter, 0) as Task;
				if (tempTask == task) {
					iter = tempIter;
					return true;
				}
			} while (model.IterNext (ref tempIter) == true);
			
			return false;
		}
		
		public int GetNChildren(Gtk.TreeIter iter)
		{
			return treeView.Model.IterNChildren();
		}
			
		// Find the index within the tree
		public int GetIterIndex (Gtk.TreeIter iter)
		{
			Gtk.TreePath path = treeView.Model.GetPath (iter);
			Gdk.Rectangle rect =
				treeView.GetBackgroundArea (path, treeView.GetColumn (0));
			
			int pos = 0;
			Gtk.TreeIter tempIter;
			Gtk.TreeModel model = treeView.Model;
			Task task = model.GetValue (iter, 0) as Task;
			
			if (model.GetIterFirst (out tempIter) == false)
				return 0;
			
			// This is ugly, but figure out what position the specified iter is
			// at so we can return a value accordingly.
			do {
				Task tempTask = model.GetValue (tempIter, 0) as Task;
				if (tempTask == task)
					break;
				
				pos++;
			} while (model.IterNext (ref tempIter) == true);
			
			return pos;
		}
		
		// Find the height position within the tree
		public int GetIterPos (Gtk.TreeIter iter)
		{
			Gtk.TreePath path = treeView.Model.GetPath (iter);
			Gdk.Rectangle rect =
				treeView.GetBackgroundArea (path, treeView.GetColumn (0));
			int height = rect.Height;
			
			int pos = 0;
			Gtk.TreeIter tempIter;
			Gtk.TreeModel model = treeView.Model;
			Task task = model.GetValue (iter, 0) as Task;
			
			if (model.GetIterFirst (out tempIter) == false)
				return 0;
			
			// This is ugly, but figure out what position the specified iter is
			// at so we can return a value accordingly.
			do {
				Task tempTask = model.GetValue (tempIter, 0) as Task;
				if (tempTask == task)
					break;
				
				pos++;
			} while (model.IterNext (ref tempIter) == true);

//Logger.Debug ("pos: {0}", pos);
//Logger.Debug ("height: {0}", height);			
//Logger.Debug ("returning: {0}", pos * height + header.Requisition.Height + 10);
			// + 10 is for the spacing added on when packing the header
			return pos * height + header.Requisition.Height;
		}
		
		public void EnterEditMode (Task task, Gtk.TreeIter iter)
		{
			Gtk.TreePath path;
			
			// Select the iter and go into editing mode on the task name
			
			// TODO: Figure out a way to NOT hard-code the column number
			Gtk.TreeViewColumn nameColumn = treeView.Columns [2];
			Gtk.CellRendererText nameCellRendererText =
				nameColumn.CellRenderers [0] as Gtk.CellRendererText;
			path = treeView.Model.GetPath (iter);
			
			treeView.Model.IterNChildren();
				
			treeView.SetCursorOnCell (path, nameColumn, nameCellRendererText, true);
		}
		#endregion // Methods
		
		#region Private Methods
		protected override void OnRealized ()
		{
			base.OnRealized ();
			
			if (treeView.GetNumberOfTasks () == 0
					&& (showCompletedTasks == false || hideWhenEmpty == true))
				Hide ();
			else
				Show ();
		}

        /// <summary>
        /// Filter out tasks that don't fit within the group's date range
        /// </summary>
		protected virtual bool FilterTasks (Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			Task task = model.GetValue (iter, 0) as Task;
			if (task == null)
				return false;
			
			// Do something special when task.DueDate == DateTime.MinValue since
			// these tasks should always be in the very last category.
			if (task.DueDate == DateTime.MinValue) {
				if (timeRangeEnd == DateTime.MaxValue) {
					if (ShowCompletedTask (task) == false)
						return false;
					
					return true;
				} else {
					return false;
				}
			}
			
			if (task.DueDate < timeRangeStart || task.DueDate > timeRangeEnd)
				return false;
			
			if (ShowCompletedTask (task) == false)
				return false;
			
			return true;
		}
		
		private bool ShowCompletedTask (Task task)
		{
			if (task.State == TaskState.Completed) {
				if (showCompletedTasks == false)
					return false;
				
				// Only show completed tasks that are from "Today".  Once it's
				// tomorrow, don't show completed tasks in this group and
				// instead, show them in the Completed Tasks Group.
				if (task.CompletionDate == DateTime.MinValue)
					return false; // Just in case
				
				if (IsToday (task.CompletionDate) == false)
					return false;
			}
			
			return true;
		}
		
		private bool IsToday (DateTime testDate)
		{
			DateTime today = DateTime.Now;
			if (today.Year != testDate.Year
					|| today.DayOfYear != testDate.DayOfYear)
				return false;
			
			return true;
		}
		
		/// <summary>
		/// Refilter the hard way by discovering the category to filter on
		/// </summary>
		private void Refilter ()
		{
			Category cat = GetSelectedCategory ();
			if (cat != null)
				Refilter (cat);
		}
		
		/// <summary>
		/// This returns the currently selected category.
		/// TODO: This should really be moved as a method Application or
		/// or something.
		/// </summary>
		/// <returns>
		/// A <see cref="Category"/>
		/// </returns>
		private Category GetSelectedCategory ()
		{
			// TODO: Move this code into some function in the backend/somewhere
			// with the signature of GetCategoryForName (string catName):Category
			string selectedCategoryName =
				Application.Preferences.Get (Preferences.SelectedCategoryKey);
			
			if (selectedCategoryName != null) {
				Gtk.TreeIter iter;
				Gtk.TreeModel model = Application.LocalCache.Categories;

				// Iterate through (yeah, I know this is gross!) and find the
				// matching category
				if (model.GetIterFirst (out iter) == true) {
					do {
						Category cat = model.GetValue (iter, 0) as Category;
						if (cat == null)
							continue; // Needed for some reason to prevent crashes from some backends
						if (cat.Name.CompareTo (selectedCategoryName) == 0) {
							return cat;
						}
					} while (model.IterNext (ref iter) == true);
				}
			}
			
			return null;
		}
		#endregion // Private Methods
		
		#region Event Handlers
		void OnNumberOfTasksChanged (object sender, EventArgs args)
		{
			//Logger.Debug ("TaskGroup (\"{0}\").OnNumberOfTasksChanged ()", DisplayName);
			// Check to see whether this group should be hidden or shown.
			if (treeView.GetNumberOfTasks () == 0
					&& (showCompletedTasks == false || hideWhenEmpty == true))
				Hide ();
			else
				Show ();
		}
		
		void OnRowActivated (object sender, Gtk.RowActivatedArgs args)
		{
			// Pass this on to the TaskWindow
			if (RowActivated != null)
				RowActivated (sender, args);
		}
		
		[GLib.ConnectBefore]
		void OnButtonPressed (object sender, Gtk.ButtonPressEventArgs args)
		{
			// Pass this on to the TaskWindow
			if (ButtonPressed != null)
				ButtonPressed (sender, args);
		}
		
		protected void OnSettingChanged (Preferences preferences,
										 string settingKey)
		{
			if (settingKey.CompareTo (Preferences.ShowCompletedTasksKey) != 0)
				return;
			
			bool newValue =
				preferences.GetBool (Preferences.ShowCompletedTasksKey);
			if (showCompletedTasks == newValue)
				return; // don't do anything if nothing has changed
			
			showCompletedTasks = newValue;
			
			Category cat = GetSelectedCategory ();
			if (cat != null)
				Refilter (cat);
		}

		#endregion // Event Handlers
	}
}
