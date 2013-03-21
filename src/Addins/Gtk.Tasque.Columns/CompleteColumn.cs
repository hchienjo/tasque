//
// CompleteColumn.cs
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
	[TaskColumnExtension ("Completed")]
	public class CompleteColumn : ITaskColumn
	{
		public CompleteColumn ()
		{
			TreeViewColumn = new TreeViewColumn {
				Title = Catalog.GetString ("Completed"),
				Sizing = TreeViewColumnSizing.Autosize,
				Resizable = false,
				Clickable = true
			};
			
			var renderer = new CellRendererToggle ();
			renderer.Toggled += OnTaskToggled;
			TreeViewColumn.PackStart (renderer, false);
			TreeViewColumn.SetCellDataFunc (renderer, TaskToggleCellDataFunc);
		}

		public int DefaultPosition { get { return 0; } }

		public TreeViewColumn TreeViewColumn { get; private set; }

		public void Initialize (TreeModel model, TaskView view, IPreferences preferences)
		{
			if (model == null)
				throw new ArgumentNullException ("model");
			this.model = model;
		}

		public event EventHandler<TaskRowEditingEventArgs> CellEditingStarted;
		public event EventHandler<TaskRowEditingEventArgs> CellEditingFinished;
		
		void OnTaskToggled (object o, ToggledArgs args)
		{
			Logger.Debug ("OnTaskToggled");
			TreeIter iter;
			var path = new TreePath (args.Path);
			if (!model.GetIter (out iter, path))
				return; // Do nothing
			
			var task = model.GetValue (iter, 0) as ITask;
			if (task == null)
				return;
			
			string statusMsg;
			if (task.State == TaskState.Active) {
				task.Complete ();
				statusMsg = Catalog.GetString ("Task Completed");
			} else {
				statusMsg = Catalog.GetString ("Action Canceled");
				task.Activate ();
			}
			TaskWindow.ShowStatus (statusMsg, 5);
		}

		void TaskToggleCellDataFunc (TreeViewColumn treeColumn, CellRenderer cell,
		                             TreeModel treeModel, TreeIter iter)
		{
			var crt = cell as CellRendererToggle;
			var task = model.GetValue (iter, 0) as ITask;
			crt.Active = task != null && task.State != TaskState.Active;
		}
		
		TreeModel model;
	}
}
