//
// RtmTaskListRepository.cs
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
using System.Linq;
using Tasque.Data;
using RtmNet;

namespace Tasque.Backends.Rtm
{
	using ITaskListTaskCollectionRepo =
		ICollectionRepository<ITaskCore, ITaskListCore>;
	
	public class RtmTaskListRepository : ITaskListRepository
	{
		public RtmTaskListRepository (RtmBackend backend)
		{
			if (backend == null)
				throw new ArgumentNullException ("backend");
			this.backend = backend;

			Notes = new Collection<Tuple<string, INoteCore>> ();
		}

		/// <summary>
		/// Gets the task id - Note tuples.
		/// </summary>
		/// <value>
		/// Task ids and resp. notes.
		/// </value>
		public ICollection<Tuple<string, INoteCore>> Notes { get; private set; }

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
		
		bool ITaskListTaskCollectionRepo.SupportsSharingItemsWithOtherCollections {
			get { return false; }
		}

		IEnumerable<ITaskCore> ITaskListTaskCollectionRepo.GetAll (
			ITaskListCore container)
		{
			var tasks = backend.Rtm.TasksGetList (container.Id);
			foreach (var rtmList in tasks.ListCollection) {
				if (rtmList.TaskSeriesCollection == null)
					continue;
				foreach (var rtmTaskSeries in rtmList.TaskSeriesCollection) {
					var last = rtmTaskSeries.TaskCollection.Length - 1;
					var rtmTask = rtmTaskSeries.TaskCollection [last];
					var taskId = backend.EncodeTaskId (rtmTaskSeries.TaskID,
					                                   rtmTask.TaskID);
					ITaskCore task;
					if (rtmTask.Completed == DateTime.MinValue) {
						task = backend.Factory.CreateTask (
							taskId, rtmTaskSeries.Name);
					} else {
						task = backend.Factory.CreateCompletedTask (
							taskId, rtmTaskSeries.Name, rtmTask.Completed);
					}
					task.DueDate = rtmTask.Due;
					task.Priority = rtmTask.GetTaskPriority ();

					CacheNotes (rtmTaskSeries);
					yield return task;
				}
			}
		}

		ITaskCore ITaskListTaskCollectionRepo.GetBy (ITaskListCore container,
		                                             string id)
		{
			throw new NotImplementedException ();
		}

		void ITaskListTaskCollectionRepo.Add (ITaskListCore container,
		                                      ITaskCore item)
		{
			throw new NotSupportedException (
				"A task can only belong to one task list.");
		}

		void ITaskListTaskCollectionRepo.AddNew (ITaskListCore container,
		                                         ITaskCore item)
		{
			var list = backend.Rtm.TasksAdd (
				backend.Timeline, item.Text, container.Id);
			var taskSeries = list.TaskSeriesCollection [0];
			var last = taskSeries.TaskCollection.Length - 1;
			var id = backend.EncodeTaskId (
				taskSeries.TaskID, taskSeries.TaskCollection [last].TaskID);
			item.SetId (id);
		}

		void ITaskListTaskCollectionRepo.Remove (ITaskListCore container,
		                                         ITaskCore item)
		{
			string taskSeriesId, taskId;
			backend.DecodeTaskId (item, out taskSeriesId, out taskId);
			backend.Rtm.TasksDelete (backend.Timeline, container.Id,
			                         taskSeriesId, taskId);
		}

		void ITaskListTaskCollectionRepo.ClearAll (ITaskListCore container)
		{
			throw new NotImplementedException ();
		}

		void CacheNotes (TaskSeries rtmTaskSeries)
		{
			foreach (var rtmNote in rtmTaskSeries.Notes.NoteCollection) {
				var noteTuple = Notes.SingleOrDefault (
					t => t.Item2.Id == rtmNote.ID);
				INoteCore note;
				if (noteTuple != null)
					note = noteTuple.Item2;
				else {
					note = backend.Factory.CreateNote (rtmNote.ID);
					Notes.Add (new Tuple<string, INoteCore> (
						rtmTaskSeries.TaskID, note));
				}
				note.Title = rtmNote.Title;
				note.Text = rtmNote.Text;
			}
		}

		RtmBackend backend;
	}
}
