//
// PriorityColumn.cs
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
using Tasque.Core;

namespace Gtk.Tasque
{
	// TODO: Use xml addin description model to provide localized column name
	[TaskColumnExtension ("Priority")]
	public class PriorityColumn : ITaskColumn
	{
		public PriorityColumn ()
		{
			TreeViewColumn = new TreeViewColumn {
				Title = Catalog.GetString ("Priority"),
				Sizing = TreeViewColumnSizing.Fixed,
				Alignment = 0.5f,
				FixedWidth = 30,
				Resizable = false,
				Clickable = true
			};
			
			var priorityStore = new ListStore (typeof (string));
			priorityStore.AppendValues (Catalog.GetString ("1")); // High
			priorityStore.AppendValues (Catalog.GetString ("2")); // Medium
			priorityStore.AppendValues (Catalog.GetString ("3")); // Low
			priorityStore.AppendValues (Catalog.GetString ("-")); // None
			
			var renderer = new CellRendererCombo {
				Editable = true,
				HasEntry = false,
				Model = priorityStore,
				TextColumn = 0,
				Xalign = 0.5f
			};
			
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
					TaskPriority newPriority;
					var newText = args.NewText;
					if (newText == Catalog.GetString ("3"))
						newPriority = TaskPriority.Low;
					else if (newText == Catalog.GetString ("2"))
						newPriority = TaskPriority.Medium;
					else if (newText == Catalog.GetString ("1"))
						newPriority = TaskPriority.High;
					else
						newPriority = TaskPriority.None;
				
					// Update the priority if it's different
					var task = model.GetValue (iter, 0) as ITask;
					if (task.Priority != newPriority)
						task.Priority = newPriority;
				}
				EndCellEditing ();
			};

			TreeViewColumn.PackStart (renderer, true);
			TreeViewColumn.SetCellDataFunc (renderer, TaskPriorityCellDataFunc);
		}

		public int DefaultPosition { get { return 1; } }

		public TreeViewColumn TreeViewColumn { get; private set; }

		public void Initialize (TreeModel model, TaskView view,  IPreferences preferences)
		{
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
		
		void TaskPriorityCellDataFunc (TreeViewColumn treeColumn, CellRenderer cell,
		                               TreeModel treeModel, TreeIter iter)
		{
			// TODO: Add bold (for high), light (for None), and also colors to priority?
			var crc = cell as CellRendererCombo;
			var task = model.GetValue (iter, 0) as ITask;
			if (task == null)
				return;
			
			switch (task.Priority) {
			case TaskPriority.Low:
				crc.Text = Catalog.GetString ("3");
				break;
			case TaskPriority.Medium:
				crc.Text = Catalog.GetString ("2");
				break;
			case TaskPriority.High:
				crc.Text = Catalog.GetString ("1");
				break;
			default:
				crc.Text = Catalog.GetString ("-");
				break;
			}
		}
		
		TreeModel model;
		TaskBeingEdited taskBeingEdited;
	}
}
