//
// SqliteBackend.cs
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
using Mono.Data.Sqlite;
using Tasque.Data.Sqlite.Gtk;
using Tasque.Utils;

namespace Tasque.Data.Sqlite
{
	[BackendExtension ("Local file")]
	public class SqliteBackend : IBackend
	{
		public TasqueObjectFactory Factory { get; private set; }

		public bool IsInitialized { get; private set; }

		public event EventHandler Initialized, Disposed;

		bool IBackend.IsConfigured { get { return IsInitialized; } }

		IBackendPreferences IBackend.Preferences {
			get { return new SqlitePreferences (); }
		}
		
		INoteRepository IRepositoryProvider<INoteRepository>.Repository {
			get { return noteRepo; }
		}
		
		ITaskListRepository IRepositoryProvider<ITaskListRepository>
			.Repository { get { return taskListRepo; }
		}
		
		ITaskRepository IRepositoryProvider<ITaskRepository>.Repository {
			get { return taskRepo; }
		}
		
		void IBackend.Initialize (IPreferences preferences)
		{
			if (preferences == null)
				throw new ArgumentNullException ("preferences");
			
			database = new Database ();
			database.Open ();
			
			allList = new AllList (preferences);

			taskListRepo = new SqliteTaskListRepository (this, database);
			taskRepo = new SqliteTaskRepository (this, database);
			noteRepo = new SqliteNoteRepository (database);

			Factory = new TasqueObjectFactory (
				taskListRepo, taskRepo, noteRepo);

			IsInitialized = true;
			if (Initialized != null)
				Initialized (this, EventArgs.Empty);
		}

		void IDisposable.Dispose ()
		{
			if (disposed)
				return;

			database.Close ();
			database = null;
			disposed = true;
			if (Disposed != null)
				Disposed (this, EventArgs.Empty);
		}
		
		IEnumerable<ITaskListCore> IBackend.GetAll ()
		{
			yield return allList;

			ITaskListCore taskList;
			var hasValues = false;

			var command = "SELECT id, name FROM Categories";
			using (var cmd = new SqliteCommand (
				command, database.Connection)) {
				using (var dataReader = cmd.ExecuteReader ()) {
					while (dataReader.Read ()) {
						var id = dataReader.GetInt32 (0).ToString ();
						var name = dataReader.GetString (1);
						hasValues = true;
						taskList = Factory.CreateTaskList (id, name);
						yield return taskList;
					}
				}
			}

			if (!hasValues) {
				var workCategory = CreateInitialTaskList ("Work");
				yield return workCategory;
				yield return CreateInitialTaskList ("Personal");
				yield return CreateInitialTaskList ("Family");
				yield return CreateInitialTaskList ("Project");

				var taskName = "Create some tasks";
				var insertCommand = string.Format (
					"INSERT INTO Tasks" +
					"(Name, DueDate, CompletionDate, Priority, State, " +
					"Category, ExternalID) " +
					"VALUES ('{0}', '{1}', '{2}', " +
					"'{3}', '{4}', '{5}', '{6}');",
					taskName, Database.FromDateTime (DateTime.MinValue),
					Database.FromDateTime (DateTime.MinValue), 0,
					(int)TaskState.Active, workCategory.Id, string.Empty);
				using (var cmd = new SqliteCommand (database.Connection)) {
					cmd.CommandText = insertCommand;
					cmd.ExecuteNonQuery ();
				}
			}
		}
		
		ITaskListCore IBackend.GetBy (string id)
		{
			throw new NotImplementedException ();
		}
		
		void IBackend.Create (ITaskListCore taskList)
		{
			throw new NotImplementedException ();
		}
		
		void IBackend.Delete (ITaskListCore taskList)
		{
			throw new NotImplementedException ();
		}
		
		event EventHandler IBackend.NeedsConfiguration { add {} remove {} }

		ITaskListCore CreateInitialTaskList (string listName)
		{
			var command = "INSERT INTO Categories (Name, ExternalID) " +
				"values (@name, ''); SELECT last_insert_rowid();";
			using (var cmd = new SqliteCommand (database.Connection)) {
				cmd.CommandText = command;
				cmd.Parameters.AddWithValue ("@name", listName);
				var id = cmd.ExecuteScalar ().ToString ();
				return Factory.CreateTaskList (id, listName);
			}
		}

		bool disposed;
		Database database;
		AllList allList;
		ITaskListRepository taskListRepo;
		ITaskRepository taskRepo;
		INoteRepository noteRepo;
	}
}
