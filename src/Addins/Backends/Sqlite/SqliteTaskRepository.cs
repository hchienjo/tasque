//
// SqliteTaskRepository.cs
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

namespace Tasque.Data.Sqlite
{
	using TaskTaskCollectionRepo = ICollectionRepository<ITaskCore, ITaskCore>;
	using NoteCollectionRepo = ICollectionRepository<INoteCore, ITaskCore>;
	
	public class SqliteTaskRepository : ITaskRepository
	{
		const string NestedTasksErrMsg = "Nested tasks are not supported.";
		
		public SqliteTaskRepository (SqliteBackend backend, Database database)
		{
			if (backend == null)
				throw new ArgumentNullException ("backend");
			if (database == null)
				throw new ArgumentNullException ("database");
			this.backend = backend;
			this.database = database;
		}

		DateTime ITaskRepository.UpdateDueDate (ITaskCore task, DateTime date)
		{
			var command = "UPDATE Tasks SET DueDate=@dueDate WHERE ID=@id;";
			using (var cmd = new SqliteCommand (database.Connection)) {
				cmd.CommandText = command;
				cmd.Parameters.AddWithValue ("@dueDate",
				                             Database.FromDateTime (date));
				cmd.Parameters.AddIdParameter (task);
				cmd.ExecuteNonQuery ();
			}
			return date;
		}

		string ITaskRepository.UpdateText (ITaskCore task, string text)
		{
			var command = "UPDATE Tasks SET Name=@name WHERE ID=@id;";
			using (var cmd = new SqliteCommand (database.Connection)) {
				cmd.CommandText = command;
				cmd.Parameters.AddWithValue ("@name", text);
				cmd.Parameters.AddIdParameter (task);
				cmd.ExecuteNonQuery ();
			}
			return text;
		}

		TaskPriority ITaskRepository.UpdatePriority (ITaskCore task,
		                                             TaskPriority priority)
		{
			var command = "UPDATE Tasks SET Priority=@priority WHERE ID=@id;";
			using (var cmd = new SqliteCommand (database.Connection)) {
				cmd.CommandText = command;
				cmd.Parameters.AddWithValue ("@priority", (int)priority);
				cmd.Parameters.AddIdParameter (task);
				cmd.ExecuteNonQuery ();
			}
			return priority;
		}

		void ITaskRepository.Activate (ITaskCore task)
		{
			var command = "UPDATE Tasks SET State=@state, " +
				"CompletionDate=@completionDate WHERE ID=@id;";
			using (var cmd = new SqliteCommand (database.Connection)) {
				cmd.CommandText = command;
				cmd.Parameters.AddWithValue ("@state", (int)TaskState.Active);
				cmd.Parameters.AddWithValue ("@completionDate",
					Database.FromDateTime (DateTime.MinValue));
				cmd.Parameters.AddIdParameter (task);
				cmd.ExecuteNonQuery ();
			}
		}

		DateTime ITaskRepository.Complete (ITaskCore task,
		                                   DateTime completionDate)
		{
			var command = "UPDATE Tasks SET State=@state, " +
				"CompletionDate=@completionDate WHERE ID=@id;";
			using (var cmd = new SqliteCommand (database.Connection)) {
				cmd.CommandText = command;
				cmd.Parameters.AddWithValue (
					"@state", (int)TaskState.Completed);
				cmd.Parameters.AddWithValue ("@completionDate",
					Database.FromDateTime (completionDate));
				cmd.Parameters.AddIdParameter (task);
				cmd.ExecuteNonQuery ();
			}
			return completionDate;
		}

		void ITaskRepository.Discard (ITaskCore task)
		{
			var command = "UPDATE Tasks SET State=@state WHERE ID=@id;";
			using (var cmd = new SqliteCommand (database.Connection)) {
				cmd.CommandText = command;
				cmd.Parameters.AddWithValue (
					"@state", (int)TaskState.Discarded);
				cmd.Parameters.AddIdParameter (task);
				cmd.ExecuteNonQuery ();
			}
		}

		bool ITaskRepository.SupportsNestedTasks { get { return false; } }

		bool ITaskRepository.SupportsDiscarding { get { return true; } }

		NoteSupport ITaskRepository.NoteSupport {
			get { return NoteSupport.Multiple; }
		}
		
		#region Notes
		
		INoteCore ITaskRepository.UpdateNote (ITaskCore task, INoteCore note)
		{
			throw new NotSupportedException (
				"This backend supports multiple notes.");
		}

		bool NoteCollectionRepo.SupportsSharingItemsWithOtherCollections {
			get { return false; }
		}
		
		IEnumerable<INoteCore> NoteCollectionRepo.GetAll (ITaskCore container)
		{
			var command =
				"SELECT ID, Name, Text FROM Notes WHERE Task=@id;";
			using (var cmd = new SqliteCommand (database.Connection)) {
				cmd.CommandText = command;
				cmd.Parameters.AddIdParameter (container);
				using (var dataReader = cmd.ExecuteReader ()) {
					var id = dataReader.GetInt32 (0).ToString ();
					var name = dataReader.GetString (1);
					var text = dataReader.GetString (2);
					var note = backend.Factory.CreateNote (id);
					note.Title = name;
					note.Text = text;
					yield return note;
				}
			}
		}
		
		INoteCore NoteCollectionRepo.GetBy (ITaskCore container, string id)
		{
			throw new NotImplementedException ();
		}
		
		void NoteCollectionRepo.Add (ITaskCore container, INoteCore item)
		{
			throw new NotSupportedException (
				"A note can only belong to a single task.");
		}
		
		void NoteCollectionRepo.AddNew (ITaskCore container, INoteCore item)
		{
			var command = "INSERT INTO Notes (Name, Text, Task)" +
				"VALUES (@name, @text, @id); SELECT last_insert_rowid();";
			using (var cmd = new SqliteCommand (database.Connection)) {
				cmd.CommandText = command;
				cmd.Parameters.AddWithValue ("@name", item.Title);
				cmd.Parameters.AddWithValue ("@text", item.Text);
				cmd.Parameters.AddIdParameter (container);
				var id = cmd.ExecuteScalar ().ToString ();
				item.SetId (id);
			}
		}
		
		void NoteCollectionRepo.Remove (ITaskCore container, INoteCore item)
		{
			var command = "DELETE FROM Notes WHERE ID=@id;";
			using (var cmd = new SqliteCommand (database.Connection)) {
				cmd.CommandText = command;
				cmd.Parameters.AddIdParameter (item);
				cmd.ExecuteNonQuery ();
			}
		}
		
		void NoteCollectionRepo.ClearAll (ITaskCore container)
		{
			throw new NotImplementedException ();
		}
		
		#endregion

		#region Nested tasks

		bool TaskTaskCollectionRepo.SupportsSharingItemsWithOtherCollections {
			get { throw new NotSupportedException (NestedTasksErrMsg); }
		}

		IEnumerable<ITaskCore> TaskTaskCollectionRepo.GetAll (
			ITaskCore container)
		{
			throw new NotSupportedException (NestedTasksErrMsg);
		}

		ITaskCore TaskTaskCollectionRepo.GetBy (ITaskCore container, string id)
		{
			throw new NotSupportedException (NestedTasksErrMsg);
		}

		void TaskTaskCollectionRepo.Add (ITaskCore container, ITaskCore item)
		{
			throw new NotSupportedException (NestedTasksErrMsg);
		}

		void TaskTaskCollectionRepo.AddNew (ITaskCore container,
		                                    ITaskCore item)
		{
			throw new NotSupportedException (NestedTasksErrMsg);
		}

		void TaskTaskCollectionRepo.Remove (ITaskCore container,
		                                    ITaskCore item)
		{
			throw new NotSupportedException (NestedTasksErrMsg);
		}

		void TaskTaskCollectionRepo.ClearAll (ITaskCore container)
		{
			throw new NotSupportedException (NestedTasksErrMsg);
		}

		#endregion

		SqliteBackend backend;
		Database database;
	}
}
