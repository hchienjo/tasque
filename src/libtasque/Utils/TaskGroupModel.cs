
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Tasque.Core;

namespace Tasque.Utils
{
	/// <summary>
	/// Task group model. Filters tasks.
	/// </summary>
	public class TaskGroupModel
		: IEnumerable<ITask>, INotifyCollectionChanged, INotifyPropertyChanged, IDisposable
	{
		public DateTime TimeRangeStart
		{
			get { return timeRangeStart; }
		}

		public DateTime TimeRangeEnd
		{
			get { return timeRangeEnd; }
		}
		
		public TaskGroupModel (DateTime rangeStart, DateTime rangeEnd,
		                       ICollection<ITask> tasks, IPreferences preferences)
		{
			if (preferences == null)
				throw new ArgumentNullException ("preferences");
			if (tasks == null)
				throw new ArgumentNullException ("tasks");
			
			this.timeRangeStart = rangeStart;
			this.timeRangeEnd = rangeEnd;
			
			taskChangeLog = new List<MementoTask> ();
			
			showCompletedTasks = preferences.GetBool (
				PreferencesKeys.ShowCompletedTasksKey);
			
			originalTasks = tasks;
			((INotifyCollectionChanged)tasks).CollectionChanged += HandleCollectionChanged;
			
			// register change events for each task
			foreach (var item in tasks) {
				item.PropertyChanging += HandlePropertyChanging;
				item.PropertyChanged += HandlePropertyChanged;
			}
		}

		void HandlePropertyChanging (object sender, PropertyChangingEventArgs e)
		{
			var task = (ITask)sender;
			var index = 0;
			foreach (var item in this) {
				if (item == task)
					break;
				index++;
			}
			var mementoTask = new MementoTask (task, this, index);
			taskChangeLog.Add (mementoTask);
		}

		void HandlePropertyChanged (object sender, PropertyChangedEventArgs e)
		{
			var task = (ITask)sender;
			var mementoTask = taskChangeLog.First (m => m.OriginalTaskRef == task);
			taskChangeLog.Remove (mementoTask);
			
			if (mementoTask.Filtered) {
				if (!FilterTasks (task)) {
					var eArgs = new NotifyCollectionChangedEventArgs (
						NotifyCollectionChangedAction.Remove, task, mementoTask.OriginalIndex);
					OnCollectionChanged (eArgs);
				}
			} else {
				if (FilterTasks (task)) {
					var index = 0;
					foreach (var item in this) {
						if (item == task)
							break;
						index++;
					}
					var eArgs = new NotifyCollectionChangedEventArgs (
						NotifyCollectionChangedAction.Add, task, index);
					OnCollectionChanged (eArgs);
				}
			}
		}

		void HandleCollectionChanged (object sender, NotifyCollectionChangedEventArgs e)
		{
			ITask changedItem = null;
			var index = 0;
			//FIXME: Only accounts for add and remove actions
			switch (e.Action) {
			case NotifyCollectionChangedAction.Add:
				changedItem = (ITask)e.NewItems [0];
				
				// register change event on task
				changedItem.PropertyChanged += HandlePropertyChanged;
				changedItem.PropertyChanging += HandlePropertyChanging;
				
				if (!FilterTasks (changedItem))
					return;
				
				// calculate index without filtered out items
				foreach (var item in this) {
					if (item == changedItem)
						break;
					index++;
				}
				
				break;
			case NotifyCollectionChangedAction.Remove:
				changedItem = (ITask)e.OldItems [0];
				
				// unregister change event on task
				changedItem.PropertyChanged -= HandlePropertyChanged;
				changedItem.PropertyChanging -= HandlePropertyChanging;
				
				if (!FilterTasks (changedItem))
					return;
					
				var i = 0;
				var enmrtr = originalTasks.GetEnumerator ();
				bool enmrtrStatus;
				while (enmrtrStatus = enmrtr.MoveNext ()) {
					// move enumerator to right position
					if (i++ < e.OldStartingIndex)
						continue;
				
					// right position: i == oldStartingIndex
					if (FilterTasks (enmrtr.Current))
						break;
				}
				
				if (enmrtrStatus) {
					foreach (var task in this) {
						if (task == enmrtr.Current)
							break;
						index++;
					}
				} else
					index = this.Count ();
				
				break;
			}
			
			var eArgs = new NotifyCollectionChangedEventArgs (e.Action, changedItem, index);
			OnCollectionChanged (eArgs);
		}
		
		public IEnumerator<ITask> GetEnumerator ()
		{
			foreach (var item in originalTasks) {
				if (FilterTasks (item))
					yield return item;
			}
		}
		
		public void SetRange (DateTime rangeStart, DateTime rangeEnd)
		{
			this.timeRangeStart = rangeStart;
			this.timeRangeEnd = rangeEnd;
			OnPropertyChanged ("TimeRangeStart");
			OnPropertyChanged ("TimeRangeEnd");
		}
		
		public void Dispose ()
		{
			if (disposed)
				return;
			
			foreach (var item in originalTasks) {
				item.PropertyChanged -= HandlePropertyChanged;
				item.PropertyChanging -= HandlePropertyChanging;
			}
			
			disposed = true;
		}

		public event NotifyCollectionChangedEventHandler CollectionChanged;
		
		protected virtual void OnCollectionChanged (NotifyCollectionChangedEventArgs e)
		{
			if (CollectionChanged != null)
				CollectionChanged (this, e);
		}
		
		public event PropertyChangedEventHandler PropertyChanged;
		
		protected virtual void OnPropertyChanged (string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged (this, new PropertyChangedEventArgs (propertyName));
		}
		
		/// <summary>
	        /// Filter out tasks that don't fit within the group's date range
	        /// </summary>
		protected virtual bool FilterTasks (ITask task)
		{
			if (task == null || task.State == TaskState.Discarded)
				return false;
			
			// Do something special when task.DueDate == DateTime.MinValue since
			// these tasks should always be in the very last taskList.
			if (task.DueDate == DateTime.MinValue) {
				if (timeRangeEnd == DateTime.MaxValue) {
					if (!ShowCompletedTask (task))
						return false;
					
					return true;
				} else {
					return false;
				}
			}
			
			if (task.DueDate < timeRangeStart || task.DueDate > timeRangeEnd)
				return false;
			
			if (!ShowCompletedTask (task))
				return false;

			return true;
		}
		
		protected DateTime timeRangeStart;
		protected DateTime timeRangeEnd;
		protected bool showCompletedTasks = false;

		private bool ShowCompletedTask (ITask task)
		{
			if (task.State == TaskState.Completed) {
				if (!showCompletedTasks)
					return false;
				
				// Only show completed tasks that are from "Today".  Once it's
				// tomorrow, don't show completed tasks in this group and
				// instead, show them in the Completed Tasks Group.
				if (task.CompletionDate == DateTime.MinValue)
					return false; // Just in case
				
				if (task.CompletionDate.Date != DateTime.Today)
					return false;
			}
			
			return true;
		}
		
		#region Explicit content
		IEnumerator IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}
		#endregion
		
		ICollection<ITask> originalTasks;
		List<MementoTask> taskChangeLog;
		bool disposed;
		
		class MementoTask
		{
			public MementoTask (
				ITask originalTask, TaskGroupModel groupModel, int index)
			{
				OriginalTaskRef = originalTask;
				OriginalIndex = index;
				Filtered = groupModel.FilterTasks (originalTask);
			}
			
			public int OriginalIndex { get; private set; }
			public ITask OriginalTaskRef { get; private set; }
			public bool Filtered { get; private set; }
		}
	}
}
