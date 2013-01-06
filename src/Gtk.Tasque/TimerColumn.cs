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
using System.Timers;
using Mono.Unix;
using Tasque;
using Gdk;

namespace Gtk.Tasque
{
	public class TimerColumn : TreeViewColumn
	{
		public TimerColumn (IPreferences preferences, TreeModel model)
		{
			if (model == null)
				throw new ArgumentNullException ("model");
			this.model = model;
			if (preferences == null)
				throw new ArgumentNullException ("preferences");
			this.preferences = preferences;

			timeoutTargets = new ConcurrentDictionary<ITask, TaskTimer> ();

			Title = Catalog.GetString ("Timer");
			Sizing = TreeViewColumnSizing.Fixed;
			FixedWidth = 20;
			Resizable = false;
			
			var renderer = new CellRendererPixbuf {	Xalign = 0.5f };
			PackStart (renderer, false);
			SetCellDataFunc (renderer, TaskTimerCellDataFunc);
		}

		public void StartTimer (ITask task)
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

			var timer = new TaskTimer (timeout, treeIter, model);
			// if no iter found for task or this task exists in the timer dictinary, return
			// silently (this is a concurrency sensitive area, hence we shouldn't be to
			// strict if it is called multiple times)
			if (!iterFound || !timeoutTargets.TryAdd (task, timer))
				return;

			timer.TimerStopped += (sender, e) => {
				TaskTimer tmr;
				timeoutTargets.TryRemove (e.Task, out tmr);
				if (TimerExpired != null)
					TimerExpired (this, e);
			};

			timer.Tick += (sender, e) => {
				if (Tick != null)
					Tick (this, e);
			};

			timer.Start ();
		}

		public void PauseTimer (ITask task)
		{
			if (task == null)
				return;
			TaskTimer timer;
			if (timeoutTargets.TryGetValue (task, out timer))
				timer.Pause ();
		}

		public void ResumeTimer (ITask task)
		{
			if (task == null)
				return;
			TaskTimer timer;
			if (timeoutTargets.TryGetValue (task, out timer))
				timer.Resume ();
		}

		public TaskTimerState? GetTimerState (ITask task)
		{
			if (task == null)
				throw new ArgumentNullException ("task");
			TaskTimer timer;
			if (!timeoutTargets.TryGetValue (task, out timer))
				return null;
			return timer.State;
		}

		public void CancelTimer (ITask task)
		{
			if (task == null)
				return;
			TaskTimer timer;
			if (timeoutTargets.TryRemove (task, out timer))
				timer.Cancel ();
		}

		public event EventHandler<TickEventArgs> Tick;

		public event EventHandler<TimerExpiredEventArgs> TimerExpired;

		void TaskTimerCellDataFunc (TreeViewColumn treeColumn, CellRenderer cell,
		                            TreeModel treeModel, TreeIter iter)
		{
			var task = treeModel.GetValue (iter, 0) as ITask;
			TaskTimer timer;
			var crp = cell as CellRendererPixbuf;
			if (task == null || !timeoutTargets.TryGetValue (task, out timer)) {
				crp.Pixbuf = null;
				return;
			}

			crp.Pixbuf = timer.CurrentAnimPixbuf;
		}

		ConcurrentDictionary<ITask, TaskTimer> timeoutTargets;
		IPreferences preferences;
		TreeModel model;

		public class TimerExpiredEventArgs : EventArgs
		{
			public TimerExpiredEventArgs (ITask task, bool canceled)
			{
				if (task == null)
					throw new ArgumentNullException ("task");
				Task = task;
				Canceled = canceled;
			}

			public bool Canceled { get; private set; }

			public ITask Task { get; private set; }
		}

		public class TickEventArgs : EventArgs
		{
			public TickEventArgs (int tick, ITask task)
			{
				if (task == null)
					throw new ArgumentNullException ("task");
				Task = task;
				CountdownTick = tick;
			}

			public int CountdownTick { get; private set; }

			public ITask Task { get; private set; }
		}

		public enum TaskTimerState
		{
			NotStarted,
			Running,
			Paused,
			Stopped
		}

		class TaskTimer
		{
			static TaskTimer ()
			{
				inactiveAnimPixbufs = new Pixbuf [12];
				for (int i = 0; i < 12; i++) {
					var iconName = string.Format ("tasque-completing-{0}", i);
					inactiveAnimPixbufs [i] = Utilities.GetIcon (iconName, 16);
				}
			}
			
			static Pixbuf [] inactiveAnimPixbufs;

			public TaskTimer (int timeout, TreeIter iter, TreeModel model)
			{
				if (model == null)
					throw new ArgumentNullException ("model");
				if (timeout < 0)
					timeout = 5;

				CurrentAnimPixbuf = inactiveAnimPixbufs [0];

				long lngTimeout = timeout * 1000;
				var interval = lngTimeout / (double)inactiveAnimPixbufs.Length;

				this.model = model;
				this.iter = iter;

				timer = new Timer (interval);
				timer.Elapsed += delegate {
					try {
						CurrentAnimPixbuf = inactiveAnimPixbufs [++i];
						NotifyChange ();
					} catch (IndexOutOfRangeException) {
						StopTimer (false);
					}
				};

				countdown = timeout;
				sTimer = new Timer (1000);
				sTimer.Elapsed += delegate {
					if (countdown == 0) {
						sTimer.Dispose ();
						return;
					}

					var task = model.GetValue (iter, 0) as ITask;
					if (task == null)
						return;

					if (Tick != null)
						Tick (this, new TickEventArgs (--countdown, task));
				};
			}

			public Pixbuf CurrentAnimPixbuf { get; private set; }

			public TaskTimerState State { get; private set; }

			public void Start ()
			{
				timer.Start ();
				sTimer.Start ();
				State = TaskTimerState.Running;
				NotifyChange ();
			}

			public void Pause ()
			{
				timer.Stop ();
				sTimer.Stop ();
				State = TaskTimerState.Paused;
				NotifyChange ();
			}

			public void Resume ()
			{
				timer.Start ();
				sTimer.Start ();
				State = TaskTimerState.Running;
				NotifyChange ();
			}

			public void Cancel ()
			{
				StopTimer (true);
			}

			public event EventHandler<TickEventArgs> Tick;

			public event EventHandler<TimerExpiredEventArgs> TimerStopped;

			void NotifyChange ()
			{
				var path = model.GetPath (iter);
				model.EmitRowChanged (path, iter);
			}

			void StopTimer (bool canceled)
			{
				timer.Dispose ();
				sTimer.Dispose ();
				State = TaskTimerState.Stopped;
				CurrentAnimPixbuf = null;

				var task = model.GetValue (iter, 0) as ITask;
				if (task == null)
					return;

				NotifyChange ();

				if (TimerStopped != null)
					TimerStopped (this, new TimerExpiredEventArgs (task, canceled));
			}

			int i;
			int countdown;
			TreeModel model;
			TreeIter iter;
			Timer timer;
			Timer sTimer;
		}
	}
}
