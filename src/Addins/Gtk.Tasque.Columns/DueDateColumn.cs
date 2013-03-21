//
// DueDateColumn.cs
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
	[TaskColumnExtension ("Due Date")]
	public class DueDateColumn : ITaskColumn
	{
		public DueDateColumn ()
		{
			TreeViewColumn = new TreeViewColumn {
				Title = Catalog.GetString ("Due Date"),
				Sizing = TreeViewColumnSizing.Fixed,
				Alignment = 0f,
				FixedWidth = 90,
				Resizable = false,
				Clickable = true
			};
			
			var dueDateStore = new ListStore (typeof (string));
			var today = DateTime.Now;
			dueDateStore.AppendValues (
				today.ToString (Catalog.GetString ("M/d - ")) + Catalog.GetString ("Today"));
			dueDateStore.AppendValues (
				today.AddDays (1).ToString (Catalog.GetString ("M/d - ")) + Catalog.GetString ("Tomorrow"));
			dueDateStore.AppendValues (
				today.AddDays (2).ToString (Catalog.GetString ("M/d - ddd")));
			dueDateStore.AppendValues (
				today.AddDays (3).ToString (Catalog.GetString ("M/d - ddd")));
			dueDateStore.AppendValues (
				today.AddDays (4).ToString (Catalog.GetString ("M/d - ddd")));
			dueDateStore.AppendValues (
				today.AddDays (5).ToString (Catalog.GetString ("M/d - ddd")));
			dueDateStore.AppendValues (
				today.AddDays (6).ToString (Catalog.GetString ("M/d - ddd")));
			dueDateStore.AppendValues (
				today.AddDays (7).ToString (Catalog.GetString ("M/d - ")) + Catalog.GetString ("In 1 Week"));			
			dueDateStore.AppendValues (Catalog.GetString ("No Date"));
			dueDateStore.AppendValues (Catalog.GetString ("Choose Date..."));
			
			var renderer = new CellRendererCombo {
				Editable = true,
				HasEntry = false,
				Model = dueDateStore,
				TextColumn = 0,
				Xalign = 0.0f
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
				var newText = args.NewText;
				if (newText != null && model.GetIter (out iter, path)) {
					
					//  2/11 - Today
					//  2/12 - Tomorrow
					//  2/13 - Wed
					//  2/14 - Thu
					//  2/15 - Fri
					//  2/16 - Sat
					//  2/17 - Sun
					// --------------
					//  2/18 - In 1 Week
					// --------------
					//  No Date
					// ---------------
					//  Choose Date...
					
					var newDate = DateTime.MinValue;
					var tday = DateTime.Now;
					var task = model.GetValue (iter, 0) as ITask;
					
					if (newText == tday.ToString (Catalog.GetString ("M/d - ")) + Catalog.GetString ("Today"))
						newDate = tday;
					else if (newText == tday.AddDays (1).ToString (Catalog.GetString ("M/d - "))
					         + Catalog.GetString ("Tomorrow"))
						newDate = tday.AddDays (1);
					else if (newText == Catalog.GetString ("No Date"))
						newDate = DateTime.MinValue;
					else if (newText == tday.AddDays (7).ToString (Catalog.GetString ("M/d - "))
					         + Catalog.GetString ("In 1 Week"))
						newDate = tday.AddDays (7);
					else if (newText == Catalog.GetString ("Choose Date...")) {
						var tc = new TaskCalendar (task, view.TreeView.Parent);
						tc.ShowCalendar ();
						return;
					} else {
						for (int i = 2; i <= 6; i++) {
							DateTime testDate = tday.AddDays (i);
							if (testDate.ToString (Catalog.GetString ("M/d - ddd")) == newText) {
								newDate = testDate;
								break;
							}
						}
					}
					
					Logger.Debug ("task.State {0}", task.State);
					
					if (task.State != TaskState.Completed) {
						// Modify the due date
						task.DueDate = newDate;
					}
				}
				EndCellEditing ();
			};
			
			TreeViewColumn.PackStart (renderer, true);
			TreeViewColumn.SetCellDataFunc (renderer, DueDateCellDataFunc);
		}

		public int DefaultPosition { get { return 3; } }

		public TreeViewColumn TreeViewColumn { get; private set; }

		public void Initialize (TreeModel model, TaskView view, IPreferences preferences)
		{
			if (view == null)
				throw new ArgumentNullException ("view");
			this.view = view;
			if (model == null)
				throw new ArgumentNullException ("model");
			this.model = model;
		}
		
		public event EventHandler<TaskRowEditingEventArgs> CellEditingStarted;
		
		public event EventHandler<TaskRowEditingEventArgs> CellEditingFinished;

		void DueDateCellDataFunc (TreeViewColumn treeColumn, CellRenderer cell,
		                          TreeModel treeModel, TreeIter iter)
		{
			var crc = cell as CellRendererCombo;
			var task = model.GetValue (iter, 0) as ITask;
			if (task == null)
				return;
			
			var date = task.State == TaskState.Completed ? task.CompletionDate : task.DueDate;
			if (date == DateTime.MinValue || date == DateTime.MaxValue) {
				crc.Text = "-";
				return;
			}
			
			if (date.Year == DateTime.Today.Year)
				crc.Text = date.ToString (Catalog.GetString ("M/d - ddd"));
			else
				crc.Text = date.ToString (Catalog.GetString ("M/d/yy - ddd"));
		}

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
		
		TreeModel model;
		TaskView view;
		TaskBeingEdited taskBeingEdited;
	}
}
