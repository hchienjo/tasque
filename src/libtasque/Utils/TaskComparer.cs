//
// TaskComparer.cs
//
// Author:
//       Antonius Riha <antoniusriha@gmail.com>
//
// Copyright (c) 2012 Antonius Riha
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
using System.Collections.Generic;
using Tasque.Core;

namespace Tasque.Utils
{
	public class TaskComparer : Comparer<ITask>
	{
		/// <summary>
		/// This is used for sorting tasks in the TaskWindow and should compare
		/// based on due date.
		/// </summary>
		/// <returns>
		/// whether the task has lower, equal or higher sort order.
		/// </returns>
		public override int Compare (ITask x, ITask y)
		{
			if (x == null || y == null)
				return 0;

			bool isSameDate = true;
			if (x.DueDate.Year != y.DueDate.Year
			    || x.DueDate.DayOfYear != y.DueDate.DayOfYear)
				isSameDate = false;
			
			if (!isSameDate) {
				if (x.DueDate == DateTime.MinValue) {
					// No due date set on this task. Since we already tested to see
					// if the dates were the same above, we know that the passed-in
					// task has a due date set and it should be "higher" in a sort.
					return 1;
				} else if (y.DueDate == DateTime.MinValue) {
					// This task has a due date and should be "first" in sort order.
					return -1;
				}
				
				int result = x.DueDate.CompareTo (y.DueDate);
				
				if (result != 0)
					return result;
			}
			
			// The due dates match, so now sort based on priority and name
			return CompareByPriorityAndName (x, y);
		}

		protected int CompareByPriorityAndName (ITask x, ITask y)
		{
			// The due dates match, so now sort based on priority
			if (x.Priority != y.Priority) {
				switch (x.Priority) {
				case TaskPriority.High:
					return -1;
				case TaskPriority.Medium:
					if (y.Priority == TaskPriority.High)
						return 1;
					else
						return -1;
				case TaskPriority.Low:
					if (y.Priority == TaskPriority.None)
						return -1;
					else
						return 1;
				case TaskPriority.None:
					return 1;
				}
			}
			
			// Due dates and priorities match, now sort by text
			return x.Text.CompareTo (y.Text);
		}
	}
}
