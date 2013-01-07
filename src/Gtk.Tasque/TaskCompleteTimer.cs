//
// TaskCompletingTimer.cs
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
using System.Timers;
using Tasque;
using Gdk;

namespace Gtk.Tasque
{
	public class TaskCompleteTimer
	{
		static TaskCompleteTimer ()
		{
			inactiveAnimPixbufs = new Pixbuf [12];
			for (int i = 0; i < 12; i++) {
				var iconName = string.Format ("tasque-completing-{0}", i);
				inactiveAnimPixbufs [i] = Utilities.GetIcon (iconName, 16);
			}
		}
		
		static Pixbuf [] inactiveAnimPixbufs;
		
		public TaskCompleteTimer (int timeout, TreeIter iter, TreeModel model)
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
					Tick (this, new TaskCompleteTimerTickEventArgs (--countdown, task));
			};
		}
		
		public Pixbuf CurrentAnimPixbuf { get; private set; }
		
		public TaskCompleteTimerState State { get; private set; }
		
		public void Start ()
		{
			timer.Start ();
			sTimer.Start ();
			State = TaskCompleteTimerState.Running;
			NotifyChange ();
		}
		
		public void Pause ()
		{
			timer.Stop ();
			sTimer.Stop ();
			State = TaskCompleteTimerState.Paused;
			NotifyChange ();
		}
		
		public void Resume ()
		{
			timer.Start ();
			sTimer.Start ();
			State = TaskCompleteTimerState.Running;
			NotifyChange ();
		}
		
		public void Cancel ()
		{
			StopTimer (true);
		}
		
		public event EventHandler<TaskCompleteTimerTickEventArgs> Tick;
		
		public event EventHandler<TaskCompleteTimerStoppedEventArgs> TimerStopped;
		
		void NotifyChange ()
		{
			var path = model.GetPath (iter);
			model.EmitRowChanged (path, iter);
		}
		
		void StopTimer (bool canceled)
		{
			timer.Dispose ();
			sTimer.Dispose ();
			State = TaskCompleteTimerState.Stopped;
			CurrentAnimPixbuf = null;
			
			var task = model.GetValue (iter, 0) as ITask;
			if (task == null)
				return;
			
			NotifyChange ();
			
			if (TimerStopped != null)
				TimerStopped (this, new TaskCompleteTimerStoppedEventArgs (task, canceled));
		}
		
		int i;
		int countdown;
		TreeModel model;
		TreeIter iter;
		Timer timer;
		Timer sTimer;
	}
}
