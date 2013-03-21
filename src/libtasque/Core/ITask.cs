//
// ITask.cs
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
using System.Collections.ObjectModel;

namespace Tasque.Core
{
	public interface ITask : ITaskCore, ITasqueObject
	{
		DateTime CompletionDate { get; }
		bool IsComplete { get; }
		TaskState State { get; }

		NoteSupport NoteSupport { get; }
		bool SupportsSharingNotesWithOtherTasks { get; }
		bool HasNotes { get; }
		ObservableCollection<INote> Notes { get; }
		INote Note { get; set; }

		bool SupportsNestedTasks { get; }
		bool SupportsSharingNestedTasksWithOtherTasks { get; }
		bool HasNestedTasks { get; }
		ObservableCollection<ITask> NestedTasks { get; }

		bool SupportsDiscarding { get; }

		void Activate ();
		void Complete ();
		void Discard ();

		INote CreateNote ();
		INote CreateNote (string text);
		ITask CreateNestedTask (string text);

		event EventHandler Completing, Completed,
			Activating, Activated, Discarding, Discarded;
	}
}
