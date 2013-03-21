//
// ITaskRepository.cs
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

namespace Tasque.Data
{
	public interface ITaskRepository
		: ICollectionRepository<INoteCore, ITaskCore>,
		ICollectionRepository<ITaskCore, ITaskCore>
	{
		/// <summary>
		/// Determines whether the specified instance can have nested tasks.
		/// HINT: If nested tasks are not supported by the backend, always
		/// return <c>false</c>.
		/// </summary>
		/// <returns>
		/// <c>true</c> if the specified instance can have nested tasks task;
		/// otherwise, <c>false</c>.
		/// </returns>
		/// <param name='task'>
		/// Task.
		/// </param>
		bool SupportsNestedTasks { get; }

		/// <summary>
		/// Gets a value indicating whether this task/backend supports putting
		/// a task in the trash. This means that a task is not totally deleted
		/// from a backend, but put into a virtual trash bin, from which it may
		/// be restored later. HINT: If discarding/recycling is not supported
		/// by the backend, always return <c>false</c>.
		/// </summary>
		/// <value>
		/// <c>true</c> if a task can be discarded; otherwise, <c>false</c>.
		/// </value>
		bool SupportsDiscarding { get; }

		/// <summary>
		/// Gets the type of note support of this task/backend.
		/// </summary>
		/// <value>
		/// The note support.
		/// </value>
		NoteSupport NoteSupport { get; }

		INoteCore UpdateNote (ITaskCore task, INoteCore note);

		DateTime UpdateDueDate (ITaskCore task, DateTime date);
		string UpdateText (ITaskCore task, string text);
		TaskPriority UpdatePriority (ITaskCore task, TaskPriority priority);
		void Activate (ITaskCore task);
		DateTime Complete (ITaskCore task, DateTime completionDate);
		void Discard (ITaskCore task);
	}
}
