
using System;
using System.Collections.Generic;

namespace Tasque
{
	public static class TaskGroupModelFactory
	{
		public static TaskGroupModel CreateTodayModel (ICollection<ITask> tasks, Preferences preferences)
		{
			DateTime rangeStart = DateTime.Now;
			rangeStart = new DateTime (rangeStart.Year, rangeStart.Month,
									   rangeStart.Day, 0, 0, 0);
			DateTime rangeEnd = DateTime.Now;
			rangeEnd = new DateTime (rangeEnd.Year, rangeEnd.Month,
									 rangeEnd.Day, 23, 59, 59);
			return new TaskGroupModel (rangeStart, rangeEnd, tasks, preferences);
		}

		public static TaskGroupModel CreateOverdueModel (ICollection<ITask> tasks, Preferences preferences)
		{
			DateTime rangeStart = DateTime.MinValue;
			DateTime rangeEnd = DateTime.Now.AddDays (-1);
			rangeEnd = new DateTime (rangeEnd.Year, rangeEnd.Month, rangeEnd.Day,
									 23, 59, 59);

			return new TaskGroupModel (rangeStart, rangeEnd, tasks, preferences);
		}

		public static TaskGroupModel CreateTomorrowModel (ICollection<ITask> tasks, Preferences preferences)
		{
			DateTime rangeStart = DateTime.Now.AddDays (1);
			rangeStart = new DateTime (rangeStart.Year, rangeStart.Month,
									   rangeStart.Day, 0, 0, 0);
			DateTime rangeEnd = DateTime.Now.AddDays (1);
			rangeEnd = new DateTime (rangeEnd.Year, rangeEnd.Month,
									 rangeEnd.Day, 23, 59, 59);

			return new TaskGroupModel (rangeStart, rangeEnd, tasks, preferences);
		}
	}
}
