//
// NotesColumn.cs
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
using Gdk;


namespace Gtk.Tasque
{
	public class NotesColumn : ITaskColumn
	{
		static NotesColumn ()
		{
			notePixbuf = Utilities.GetIcon ("tasque-note", 12);
		}
		
		static Pixbuf notePixbuf;
		
		public NotesColumn ()
		{
			TreeViewColumn = new TreeViewColumn {
				Title = Catalog.GetString ("Notes"),
				Sizing = TreeViewColumnSizing.Fixed,
				FixedWidth = 20,
				Resizable = false
			};
			
			var renderer = new CellRendererPixbuf ();
			TreeViewColumn.PackStart (renderer, false);
			TreeViewColumn.SetCellDataFunc (renderer, TaskNotesCellDataFunc);
		}

		public int DefaultPosition { get { return 4; } }

		public TreeViewColumn TreeViewColumn { get; private set; }
		
		public void Initialize (TreeModel model, TaskView view, IPreferences preferences)
		{
			if (model == null)
				throw new ArgumentNullException ("model");
			this.model = model;
		}
		
		public event EventHandler<TaskRowEditingEventArgs> CellEditingStarted;
		
		public event EventHandler<TaskRowEditingEventArgs> CellEditingFinished;

		void TaskNotesCellDataFunc (TreeViewColumn treeColumn, CellRenderer cell,
		                            TreeModel treeModel, TreeIter iter)
		{
			var crp = cell as CellRendererPixbuf;
			var task = model.GetValue (iter, 0) as ITask;
			if (task == null) {
				crp.Pixbuf = null;
				return;
			}
			
			crp.Pixbuf = task.HasNotes ? notePixbuf : null;
		}
		
		TreeModel model;
	}
}
