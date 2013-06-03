//
// TasqueObjectFactory.cs
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

namespace Tasque.Data
{
	public class TasqueObjectFactory
	{
		public TasqueObjectFactory (ITaskListRepository taskListRepo,
			ITaskRepository taskRepo, INoteRepository noteRepo = null)
		{
			if (taskListRepo == null)
				throw new ArgumentNullException ("taskListRepo");
			if (taskRepo == null)
				throw new ArgumentNullException ("taskRepo");
			this.taskListRepo = taskListRepo;
			this.taskRepo = taskRepo;
			this.noteRepo = noteRepo;
		}

		public INoteCore CreateNote (string id)
		{
			return Note.CreateNote (id, noteRepo);
		}

		public ITaskListCore CreateTaskList (string id, string name)
		{
			return TaskList.CreateTaskList (
				id, name, taskListRepo, taskRepo, noteRepo);
		}
		
		public ITaskListCore CreateSmartTaskList (string id, string name)
		{
			return TaskList.CreateSmartTaskList (id, name, taskListRepo);
		}

		public ITaskCore CreateTask (string id, string text)
		{
			return Task.CreateTask (id, text, taskRepo, noteRepo);
		}

		public ITaskCore CreateCompletedTask (string id, string text,
		                                      DateTime completionDate)
		{
			return Task.CreateCompletedTask (
				id, text, completionDate, taskRepo, noteRepo);
		}

		public ITaskCore CreateDiscardedTask (string id, string text)
		{
			return Task.CreateDiscardedTask (id, text, taskRepo, noteRepo);
		}

		ITaskListRepository taskListRepo;
		ITaskRepository taskRepo;
		INoteRepository noteRepo;
	}
}
