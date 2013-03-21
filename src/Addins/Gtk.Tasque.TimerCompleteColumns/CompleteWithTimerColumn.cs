//
// CompleteWithTimerColumn.cs
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
	[TaskColumnExtension ("Completed", null, "CompleteColumn", "TimerColumn")]
	public class CompleteWithTimerColumn : ITaskColumn
	{
		public CompleteWithTimerColumn ()
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
			if (view == null)
				throw new ArgumentNullException ("view");
			this.view = view;
			if (preferences == null)
				throw new ArgumentNullException ("preferences");
			this.preferences = preferences;
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
			
			// remove any timer set up on this task
			var timerCol = (TimerColumn)view.GetColumn (typeof (TimerColumn));
			var tmr = timerCol.GetTimer (task);
			if (tmr != null)
				tmr.Cancel ();
			
			if (task.State == TaskState.Active) {
				bool showCompletedTasks = preferences.GetBool (PreferencesKeys.ShowCompletedTasksKey);
				
				// When showCompletedTasks is true, complete the tasks right
				// away.  Otherwise, set a timer and show the timer animation
				// before marking the task completed.
				if (showCompletedTasks) {
					task.Complete ();
					var statusMsg = Catalog.GetString ("Task Completed");
					TaskWindow.ShowStatus (statusMsg, 5000);
				} else {
					var timer = timerCol.CreateTimer (task);
					timer.TimerStopped += (s, e) => {
						if (!e.Canceled)
							e.Task.Complete ();
					};
					timer.Tick += (s, e) => {
						var statusMsg = string.Format (
							Catalog.GetString ("Completing Task In: {0}"), e.CountdownTick);
						TaskWindow.ShowStatus (statusMsg, 2000);
					};
					timer.Start ();
				}
			} else {
				var statusMsg = Catalog.GetString ("Action Canceled");
				TaskWindow.ShowStatus (statusMsg, 5000);
				task.Activate ();
			}
		}
		
		void TaskToggleCellDataFunc (TreeViewColumn treeColumn, CellRenderer cell,
		                             TreeModel treeModel, TreeIter iter)
		{
			var crt = cell as CellRendererToggle;
			var task = model.GetValue (iter, 0) as ITask;
			if (task == null)
				crt.Active = false;
			else {
				var timerCol = (TimerColumn) view.GetColumn (typeof (TimerColumn));
				crt.Active = !(task.State == TaskState.Active && timerCol.GetTimer (task) == null);
			}
		}
		
		TreeModel model;
		IPreferences preferences;
		TaskView view;
	}
}
