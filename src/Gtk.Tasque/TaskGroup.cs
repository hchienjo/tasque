// TaskGroup.cs created with MonoDevelop
// User: boyd at 7:50 PM 2/11/2008

using System;
using System.Collections.Generic;
using System.Linq;
using Gdk;
using Gtk;
using Gtk.Tasque;
using Tasque.Core;
using Tasque.Utils;

namespace Tasque
{
	/// <summary>
	/// A TaskGroup is a Widget that represents a grouping of tasks that
	/// are shown in the TaskWindow.  For example, "Overdue", "Today",
	/// "Tomorrow", etc.
	/// </summary>
	public class TaskGroup : Gtk.VBox
	{
		Gtk.Label header;
		TaskView taskView;
		TreeModel treeModel;
		Gtk.HBox extraWidgetHBox;
		Gtk.Widget extraWidget;
		
		bool hideWhenEmpty;
		
		#region Constructor
		public TaskGroup (string groupName, DateTime rangeStart,
		                  DateTime rangeEnd, ICollection<ITask> tasks, GtkApplicationBase application)
		{
			if (application == null)
				throw new ArgumentNullException ("application");
			Application = application;

			hideWhenEmpty = true;
						
			// TODO: Add a date time event watcher so that when we rollover to
			// a new day, we can update the rangeStart and rangeEnd times.  The
			// ranges will be used to determine whether tasks fit into certain
			// groups in the main TaskWindow.  Reference Tomboy's NoteOfTheDay
			// add-in for code that reacts on day changes.

			treeModel = CreateModel (rangeStart, rangeEnd, tasks);
			
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
			header.Markup = GetHeaderMarkup (groupName);
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
			taskView = new TaskView (treeModel, application.Preferences);
			taskView.TreeView.Show ();
			PackStart (taskView.TreeView, true, true, 0);
			
			taskView.NumberOfTasksChanged += OnNumberOfTasksChanged;
			taskView.TreeView.RowActivated += OnRowActivated;
			taskView.TreeView.ButtonPressEvent += OnButtonPressed;
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
			get { return Model.TimeRangeStart; }
			set {
				if (value == Model.TimeRangeStart)
					return;
				
				Model.SetRange (value, Model.TimeRangeEnd);
				Refilter ();
			}
		}
		
		/// <value>
		/// Get and set the maxiumum date for the group
		/// </value>
		public DateTime TimeRangeEnd
		{
			get { return Model.TimeRangeEnd; }
			set {
				if (value == Model.TimeRangeEnd)
					return;
				
				Model.SetRange (Model.TimeRangeStart, value);
				Refilter ();
			}
		}
		
		public TaskView TaskView
		{
			get { return this.taskView; }
		}
		#endregion // Public Properties
		
		#region Public Methods
		public void Refilter (ITaskList selectedTaskList)
		{
			taskView.Refilter (selectedTaskList);
		}
		
		/// <summary>
		/// Convenience method to determine whether the specified task is
		/// currently shown in this TaskGroup.
		/// </summary>
		/// <param name="task">
		/// A <see cref="ITask"/>
		/// </param>
		/// <param name="iter">
		/// A <see cref="Gtk.TreeIter"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/> True if the specified <see
		/// cref="ITask">task</see> is currently shown inside this TaskGroup.
		/// Additionally, if true, the <see cref="Gtk.TreeIter">iter</see> will
		/// point to the specified <see cref="ITask">task</see>.
		/// </returns>
		public bool ContainsTask (ITask task, out Gtk.TreeIter iter)
		{
			Gtk.TreeIter tempIter;
			Gtk.TreeModel model = taskView.Model;
			
			iter = Gtk.TreeIter.Zero;
			
			if (!model.GetIterFirst (out tempIter))
				return false;
			
			// Loop through the model looking for a matching task
			do {
				ITask tempTask = model.GetValue (tempIter, 0) as ITask;
				if (tempTask == task) {
					iter = tempIter;
					return true;
				}
			} while (model.IterNext (ref tempIter));
			
			return false;
		}
		
		public int GetNChildren(Gtk.TreeIter iter)
		{
			return taskView.Model.IterNChildren();
		}
			
		// Find the index within the tree
		public int GetIterIndex (Gtk.TreeIter iter)
		{
			Gtk.TreePath path = taskView.Model.GetPath (iter);
			Gdk.Rectangle rect =
				taskView.TreeView.GetBackgroundArea (path, taskView.TreeView.GetColumn (0));
			
			int pos = 0;
			Gtk.TreeIter tempIter;
			Gtk.TreeModel model = taskView.Model;
			ITask task = model.GetValue (iter, 0) as ITask;
			
			if (!model.GetIterFirst (out tempIter))
				return 0;
			
			// This is ugly, but figure out what position the specified iter is
			// at so we can return a value accordingly.
			do {
				ITask tempTask = model.GetValue (tempIter, 0) as ITask;
				if (tempTask == task)
					break;
				
				pos++;
			} while (model.IterNext (ref tempIter));
			
			return pos;
		}
		
		// Find the height position within the tree
		public int GetIterPos (Gtk.TreeIter iter)
		{
			Gtk.TreePath path = taskView.Model.GetPath (iter);
			Gdk.Rectangle rect =
				taskView.TreeView.GetBackgroundArea (path, taskView.TreeView.GetColumn (0));
			int height = rect.Height;
			
			int pos = 0;
			Gtk.TreeIter tempIter;
			Gtk.TreeModel model = taskView.Model;
			ITask task = model.GetValue (iter, 0) as ITask;
			
			if (!model.GetIterFirst (out tempIter))
				return 0;
			
			// This is ugly, but figure out what position the specified iter is
			// at so we can return a value accordingly.
			do {
				ITask tempTask = model.GetValue (tempIter, 0) as ITask;
				if (tempTask == task)
					break;
				
				pos++;
			} while (model.IterNext (ref tempIter));

//Logger.Debug ("pos: {0}", pos);
//Logger.Debug ("height: {0}", height);			
//Logger.Debug ("returning: {0}", pos * height + header.Requisition.Height + 10);
			// + 10 is for the spacing added on when packing the header
			return pos * height + header.Requisition.Height;
		}
		
		public void EnterEditMode (ITask task, Gtk.TreeIter iter)
		{
			Gtk.TreePath path;
			
			// Select the iter and go into editing mode on the task name
			
			// TODO: Figure out a way to NOT hard-code the column number
			Gtk.TreeViewColumn nameColumn = taskView.TreeView.Columns [2];
			Gtk.CellRendererText nameCellRendererText =
				nameColumn.CellRenderers [0] as Gtk.CellRendererText;
			path = taskView.Model.GetPath (iter);
			
			taskView.Model.IterNChildren();
				
			taskView.TreeView.SetCursorOnCell (path, nameColumn, nameCellRendererText, true);
		}
		#endregion // Methods
		
		#region Private Methods
		protected GtkApplicationBase Application { get; private set; }

		protected TaskGroupModel Model { get; set; }

		protected override void OnRealized ()
		{
			base.OnRealized ();
			
			if (!Model.Any () && hideWhenEmpty)
				Hide ();
			else
				Show ();
		}

		protected override void OnStyleSet(Style previous_style)
		{
			base.OnStyleSet (previous_style);
			header.Markup = GetHeaderMarkup (DisplayName);
		}

		protected virtual TreeModel CreateModel (DateTime rangeStart,
		                                         DateTime rangeEnd,
		                                         ICollection<ITask> tasks)
		{
			Model = new TaskGroupModel (rangeStart, rangeEnd,
			                            tasks, Application.Preferences);
			return new TreeModelListAdapter<ITask> (Model);
		}
		
		/// <summary>
		/// Refilter the hard way by discovering the taskList to filter on
		/// </summary>
		private void Refilter ()
		{
			ITaskList cat = GetSelectedTaskList ();
			if (cat != null)
				Refilter (cat);
		}
		
		/// <summary>
		/// This returns the currently selected taskList.
		/// TODO: This should really be moved as a method Application or
		/// or something.
		/// </summary>
		/// <returns>
		/// A <see cref="ITaskList"/>
		/// </returns>
		private ITaskList GetSelectedTaskList ()
		{
			// TODO: Move this code into some function in the backend/somewhere
			// with the signature of GetTaskListForName (string catName):ITaskList
			string selectedTaskListName =
				Application.Preferences.Get (PreferencesKeys.SelectedTaskListKey);
			
			ITaskList taskList = null;
			if (selectedTaskListName != null) {
				var model = Application.BackendManager.TaskLists;
				taskList = model.FirstOrDefault (c => c != null && c.Name == selectedTaskListName);
			}
			
			return taskList;
		}
		
		/// <summary>
		/// This returns the current highlight color from the GTK theme
		/// </summary>
		/// <returns>
		/// An hexadecimal color string (ex #ffffff)
		/// </returns>
		private string GetHighlightColor ()
		{
			Gdk.Color fgColor;

			using (Gtk.Style style = Gtk.Rc.GetStyle (this)) 
				fgColor = style.Backgrounds [(int) StateType.Selected];

			return Utilities.ColorGetHex (fgColor);
		}

		private string GetHeaderMarkup (string groupName)
		{
			return string.Format ("<span size=\"x-large\" foreground=\"{0}\" weight=\"bold\">{1}</span>",
			                      GetHighlightColor (),
			                      groupName);
		}
		
		#endregion // Private Methods
		
		#region Event Handlers
		void OnNumberOfTasksChanged (object sender, EventArgs args)
		{
			//Logger.Debug ("TaskGroup (\"{0}\").OnNumberOfTasksChanged ()", DisplayName);
			// Check to see whether this group should be hidden or shown.
			if (!Model.Any () && hideWhenEmpty)
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

		#endregion // Event Handlers
	}
}
