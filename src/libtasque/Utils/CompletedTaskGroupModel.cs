
using System;
using System.Collections.Generic;
using Tasque.Core;

namespace Tasque.Utils
{
	public class CompletedTaskGroupModel : TaskGroupModel
	{
		public CompletedTaskGroupModel (DateTime rangeStart, DateTime rangeEnd,
		                                ICollection<ITask> tasks, IPreferences preferences)
			: base (rangeStart, rangeEnd, tasks, preferences)
		{
		}

		/// <summary>
		/// Override the default filter mechanism so that we show only
		/// completed tasks in this group.
		/// </summary>
		/// <param name="model">
		/// A <see cref="TreeModel"/>
		/// </param>
		/// <param name="iter">
		/// A <see cref="TreeIter"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		protected override bool FilterTasks (ITask task)
		{
			// Don't show any task here if showCompletedTasks is false
			if (!showCompletedTasks)
				return false;

			if (task == null || task.State != TaskState.Completed)
				return false;
			
			// Make sure that the task fits into the specified range depending
			// on what the user has set the range slider to be.
			if (task.CompletionDate < this.timeRangeStart)
				return false;
			
			if (task.CompletionDate == DateTime.MinValue)
				return true; // Just in case
			
			// Don't show tasks in the completed group that were completed
			// today.  Tasks completed today should still appear under their
			// original group until tomorrow.
			DateTime today = DateTime.Now;
			
			if (today.Year == task.CompletionDate.Year
					&& today.DayOfYear == task.CompletionDate.DayOfYear)
				return false;
			
			return true;
		}
	}
}
