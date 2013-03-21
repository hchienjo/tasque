//
// TaskCompleteTimer.cs
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
using Tasque.Core;
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

			// prevent overflows
			if (timeout < 0 || timeout > 1000)
				timeout = 5;

			CurrentAnimPixbuf = inactiveAnimPixbufs [0];
			
			var msTimeout = timeout * 1000;
			interval = (uint)(msTimeout / inactiveAnimPixbufs.Length);
			
			this.model = model;
			this.iter = iter;

			countdown = timeout;
		}
		
		public Pixbuf CurrentAnimPixbuf { get; private set; }
		
		public TaskCompleteTimerState State { get; private set; }
		
		public void Start ()
		{
			State = TaskCompleteTimerState.Running;
			NotifyChange ();
			GLib.Timeout.Add (interval, PixbufTimerElapsed);
			GLib.Timeout.Add (1000, SecondsTimerElapsed);
		}
		
		public void Pause ()
		{
			State = TaskCompleteTimerState.Paused;
			NotifyChange ();
		}
		
		public void Resume ()
		{
			Start ();
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
			State = TaskCompleteTimerState.Stopped;
			CurrentAnimPixbuf = null;
			
			var task = model.GetValue (iter, 0) as ITask;
			if (task == null)
				return;

			NotifyChange ();
			
			if (TimerStopped != null)
				TimerStopped (this, new TaskCompleteTimerStoppedEventArgs (task, canceled));
		}

		bool PixbufTimerElapsed ()
		{
			// if Pause () has been called on timer or timer has stopped, don't proceed
			if (State == TaskCompleteTimerState.Paused || State == TaskCompleteTimerState.Stopped)
				return false;
			
			try {
				CurrentAnimPixbuf = inactiveAnimPixbufs [++i];
				NotifyChange ();
				return true;
			} catch (IndexOutOfRangeException) {
				StopTimer (false);
				return false;
			}
		}
		
		bool SecondsTimerElapsed ()
		{
			// if Pause () has been called on timer or timer has stopped, don't proceed
			if (State == TaskCompleteTimerState.Paused || State == TaskCompleteTimerState.Stopped)
				return false;
			
			if (countdown == 0)
				return false;
			
			var task = model.GetValue (iter, 0) as ITask;
			if (task == null)
				return false;
			
			if (Tick != null)
				Tick (this, new TaskCompleteTimerTickEventArgs (--countdown, task));
			return true;
		}
		
		int i;
		int countdown;
		uint interval;
		TreeModel model;
		TreeIter iter;
	}
}
