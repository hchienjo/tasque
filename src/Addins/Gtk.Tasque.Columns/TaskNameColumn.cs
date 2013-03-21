//
// NameColumn.cs
//
// Author:
//       Antonius Riha <antoniusriha@gmail.com>
//
// Copyright (c) 2013 Antonius Riha
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
using Mono.Unix;
using Tasque;
using GLib;
using Tasque.Core;
using Tasque.DateFormatters;

namespace Gtk.Tasque
{
	// TODO: Use xml addin description model to provide localized column name
	[TaskColumnExtension ("Task Name")]
	public class TaskNameColumn : ITaskColumn
	{
		public TaskNameColumn ()
		{
			TreeViewColumn = new TreeViewColumn {
				Title = Catalog.GetString ("Task Name"),
				Sizing = TreeViewColumnSizing.Autosize,
				Expand = true,
				Resizable = true
			};
			
			// TODO: Add in code to determine how wide we should make the name
			// column.
			// TODO: Add in code to readjust the size of the name column if the
			// user resizes the Task Window.
			//column.FixedWidth = 250;
			
			var renderer = new CellRendererText { Editable = true };

			renderer.EditingStarted += (o, args) => {
				TreeIter iter;
				var path = new TreePath (args.Path);
				if (!model.GetIter (out iter, path))
					return;
				
				var task = model.GetValue (iter, 0) as ITask;
				if (task == null)
					return;
				
				taskBeingEdited = new TaskBeingEdited (task, iter, path);
				if (CellEditingStarted != null)
					CellEditingStarted (this, new TaskRowEditingEventArgs (task, iter, path));
			};
			
			renderer.EditingCanceled += delegate { EndCellEditing (); };
			
			renderer.Edited += (o, args) => {
				TreeIter iter;
				var path = new TreePath (args.Path);
				if (model.GetIter (out iter, path)) {
					var task = model.GetValue (iter, 0) as ITask;
					if (task == null)
						return;
					
					var newText = args.NewText;
					// Attempt to derive due date information from text.
					if (preferences.GetBool (PreferencesKeys.ParseDateEnabledKey) &&
					    task.State == TaskState.Active &&
					    task.DueDate == DateTime.MinValue) {
						
						string parsedTaskText;
						DateTime parsedDueDate;
						TaskParser.Instance.TryParse (newText, out parsedTaskText, out parsedDueDate);
						
						if (parsedDueDate != DateTime.MinValue)
							task.DueDate = parsedDueDate;
						newText = parsedTaskText;
					}
					
					task.Text = newText;
				}
				EndCellEditing ();
			};
			
			TreeViewColumn.PackStart (renderer, true);
			TreeViewColumn.SetCellDataFunc (renderer, TaskNameTextCellDataFunc);
		}

		public int DefaultPosition { get { return 2; } }

		public TreeViewColumn TreeViewColumn { get; private set; }
		
		public void Initialize (TreeModel model, TaskView view, IPreferences preferences)
		{
			if (preferences == null)
				throw new ArgumentNullException ("preferences");
			this.preferences = preferences;
			if (model == null)
				throw new ArgumentNullException ("model");
			this.model = model;
		}
		
		public event EventHandler<TaskRowEditingEventArgs> CellEditingStarted;
		
		public event EventHandler<TaskRowEditingEventArgs> CellEditingFinished;

		void EndCellEditing ()
		{
			if (taskBeingEdited == null)
				return;
			
			if (CellEditingFinished != null)
				CellEditingFinished (this, new TaskRowEditingEventArgs (taskBeingEdited.Task,
				                                                        taskBeingEdited.Iter,
				                                                        taskBeingEdited.Path));
			taskBeingEdited = null;
		}
		
		void TaskNameTextCellDataFunc (TreeViewColumn treeColumn, CellRenderer cell,
		                               TreeModel treeModel, TreeIter iter)
		{
			var crt = cell as CellRendererText;
			crt.Ellipsize = Pango.EllipsizeMode.End;
			var task = model.GetValue (iter, 0) as ITask;
			if (task == null) {
				crt.Text = string.Empty;
				return;
			}
			
			var formatString = "{0}";
			var todayTasksColor = preferences.Get (PreferencesKeys.TodayTaskTextColor);
			var overdueTaskColor = preferences.Get (PreferencesKeys.OverdueTaskTextColor);
			
			if (!task.IsComplete && task.DueDate.Date == DateTime.Today.Date)
				crt.Foreground = todayTasksColor;
			// Overdue and the task has a date assigned to it.
			else if (!task.IsComplete && task.DueDate < DateTime.Today
			         && task.DueDate != DateTime.MinValue)
				crt.Foreground = overdueTaskColor;
			
			switch (task.State) {
			// TODO: Reimplement the feature below
//			case TaskState.Active:
//				// Strikeout the text
//				var timer = timerCol.GetTimer (task);
//				if (timer != null && timer.State == TaskCompleteTimerState.Running)
//					formatString = "<span strikethrough=\"true\">{0}</span>";
//				break;
			case TaskState.Discarded:
			case TaskState.Completed:
				// Gray out the text and add strikeout
				// TODO: Determine the grayed-out text color appropriate for the current theme
				formatString = "<span strikethrough=\"true\">{0}</span>";
				crt.Foreground = "#AAAAAA";
				break;
			}
			
			crt.Markup = string.Format (formatString, Markup.EscapeText (task.Text));
		}

		TreeModel model;
		IPreferences preferences;
		TaskBeingEdited taskBeingEdited;
	}
}
