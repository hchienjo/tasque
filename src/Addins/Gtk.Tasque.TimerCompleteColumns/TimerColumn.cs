//
// TimerColumn.cs
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
using System.Collections.Concurrent;
using Mono.Unix;
using Tasque;

namespace Gtk.Tasque
{
	// TODO: Use xml addin description model to provide localized column name
	[TaskColumnExtension ("Timer")]
	public class TimerColumn : ITaskColumn
	{		
		public TimerColumn ()
		{
			timeoutTargets = new ConcurrentDictionary<ITask, TaskCompleteTimer> ();

			TreeViewColumn = new TreeViewColumn {
				Title = Catalog.GetString ("Timer"),
				Sizing = TreeViewColumnSizing.Fixed,
				FixedWidth = 20,
				Resizable = false
			};
			
			var renderer = new CellRendererPixbuf {	Xalign = 0.5f };
			TreeViewColumn.PackStart (renderer, false);
			TreeViewColumn.SetCellDataFunc (renderer, TaskTimerCellDataFunc);
		}
		
		public int DefaultPosition { get { return int.MaxValue; } }
		
		public TreeViewColumn TreeViewColumn { get; private set; }
		
		public void Initialize (TreeModel model, TaskView view, IPreferences preferences)
		{
			if (model == null)
				throw new ArgumentNullException ("model");
			this.model = model;
			if (preferences == null)
				throw new ArgumentNullException ("preferences");
			this.preferences = preferences;
			
			view.RowEditingStarted += (sender, e) => {
				var timer = GetTimer (e.Task);
				if (timer != null && timer.State == TaskCompleteTimerState.Running)
					timer.Pause ();
			};
			
			view.RowEditingFinished += (sender, e) => {
				var timer = GetTimer (e.Task);
				if (timer != null && timer.State == TaskCompleteTimerState.Paused)
					timer.Resume ();
			};
		}
		
		public TaskCompleteTimer CreateTimer (ITask task)
		{
			if (task == null)
				throw new ArgumentNullException ("task");
			
			var timeout = preferences.GetInt (PreferencesKeys.InactivateTimeoutKey);
			TreeIter treeIter;
			var iterFound = false;
			model.Foreach ((treeModel, treePath, iter) => {
				if (treeModel.GetValue (iter, 0) == task) {
					treeIter = iter;
					iterFound = true;
					return true;
				}
				return false;
			});
			
			var timer = new TaskCompleteTimer (timeout, treeIter, model);
			if (!iterFound || !timeoutTargets.TryAdd (task, timer))
				throw new Exception ("Unable to create a timer for the provided task.");
			
			timer.TimerStopped += (sender, e) => {
				TaskCompleteTimer tmr;
				timeoutTargets.TryRemove (e.Task, out tmr);
			};
			
			return timer;
		}

		public TaskCompleteTimer GetTimer (ITask task)
		{
			TaskCompleteTimer timer;
			if (timeoutTargets.TryGetValue (task, out timer))
				return timer;
			return null;
		}
		
		public event EventHandler<TaskRowEditingEventArgs> CellEditingStarted;
		public event EventHandler<TaskRowEditingEventArgs> CellEditingFinished;

		void TaskTimerCellDataFunc (TreeViewColumn treeColumn, CellRenderer cell,
		                            TreeModel treeModel, TreeIter iter)
		{
			var task = treeModel.GetValue (iter, 0) as ITask;
			TaskCompleteTimer timer;
			var crp = cell as CellRendererPixbuf;
			if (task == null || !timeoutTargets.TryGetValue (task, out timer)) {
				crp.Pixbuf = null;
				return;
			}

			crp.Pixbuf = timer.CurrentAnimPixbuf;
		}

		ConcurrentDictionary<ITask, TaskCompleteTimer> timeoutTargets;
		IPreferences preferences;
		TreeModel model;
	}
}
