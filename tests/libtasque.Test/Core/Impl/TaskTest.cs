//
// TaskTest.cs
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
using System.Collections.Specialized;
using NUnit.Framework;
using Moq;
using Tasque.Data;

namespace Tasque.Core.Impl
{
	using NoteCollectionRepo = ICollectionRepository<INote, ITask>;
	using TaskCollectionRepo = ICollectionRepository<ITask, ITask>;

	[TestFixture]
	public class TaskTest
	{
		protected Task Task { get; set; }
		protected string InitialText { get; set; }
		protected Mock<ITaskRepository> TaskRepoMock { get; set; }
		protected Mock<INoteRepository> NoteRepoMock { get; set; }

		[SetUp]
		public void Setup ()
		{
			TaskRepoMock = new Mock<ITaskRepository> ();
			NoteRepoMock = new Mock<INoteRepository> ();
			InitialText = "Task1";
			Task = new Task (InitialText, TaskRepoMock.Object,
			                 NoteRepoMock.Object);
		}
		
		[Test]
		public void CreateTask ()
		{
			TaskRepoMock.Setup (r => r.UpdateText (
				It.IsAny<ITask> (), InitialText));
			Task = new Task (InitialText, TaskRepoMock.Object,
			                 NoteRepoMock.Object);
			TaskRepoMock.Verify (r => r.UpdateText (
				It.IsAny<ITask> (), InitialText), Times.Never (), "#A0");
			Assert.AreEqual (TaskRepoMock.Object, Task.Repository, "#A1");
			Assert.IsNull (Task.Id, "#A2");
			Assert.AreEqual (InitialText, Task.Text, "#A3");
			Assert.IsEmpty (((IInternalContainee<TaskList, Task>)Task)
			                .InternalContainers, "#A4");
			Assert.IsEmpty (((IInternalContainee<Task, Task>)Task)
			                .InternalContainers, "#A5");
			Assert.IsTrue (Task.IsBackendDetached, "#A6");
			Assert.AreEqual (DateTime.MinValue, Task.DueDate, "#A7");
			Assert.AreEqual (DateTime.MinValue, Task.CompletionDate, "#A8");
			Assert.IsFalse (Task.HasNotes, "#A9");
			Assert.IsFalse (Task.IsComplete, "#A10");
			Assert.AreEqual (TaskPriority.None, Task.Priority, "#A11");
			Assert.AreEqual (TaskState.Active, Task.State, "#A12");
		}
		
		[Test]
		public void CreateTaskFromRepo ()
		{
			var id = "ols4";
			TaskRepoMock.Setup (r => r.UpdateText (
				It.IsAny<ITask> (), InitialText));
			var task = Task.CreateTask (id, InitialText,
			TaskRepoMock.Object, NoteRepoMock.Object);
			TaskRepoMock.Verify (r => r.UpdateText (
				It.IsAny<ITask> (), InitialText), Times.Never (), "#A0");
			Assert.AreEqual (id, task.Id, "#A1");
			Assert.AreEqual (InitialText, task.Text, "#A2");
		}
		
		[Test]
		public void CreateCompletedTaskFromRepo ()
		{
			var id = "ols4";
			var completionDate = DateTime.Now;
			TaskRepoMock.Setup (r => r.UpdateText (
				It.IsAny<ITask> (), InitialText));
			var task = Task.CreateCompletedTask (id, InitialText,
			completionDate, TaskRepoMock.Object, NoteRepoMock.Object);
			TaskRepoMock.Verify (r => r.UpdateText (
				It.IsAny<ITask> (), InitialText), Times.Never (), "#A0");
			Assert.AreEqual (id, task.Id, "#A1");
			Assert.AreEqual (InitialText, task.Text, "#A2");
			Assert.AreEqual (completionDate, task.CompletionDate, "#A3");
			Assert.IsTrue (task.IsComplete, "#A4");
		}
		
		[Test]
		public void CreateDiscardedTaskFromRepo ()
		{
			var id = "ols4";
			TaskRepoMock.Setup (r => r.UpdateText (
				It.IsAny<ITask> (), InitialText));
			TaskRepoMock.SetupGet (r => r.SupportsDiscarding)
				.Returns (true).Verifiable ("#A0");
			var task = Task.CreateDiscardedTask (id, InitialText,
			TaskRepoMock.Object, NoteRepoMock.Object);
			TaskRepoMock.Verify (r => r.UpdateText (
				It.IsAny<ITask> (), InitialText), Times.Never (), "#A1");
			Assert.AreEqual (id, task.Id, "#A2");
			Assert.AreEqual (InitialText, task.Text, "#A3");
			Assert.AreEqual (TaskState.Discarded, task.State, "#A4");
			
			TaskRepoMock.Setup (r => r.SupportsDiscarding)
				.Returns (false).Verifiable ("#A5");
			
			Assert.Throws<NotSupportedException> (delegate {
				Task.CreateDiscardedTask (id, InitialText, TaskRepoMock.Object,
				                          NoteRepoMock.Object);
			}, "#A6");
			TaskRepoMock.Verify ();
		}

		protected void Activate (bool detached)
		{
			Task = Task.CreateCompletedTask ("ijj", "Text 1", DateTime.Now,
				TaskRepoMock.Object, NoteRepoMock.Object);
			SetupDetachedOrAttached (Task, detached, "#A0");
			
			int activatingCount = 0, activatedCount = 0;
			Task.Activating += delegate {
				Assert.AreEqual (activatedCount, activatingCount++, "#A1");
			};
			Task.Activated += delegate {
				Assert.AreEqual (activatingCount, ++activatedCount, "#A2");
			};

			TaskRepoMock.Setup (r => r.Activate (Task));
			Task.Activate ();
			
			var times = detached ? Times.Never () : Times.Once ();
			TaskRepoMock.Verify (r => r.Activate (Task), times, "#A3");
			Assert.AreEqual (DateTime.MinValue, Task.CompletionDate, "#A4");
			Assert.AreEqual (TaskState.Active, Task.State, "#A5");
			Assert.IsFalse (Task.IsComplete, "#A6");
		}

		protected void Complete (bool detached)
		{
			SetupDetachedOrAttached (Task, detached, "#A0");
			
			int completingCount = 0, completedCount = 0;
			Task.Completing += delegate {
				Assert.AreEqual (completedCount, completingCount++, "#A1");
			};
			Task.Completed += delegate {
				Assert.AreEqual (completingCount, ++completedCount, "#A2");
			};

			var now = DateTime.Now;
			TaskRepoMock.Setup (r => r.Complete (Task, It.IsAny<DateTime> ()))
				.Returns (now);
			Task.Complete ();

			var times = detached ? Times.Never () : Times.Once ();
			TaskRepoMock.Verify (r => r.Complete (Task, It.IsAny<DateTime> ()),
			                     times, "#A3");

			if (detached) {
				Assert.LessOrEqual (now, Task.CompletionDate, "#A4a");
				Assert.GreaterOrEqual (
					DateTime.Now, Task.CompletionDate, "#A4b");
			} else
				Assert.AreEqual (now, Task.CompletionDate, "#A4");
			Assert.AreEqual (TaskState.Completed, Task.State, "#A5");
			Assert.IsTrue (Task.IsComplete, "#A6");
		}

		protected void Discard (bool detached)
		{
			TaskRepoMock.Setup (r => r.SupportsDiscarding)
				.Returns (true);
			Task = new Task ("k", TaskRepoMock.Object, NoteRepoMock.Object);
			SetupDetachedOrAttached (Task, detached, "#A0");
			
			int discardingCount = 0, discardedCount = 0;
			Task.Discarding += delegate {
				Assert.AreEqual (discardedCount, discardingCount++, "#A1");
			};
			Task.Discarded += delegate {
				Assert.AreEqual (discardingCount, ++discardedCount, "#A2");
			};

			TaskRepoMock.Setup (r => r.Discard (Task));
			Task.Discard ();
			
			var times = detached ? Times.Never () : Times.Once ();
			TaskRepoMock.Verify (r => r.Discard (Task),
			                     times, "#A3");
			Assert.AreEqual (TaskState.Discarded, Task.State, "#A4");
		}

		protected void CreateNote (bool detached, NoteSupport noteSupport,
		                           bool withText = false, string text = null)
		{
			TaskRepoMock.Setup (r => r.NoteSupport).Returns (noteSupport);
			Task = new Task ("Task1", TaskRepoMock.Object,
			                 NoteRepoMock.Object);
			SetupDetachedOrAttached (Task, detached, "#A0");
			TaskRepoMock.Setup (r => r.AddNew (Task, It.IsAny<INote> ()));
			TaskRepoMock.Setup (r => r.UpdateNote (Task, It.IsAny<INote> ()))
				.Returns ((ITask t, INote n) => n);
			
			if (noteSupport == NoteSupport.None)
				CreateNote_NoneNoteSupport (text, withText, "#B");
			else if (noteSupport == NoteSupport.Single)
				CreateNote_SingleNoteSupport (detached, text, withText, "#B");
			else
				CreateNote_MultipleNoteSupport (detached, text, withText, "#B");
		}

		protected void Refresh (bool detached)
		{
			Assert.Inconclusive ();
		}

		void CreateNote_NoneNoteSupport (
			string text, bool withText, string msg)
		{
			if (withText) {
				Assert.Throws<NotSupportedException> (delegate {
					Task.CreateNote (text);
				}, msg + 0);
			} else {
				Assert.Throws<NotSupportedException> (delegate {
					Task.CreateNote ();
				}, msg + 1);
			}
			TaskRepoMock.Verify (r => r.AddNew (Task, It.IsAny<INote> ()),
			                     Times.Never (), msg + 2);
			TaskRepoMock.Verify (r => r.UpdateNote (Task, It.IsAny<INote> ()),
			                     Times.Never (), msg + 3);
		}

		void CreateNote_SingleNoteSupport (
			bool detached, string text, bool withText, string msg)
		{
			if (!withText)
				text = null;

			int propChangingCount = 0, propChangedCount = 0;
			Task.PropertyChanging += (sender, e) => {
				Assert.AreEqual ("Note", e.PropertyName, msg + 0);
				Assert.AreEqual (propChangedCount, propChangingCount++,
				                 msg + 1);
			};
			Task.PropertyChanging += (sender, e) => {
				Assert.AreEqual ("Note", e.PropertyName, msg + 2);
				Assert.AreEqual (++propChangedCount, propChangingCount,
				                 msg + 3);
			};

			var note = withText ? Task.CreateNote (text) : Task.CreateNote ();

			Assert.IsNotNull (note, msg + 4);
			Assert.AreEqual (note, Task.Note, msg + 5);
			Assert.AreEqual (text, note.Text, msg + 6);
			Assert.AreEqual (1, propChangingCount, msg + 7);
			Assert.AreEqual (1, propChangedCount, msg + 8);
			
			TaskRepoMock.Verify (r => r.AddNew (Task, It.IsAny<INote> ()),
			                     Times.Never (), msg + 9);
			var times = detached ? Times.Never () : Times.Once ();
			TaskRepoMock.Verify (r => r.UpdateNote (Task, It.IsAny<INote> ()),
			                     times, msg + 10);
		}

		void CreateNote_MultipleNoteSupport (
			bool detached, string text, bool withText, string msg)
		{
			if (!withText)
				text = null;

			INote note = null, noteFromChangeEvent = null;
			var collectionChangedCount = 0;
			Task.Notes.CollectionChanged += (sender, e) => {
				collectionChangedCount++;
				Assert.AreEqual (NotifyCollectionChangedAction.Add, e.Action,
				                 msg + 0);
				noteFromChangeEvent = e.NewItems [0] as INote;
			};
			
			note = withText ? Task.CreateNote (text) : Task.CreateNote ();

			Assert.AreEqual (note, noteFromChangeEvent, msg + 1);
			Assert.IsNotNull (note, msg + 2);
			Assert.Contains (note, Task.Notes, msg + 3);
			Assert.AreEqual (text, note.Text, msg + 4);
			Assert.AreEqual (1, collectionChangedCount, msg + 5);

			var times = detached ? Times.Never () : Times.Once ();
			TaskRepoMock.Verify (r => r.AddNew (Task, It.IsAny<INote> ()),
			                     times, msg + 6);
			TaskRepoMock.Verify (r => r.UpdateNote (Task, It.IsAny<INote> ()),
			                     Times.Never (), msg + 7);
		}

		void SetupDetachedOrAttached (Task task, bool detached, string msg)
		{
			if (detached)
				Assert.IsTrue (task.IsBackendDetached, msg);
			else {
				task.AttachBackend (null);
				Assert.IsFalse (task.IsBackendDetached, msg);
			}
		}
	}
}
