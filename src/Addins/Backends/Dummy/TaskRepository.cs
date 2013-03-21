//
// TaskBackend.cs
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
	using INoteCollectionRepo = ICollectionRepository<INoteCore, ITaskCore>;
	using ITaskCollectionRepo = ICollectionRepository<ITaskCore, ITaskCore>;

	public class TaskRepository : ITaskRepository
	{
		const string NestedTasksErrMsg = "Nested tasks are not supported.";

		public TaskRepository (DummyBackend backend)
		{
			if (backend == null)
				throw new ArgumentNullException ("backend");
			this.backend = backend;
		}
		
		public bool SupportsDiscarding { get { return false; } }
		
		public bool SupportsNestedTasks { get { return false; } }
		
		public NoteSupport NoteSupport { get { return NoteSupport.Multiple; } }
		
		public void Activate (ITaskCore task)
		{
			var dummyTask = backend.GetTaskBy (task.Id);
			dummyTask.RevertCompletion ();
		}
		
		public DateTime Complete (ITaskCore task, DateTime completionDate)
		{
			var dummyTask = backend.GetTaskBy (task.Id);
			dummyTask.CompleteTask ();
			return dummyTask.CompletionDate;
		}
		
		public void Discard (ITaskCore task)
		{
			throw new NotSupportedException ("Discarding is not supported.");
		}
		
		public DateTime UpdateDueDate (ITaskCore task, DateTime dueDate)
		{
			var dummyTask = backend.GetTaskBy (task.Id);
			dummyTask.DueDate = dueDate == DateTime.MinValue
				? DateTime.MaxValue : dueDate;
			return dueDate;
		}
		
		public string UpdateText (ITaskCore task, string text)
		{
			var dummyTask = backend.GetTaskBy (task.Id);
			dummyTask.Text = text;
			return text;
		}
		
		public TaskPriority UpdatePriority (
			ITaskCore task, TaskPriority priority)
		{
			var dummyTask = backend.GetTaskBy (task.Id);
			dummyTask.Priority = (int)priority;
			return priority;
		}

		#region Notes

		public INoteCore UpdateNote (ITaskCore task, INoteCore note)
		{
			throw new NotSupportedException (
				"This backend supports multiple notes.");
		}

		bool INoteCollectionRepo.SupportsSharingItemsWithOtherCollections {
			get { return false; }
		}

		void INoteCollectionRepo.AddNew (ITaskCore task, INoteCore note)
		{
			var dummyTask = backend.GetTaskBy (task.Id);
			var dummyNote = new DummyNote (note.Text);
			dummyTask.TaskNotes.Add (dummyNote);
		}
		
		void INoteCollectionRepo.Add (ITaskCore task, INoteCore note)
		{
			throw new NotSupportedException (
				"Notes can only belong to one task.");
		}
		
		void INoteCollectionRepo.Remove (ITaskCore task, INoteCore note)
		{
			var dummyTask = backend.GetTaskBy (task.Id);
			var dummyNote = dummyTask.TaskNotes.Single (
				n => n.Id.ToString () == note.Id);
			dummyTask.TaskNotes.Remove (dummyNote);
		}
		
		void INoteCollectionRepo.ClearAll (ITaskCore container)
		{
			var dummyTask = backend.GetTaskBy (container.Id);
			dummyTask.TaskNotes.Clear ();
		}

		IEnumerable<INoteCore> INoteCollectionRepo.GetAll (
			ITaskCore container)
		{
			var dummyTask = backend.GetTaskBy (container.Id);
			foreach (var dummyNote in dummyTask.TaskNotes)
				yield return CreateNote (dummyNote);
		}
		
		INoteCore INoteCollectionRepo.GetBy (
			ITaskCore container, string id)
		{
			return CreateNote (backend.GetNoteBy (id));
		}

		#endregion

		#region Nested Tasks

		bool ITaskCollectionRepo.SupportsSharingItemsWithOtherCollections {
			get { throw new NotSupportedException (NestedTasksErrMsg); }
		}
		
		void ITaskCollectionRepo.Add (ITaskCore container, ITaskCore item)
		{
			throw new NotSupportedException (NestedTasksErrMsg);
		}
		
		void ITaskCollectionRepo.AddNew (ITaskCore container, ITaskCore item)
		{
			throw new NotSupportedException (NestedTasksErrMsg);
		}
		
		void ITaskCollectionRepo.Remove (ITaskCore container, ITaskCore item)
		{
			throw new NotSupportedException (NestedTasksErrMsg);
		}
		
		void ITaskCollectionRepo.ClearAll (ITaskCore container)
		{
			throw new NotSupportedException (NestedTasksErrMsg);
		}

		IEnumerable<ITaskCore> ITaskCollectionRepo.GetAll (
			ITaskCore container)
		{
			throw new NotSupportedException (NestedTasksErrMsg);
		}
		
		ITaskCore ITaskCollectionRepo.GetBy (
			ITaskCore container, string id)
		{
			throw new NotSupportedException (NestedTasksErrMsg);
		}

		#endregion

		INoteCore CreateNote (DummyNote dummyNote)
		{
			var note = backend.Factory.CreateNote (dummyNote.Id.ToString ());
			note.Text = dummyNote.Text;
			return note;
		}

		DummyBackend backend;
	}
}
