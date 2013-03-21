//
// TaskListRepository.cs
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
using System.Collections.Generic;
using System.Linq;
using Tasque.Data;

namespace Tasque.Backends.Dummy
{
	public class TaskListRepository : ITaskListRepository
	{
		public TaskListRepository (DummyBackend backend)
		{
			if (backend == null)
				throw new ArgumentNullException ("backend");
			this.backend = backend;
		}

		public bool CanChangeName (ITaskListCore taskList)
		{
			return true;
		}

		public string UpdateName (ITaskListCore taskList, string name)
		{
			var dummyList = backend.GetTaskListBy (taskList.Id);
			dummyList.ListName = name;
			return name;
		}

		public bool SupportsSharingItemsWithOtherCollections {
			get { return true; }
		}

		public IEnumerable<ITaskCore> GetAll (ITaskListCore container)
		{
			var dummyList = backend.GetTaskListBy (container.Id);
			foreach (var dummyTask in dummyList.Tasks)
				yield return CreateTask (dummyTask);
		}

		public ITaskCore GetBy (ITaskListCore container, string id)
		{
			return CreateTask (backend.GetTaskBy (id));
		}

		public void AddNew (ITaskListCore taskList, ITaskCore task)
		{
			var dummyList = backend.GetTaskListBy (taskList.Id);
			
			var dummyTask = new DummyTask (task.Text) {
				DueDate = task.DueDate == DateTime.MaxValue
				? DateTime.MinValue : task.DueDate,
				Priority = (int)task.Priority
			};

			dummyList.Tasks.Add (dummyTask);
		}

		public void Add (ITaskListCore taskList, ITaskCore task)
		{
			var dummyList = backend.GetTaskListBy (taskList.Id);
			var dummyTask = backend.GetTaskBy (task.Id);
			dummyList.Tasks.Add (dummyTask);
		}

		public void ClearAll (ITaskListCore taskList)
		{
			var dummyList = backend.GetTaskListBy (taskList.Id);
			dummyList.Tasks.Clear ();
		}

		public void Remove (ITaskListCore taskList, ITaskCore task)
		{
			var dummyList = backend.GetTaskListBy (taskList.Id);
			var dummyTask = dummyList.Tasks.Single (
				t => t.Id.ToString () == task.Id);
			dummyList.Tasks.Remove (dummyTask);
		}

		ITaskCore CreateTask (DummyTask dummyTask)
		{
			ITaskCore task;
			if (dummyTask.IsCompleted) {
				task = backend.Factory.CreateCompletedTask (
					dummyTask.Id.ToString (), dummyTask.Text,
					dummyTask.CompletionDate);
			} else {
				task = backend.Factory.CreateTask (
					dummyTask.Id.ToString (), dummyTask.Text);
			}
			
			task.DueDate = dummyTask.DueDate == DateTime.MaxValue
				? DateTime.MinValue : dummyTask.DueDate;
			
			task.Priority = dummyTask.Priority > 3 ? TaskPriority.High
				: (TaskPriority)dummyTask.Priority;
			
			return task;
		}

		DummyBackend backend;
	}
}
