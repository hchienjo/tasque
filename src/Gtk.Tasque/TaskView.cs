// TaskTreeView.cs created with MonoDevelop
// User: boyd on 2/9/2008

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Mono.Addins;
using Tasque;
using Gtk;
using Tasque.Core;

namespace Gtk.Tasque
{
	using TaskColNode = TypeExtensionNode<TaskColumnExtensionAttribute>;
	
	/// <summary>
	/// This is the main TreeView widget that is used to show tasks in Tasque's
	/// main window.
	/// </summary>
	public class TaskView
	{
		private Gtk.TreeModelFilter modelFilter;
		private ITaskList filterTaskList;
		
		public event EventHandler NumberOfTasksChanged;

		public TaskView (Gtk.TreeModel model, IPreferences preferences)
		{
			if (preferences == null)
				throw new ArgumentNullException ("preferences");
			
			TreeView = new TreeView ();
			
			#if GTK_2_12
			// set up the timing for the tooltips
			TreeView.Settings.SetLongProperty("gtk-tooltip-browse-mode-timeout", 0, "Tasque:TaskTreeView");
			TreeView.Settings.SetLongProperty("gtk-tooltip-browse-timeout", 750, "Tasque:TaskTreeView");
			TreeView.Settings.SetLongProperty("gtk-tooltip-timeout", 750, "Tasque:TaskTreeView");

			ConnectEvents();
			#endif
			
			// TODO: Modify the behavior of the TreeView so that it doesn't show
			// the highlighted row.  Then, also tie in with the mouse hovering
			// so that as you hover the mouse around, it will automatically
			// select the row that the mouse is hovered over.  By doing this,
			// we should be able to not require the user to click on a task
			// to select it and THEN have to click on the column item they want
			// to modify.
			
			filterTaskList = null;
			
			modelFilter = new Gtk.TreeModelFilter (model, null);
			modelFilter.VisibleFunc = FilterFunc;
			
			modelFilter.RowInserted += OnRowInsertedHandler;
			modelFilter.RowDeleted += OnRowDeletedHandler;
			Refilter ();
			
			//Model = modelFilter
			
			TreeView.Selection.Mode = Gtk.SelectionMode.Single;
			TreeView.RulesHint = false;
			TreeView.HeadersVisible = false;
			TreeView.HoverSelection = true;
			
			// TODO: Figure out how to turn off selection highlight
			
			columns = new List<ITaskColumn> ();
			var nodeList = AddinManager.GetExtensionNodes (typeof(ITaskColumn)).Cast<TaskColNode> ();
			var nodes = new List<TaskColNode> (nodeList);
			foreach (var node in nodes)
				AddColumn (node, nodes);
			
			rowEditingDictionary = new ConcurrentDictionary<ITaskColumn, TaskRowEditingEventArgs> ();
			columns.Sort ((x, y) => x.DefaultPosition.CompareTo (y.DefaultPosition));
			foreach (var col in columns) {
				col.Initialize (Model, this, preferences);
				
				col.CellEditingStarted += (sender, e) => {
					if (rowEditingDictionary.IsEmpty)
						IsTaskBeingEdited = true;
					
					if (!rowEditingDictionary.Any (v => v.Value.ITask == e.ITask)) {
						if (RowEditingStarted != null)
							RowEditingStarted (this, e);
					}
					rowEditingDictionary.TryAdd ((ITaskColumn)sender, e);
				};
				
				col.CellEditingFinished += (sender, e) => {
					TaskRowEditingEventArgs args;
					rowEditingDictionary.TryRemove ((ITaskColumn)sender, out args);
					if (!rowEditingDictionary.Any (v => v.Value.ITask == e.ITask)) {
						if (RowEditingFinished != null)
							RowEditingFinished (this, e);
					}
					
					if (rowEditingDictionary.IsEmpty)
						IsTaskBeingEdited = false;
				};
				
				TreeView.AppendColumn (col.TreeViewColumn);
			}
		}
		
		public bool IsTaskBeingEdited { get; private set; }
		
		public TreeModel Model { get { return TreeView.Model; } }
		
		public TreeView TreeView { get; private set; }

		#region Public Methods
		public void Refilter ()
		{
			Refilter (filterTaskList);
		}
		
		public void Refilter (ITaskList selectedTaskList)
		{
			this.filterTaskList = selectedTaskList;
			TreeView.Model = modelFilter;
			modelFilter.Refilter ();
		}
		
		public ITaskColumn GetColumn (Type taskColumnType)
		{
			return columns.SingleOrDefault (c => c.GetType () == taskColumnType);
		}
		#endregion // Public Methods
		
		public event EventHandler<TaskRowEditingEventArgs> RowEditingStarted;
		public event EventHandler<TaskRowEditingEventArgs> RowEditingFinished;
		
		#region Private Methods
		bool AddColumn (TaskColNode colNode, List<TaskColNode> nodes)
		{
			// if column is aready in collection, return
			if (columns.Any (i => i.GetType () == colNode.Type))
				return false;
			
			// if col has a col requirement, add it first recursively if it exists,
			// otherwise return false (as the requirement cannot be resolved)
			var reqColTypeName = colNode.Data.RequiredColumnTypeName;
			if (!string.IsNullOrWhiteSpace (reqColTypeName)) {
				var reqNode = nodes.SingleOrDefault (n => n.Type.Name == reqColTypeName);
				if (reqNode == null || !AddColumn (reqNode, nodes))
					return false;
			}
			
			// if col is replaced by another col, don't add it
			if (nodes.Any (n => n.Data.ReplacedColumnTypeName == colNode.Type.Name))
				return false;
			
			columns.Add ((ITaskColumn)colNode.CreateInstance ());
			return true;
		}
		
		#if GTK_2_12
		private void ConnectEvents()
		{
			TreeView.CursorChanged += delegate(object o, EventArgs args) {			
			int toolTipMaxLength = 250;
			string snipText = "...";
			int maxNumNotes = 3;
			int notesAdded = 0;
			TreeView.TooltipText = null;
			TreeView.TriggerTooltipQuery();
			TreeModel m;
			TreeIter iter;
			List<String> list = new List<String>();
	
			if(TreeView.Selection.GetSelected(out m, out iter)) {
				ITask task = Model.GetValue (iter, 0) as ITask;
				if (task != null && task.HasNotes && task.Notes != null) {
					foreach (var note in task.Notes) {
						// for the tooltip, truncate any notes longer than 250 characters.
						if (note.Text.Length > toolTipMaxLength)
							list.Add(note.Text.Substring(0, toolTipMaxLength - snipText.Length) + 
											snipText);
						else
							list.Add(note.Text);
						notesAdded++;
						// stop iterating once we reach maxNumNotes
						if (notesAdded >= maxNumNotes) {
							break;
						}
					}
				}			      		
		
				TreeView.HasTooltip = list.Count > 0;
				if (TreeView.HasTooltip) {
					// if there are more than maxNumNotes, append a notice to the tooltip
					if (notesAdded < task.Notes.Count) {
						int nMoreNotes = task.Notes.Count - notesAdded;
						if (nMoreNotes > 1)
							list.Add(String.Format("[{0} more notes]", nMoreNotes));
						else
							list.Add("[1 more note]");
					}
					TreeView.TooltipText = String.Join("\n\n", list.ToArray());
					TreeView.TriggerTooltipQuery();
				}
			}
			};
		}
		#endif
		
		protected virtual bool FilterFunc (Gtk.TreeModel model,
										   Gtk.TreeIter iter)
		{
			// Filter out deleted tasks
			ITask task = model.GetValue (iter, 0) as ITask;

			if (task == null) {
				Logger.Error ("FilterFunc: task at iter was null");
				return false;
			}
			
			if (task.State == TaskState.Discarded) {
				//Logger.Debug ("TaskTreeView.FilterFunc:\n\t{0}\n\t{1}\n\tReturning false", task.Name, task.State);  
				return false;
			}
			
			if (filterTaskList == null)
				return true;
			
			return filterTaskList.Contains (task);
		}
		#endregion // Private Methods
		
		#region EventHandlers
		void OnRowInsertedHandler (object sender, Gtk.RowInsertedArgs args)
		{
			if (NumberOfTasksChanged == null)
				return;
			
			NumberOfTasksChanged (this, EventArgs.Empty);
		}
		
		void OnRowDeletedHandler (object sender, Gtk.RowDeletedArgs args)
		{
			if (NumberOfTasksChanged == null)
				return;
			
			NumberOfTasksChanged (this, EventArgs.Empty);
		}
		#endregion // EventHandlers
		
		List<ITaskColumn> columns;
		ConcurrentDictionary<ITaskColumn, TaskRowEditingEventArgs> rowEditingDictionary;
	}
}
