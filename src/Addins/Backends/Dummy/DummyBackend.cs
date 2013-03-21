//
// DummyBackend.cs
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
using Gtk.Tasque.Backends.Dummy;

namespace Tasque.Backends.Dummy
{
	[BackendExtension ("Dummy")]
	public class DummyBackend : IBackend2
	{
		public DummyBackend ()
		{
			// Create fake backend content
			var sharedTask1 = new DummyTask ("Buy some nails") {
				DueDate = DateTime.Now.AddDays (1),
				Priority = 3
			};

			var sharedTask2 = new DummyTask ("Replace burnt out lightbulb") {
				DueDate = DateTime.Now,
				Priority = 1
			};

			var complTask1 = new DummyTask ("Call Roger") {
				DueDate = DateTime.Now.AddDays (-1),
			};

			var complTask2 = new DummyTask ("Test task overdue") {
				DueDate = DateTime.Now.AddDays (-89)
			};

			var notesTask1 = new DummyTask ("This task has a note.") {
				DueDate = DateTime.Now.AddDays (2),
				Priority = 4
			};
			notesTask1.TaskNotes.Add (new DummyNote ("This is the note."));

			var homeList = new DummyList ("Home");
			homeList.Tasks.Add (sharedTask1);
			homeList.Tasks.Add (sharedTask2);
			homeList.Tasks.Add (complTask1);
			homeList.Tasks.Add (new DummyTask ("File taxes") {
				DueDate = new DateTime (2008, 4, 1)
			});
			homeList.Tasks.Add (new DummyTask ("Pay storage rental fee") {
				DueDate = DateTime.Now.AddDays (1)
			});

			var workList = new DummyList ("Work");
			workList.Tasks.Add (complTask2);
			workList.Tasks.Add (notesTask1);

			var projectsList = new DummyList ("Projects");
			projectsList.Tasks.Add (sharedTask1);
			projectsList.Tasks.Add (sharedTask2);
			projectsList.Tasks.Add (new DummyTask ("Purchase lumber") {
				DueDate = DateTime.Now.AddDays (1),
				Priority = 5
			});
			projectsList.Tasks.Add (new DummyTask ("Estimate drywall requirements") {
				DueDate = DateTime.Now.AddDays (1),
				Priority = 1
			});
			projectsList.Tasks.Add (new DummyTask ("Borrow framing nailer from Ben") {
				DueDate = DateTime.Now.AddDays (1),
				Priority = 4,
			});
			projectsList.Tasks.Add (new DummyTask ("Call for an insulation estimate") {
				DueDate = DateTime.Now.AddDays (1),
				Priority = 3
			});
			projectsList.Tasks.Add (new DummyTask ("Place carpet order"));

			DummyLists = new List<DummyList> {
				homeList,
				workList,
				projectsList
			};
		}
		
		public bool IsConfigured { get; private set; }

		public bool IsInitialized { get; private set; }

		public IBackendPreferences Preferences {
			get { return new DummyPreferences (); }
		}

		public void Initialize (IPreferences preferences)
		{
			if (preferences == null)
				throw new ArgumentNullException ("preferences");
			if (IsInitialized)
				return;

			// Establish connection to backend
			// Nothing to do for Dummy Backend

			// Setup repos
			noteRepo = new NoteRepository (this);
			taskListRepo = new TaskListRepository (this);
			taskRepo = new TaskRepository (this);

			// Setup TasqueObjectFactory
			Factory = new TasqueObjectFactory (
				taskListRepo, taskRepo, noteRepo);

			IsConfigured = true;
			IsInitialized = true;
			if (Initialized != null)
				Initialized (this, EventArgs.Empty);
		}

		public IEnumerable<ITaskListCore> GetAll ()
		{
			foreach (var item in DummyLists) {
				yield return Factory.CreateTaskList (
					item.Id.ToString (), item.ListName);
			}
		}

		public ITaskListCore GetBy (string id)
		{
			var dummyList = GetTaskListBy (id);
			return Factory.CreateTaskList (id, dummyList.ListName);
		}

		public void Create (ITaskListCore taskList)
		{
			throw new NotImplementedException ();
		}

		public void Delete (ITaskListCore taskList)
		{
			throw new NotImplementedException ();
		}

		public void Dispose ()
		{
			if (disposed)
				return;
			disposed = true;

			// Cleanup and disconnect from backend
			// Nothing to do for Dummy Backend

			if (Disposed != null)
				Disposed (this, EventArgs.Empty);
		}

		public event EventHandler Disposed;
		public event EventHandler Initialized;
		public event EventHandler NeedsConfiguration;

		#region Explicit content
		INoteRepository IRepositoryProvider<INoteRepository>.Repository {
			get { return noteRepo; }
		}

		ITaskListRepository IRepositoryProvider<ITaskListRepository>.Repository {
			get { return taskListRepo; }
		}

		ITaskRepository IRepositoryProvider<ITaskRepository>.Repository {
			get { return taskRepo; }
		}
		#endregion

		internal TasqueObjectFactory Factory { get; private set; }

		internal List<DummyList> DummyLists { get; private set; }
		
		internal DummyNote GetNoteBy (string id)
		{
			return DummyLists.SelectMany (l => l.Tasks)
				.SelectMany (t => t.TaskNotes)
				.First (n => n.Id.ToString () == id);
		}
		
		internal DummyTask GetTaskBy (string id)
		{
			return DummyLists.SelectMany (l => l.Tasks)
				.First (t => t.Id.ToString () == id);
		}
		
		internal DummyList GetTaskListBy (string id)
		{
			return DummyLists.Single (l => l.Id.ToString () == id);
		}

		bool disposed;
		INoteRepository noteRepo;
		ITaskListRepository taskListRepo;
		ITaskRepository taskRepo;
	}
}
