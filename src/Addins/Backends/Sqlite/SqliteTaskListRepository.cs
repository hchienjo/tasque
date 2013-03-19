//
// SqliteTaskListRepository.cs
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
	using TaskListTaskCollectionRepo =
		ICollectionRepository<ITaskCore, ITaskListCore>;
	
	public class SqliteTaskListRepository : ITaskListRepository
	{
		public SqliteTaskListRepository (SqliteBackend backend,
		                                 Database database)
		{
			if (backend == null)
				throw new ArgumentNullException ("backend");
			if (database == null)
				throw new ArgumentNullException ("database");
			this.backend = backend;
			this.database = database;
		}
		
		bool ITaskListRepository.CanChangeName (ITaskListCore taskList)
		{
			return false;
		}

		string ITaskListRepository.UpdateName (ITaskListCore taskList,
		                                       string name)
		{
			throw new NotSupportedException (
				"Cannot change the name of a task list.");
		}

		bool TaskListTaskCollectionRepo
			.SupportsSharingItemsWithOtherCollections { get { return false; }
		}

		IEnumerable<ITaskCore> TaskListTaskCollectionRepo.GetAll (
			ITaskListCore container)
		{
			var command =
				"SELECT ID, Name, DueDate, CompletionDate, Priority, State " +
				"FROM Tasks WHERE Category=@id;";
			using (var cmd = new SqliteCommand (database.Connection)) {
				cmd.CommandText = command;
				cmd.Parameters.AddIdParameter (container);
				using (var dataReader = cmd.ExecuteReader ()) {
					while (dataReader.Read ()) {
						var id = dataReader.GetInt32 (0).ToString ();
						var name = dataReader.GetString (1);
						var state = (TaskState)dataReader.GetInt32 (5);
						var completionDate =
							Database.ToDateTime (dataReader.GetInt64 (3));

						ITaskCore task;
						if (state == TaskState.Active)
							task = backend.Factory.CreateTask (id, name);
						else if (state == TaskState.Completed) {
							task = backend.Factory.CreateCompletedTask (
								id, name, completionDate);
						} else {
							task = backend.Factory.CreateDiscardedTask (
								id, name);
						}
						task.Priority = (TaskPriority)dataReader.GetInt32 (4);
						task.DueDate =
							Database.ToDateTime (dataReader.GetInt64 (2));

						yield return task;
					}
				}
			}
		}

		ITaskCore TaskListTaskCollectionRepo.GetBy (ITaskListCore container,
		                                            string id)
		{
			throw new NotImplementedException ();
		}

		void TaskListTaskCollectionRepo.Add (ITaskListCore container,
		                                     ITaskCore item)
		{
			throw new NotSupportedException (
				"A task can only belong to one task list.");
		}

		void TaskListTaskCollectionRepo.AddNew (ITaskListCore container,
		                                        ITaskCore item)
		{
			var command =
				"INSERT INTO Tasks" +
				"(Name, DueDate, CompletionDate, Priority, State, " +
				"Category, ExternalID) " +
				"VALUES (@name, @dueDate, @completionDate, " +
				"@priority, @state, @catId, ''); " +
				"SELECT last_insert_rowid();";
			using (var cmd = new SqliteCommand (database.Connection)) {
				cmd.CommandText = command;
				cmd.Parameters.AddWithValue ("@name", item.Text);
				cmd.Parameters.AddWithValue ("@dueDate",
					Database.FromDateTime (item.DueDate));
				cmd.Parameters.AddWithValue ("@completionDate",
					Database.FromDateTime (DateTime.MinValue));
				cmd.Parameters.AddWithValue ("@priority", (int)item.Priority);
				cmd.Parameters.AddWithValue ("@state", (int)item.State);
				cmd.Parameters.AddWithValue ("@catId",
				                             int.Parse (container.Id));
				var id = cmd.ExecuteScalar ().ToString ();
				item.SetId (id);
			}
		}

		void TaskListTaskCollectionRepo.Remove (ITaskListCore container,
		                                        ITaskCore item)
		{
			var command = "DELETE FROM Tasks WHERE ID=@id;";
			using (var cmd = new SqliteCommand (database.Connection)) {
				cmd.CommandText = command;
				cmd.Parameters.AddWithValue ("@id", int.Parse (item.Id));
				cmd.ExecuteNonQuery ();
			}
		}

		void TaskListTaskCollectionRepo.ClearAll (ITaskListCore container)
		{
			throw new NotImplementedException ();
		}

		SqliteBackend backend;
		Database database;
	}
}
