//
// RtmTaskRepository.cs
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
using RtmNet;

namespace Tasque.Backends.Rtm
{
	using INoteCollectionRepo = ICollectionRepository<INoteCore, ITaskCore>;
	using ITaskTaskCollectionRepo =
		ICollectionRepository<ITaskCore, ITaskCore>;

	public class RtmTaskRepository : ITaskRepository
	{
		const string NestedTasksErrMsg = "Nested tasks are not supported.";
		
		public RtmTaskRepository (RtmBackend backend)
		{
			if (backend == null)
				throw new ArgumentNullException ("backend");
			this.backend = backend;
		}
		
		bool ITaskRepository.SupportsNestedTasks { get { return false; } }
		
		bool ITaskRepository.SupportsDiscarding { get { return false; } }
		
		NoteSupport ITaskRepository.NoteSupport {
			get { return NoteSupport.Multiple; }
		}
		
		DateTime ITaskRepository.UpdateDueDate (ITaskCore task, DateTime date)
		{
			var taskList = task.TaskListContainers.First ();
			string taskSeriesId, taskId;
			backend.DecodeTaskId (task, out taskSeriesId, out taskId);
			List list;
			if (task.DueDate == DateTime.MinValue) {
				list = backend.Rtm.TasksSetDueDate (
					backend.Timeline, taskList.Id, taskSeriesId, taskId);
			} else {
				var format = "yyyy-MM-ddTHH:mm:ssZ";
				var dateString = date.ToUniversalTime ().ToString (format);
				list = backend.Rtm.TasksSetDueDate (backend.Timeline,
					taskList.Id, taskSeriesId, taskId, dateString);
			}
			var last = list.TaskSeriesCollection [0].TaskCollection.Length - 1;
			return list.TaskSeriesCollection [0].TaskCollection [last].Due;
		}
		
		string ITaskRepository.UpdateText (ITaskCore task, string text)
		{
			var taskList = task.TaskListContainers.First ();
			string taskSeriesId, taskId;
			backend.DecodeTaskId (task, out taskSeriesId, out taskId);
			var list = backend.Rtm.TasksSetName (backend.Timeline,
				taskList.Id, taskSeriesId, taskId, task.Text);
			return list.TaskSeriesCollection [0].Name;
		}
		
		TaskPriority ITaskRepository.UpdatePriority (ITaskCore task,
		                                             TaskPriority priority)
		{
			var taskList = task.TaskListContainers.First ();
			string taskSeriesId, taskId;
			backend.DecodeTaskId (task, out taskSeriesId, out taskId);
			var list = backend.Rtm.TasksSetPriority (backend.Timeline,
				taskList.Id, taskSeriesId, taskId, task.GetPriorityString ());
			var last = list.TaskSeriesCollection [0].TaskCollection.Length - 1;
			return list.TaskSeriesCollection [0]
				.TaskCollection [last].GetTaskPriority ();
		}
		
		void ITaskRepository.Activate (ITaskCore task)
		{
			var taskList = task.TaskListContainers.First ();
			string taskSeriesId, taskId;
			backend.DecodeTaskId (task, out taskSeriesId, out taskId);
			backend.Rtm.TasksUncomplete (backend.Timeline, taskList.Id,
			                             taskSeriesId, taskId);
		}
		
		DateTime ITaskRepository.Complete (ITaskCore task,
		                                   DateTime completionDate)
		{
			var taskList = task.TaskListContainers.First ();
			string taskSeriesId, taskId;
			backend.DecodeTaskId (task, out taskSeriesId, out taskId);
			var list = backend.Rtm.TasksComplete (backend.Timeline,
			    taskList.Id, taskSeriesId, taskId);
			var last = list.TaskSeriesCollection [0].TaskCollection.Length - 1;
			return list.TaskSeriesCollection [0].TaskCollection [last].Completed;
		}
		
		void ITaskRepository.Discard (ITaskCore task)
		{
			throw new NotSupportedException ("Discarding is not supported");
		}

		#region Notes

		INoteCore ITaskRepository.UpdateNote (ITaskCore task, INoteCore note)
		{
			throw new NotSupportedException (
				"This backend supports multiple notes.");
		}
		
		bool INoteCollectionRepo.SupportsSharingItemsWithOtherCollections {
			get { return false; }
		}
		
		IEnumerable<INoteCore> INoteCollectionRepo.GetAll (ITaskCore container)
		{
			var notes = backend.TaskListRepo.Notes;
			return notes.Where (t => t.Item1 == container.Id)
			            .Select (t => t.Item2);
		}
		
		INoteCore INoteCollectionRepo.GetBy (ITaskCore container, string id)
		{
			throw new NotImplementedException ();
		}
		
		void INoteCollectionRepo.Add (ITaskCore container, INoteCore item)
		{
			throw new NotSupportedException (
				"A note can only belong to a single task.");
		}
		
		void INoteCollectionRepo.AddNew (ITaskCore container, INoteCore item)
		{
			var taskList = container.TaskListContainers.First ();
			string taskSeriesId, taskId;
			backend.DecodeTaskId (container, out taskSeriesId, out taskId);
			var note = backend.Rtm.NotesAdd (backend.Timeline, taskList.Id,
				taskSeriesId, taskId, item.Title, item.Text);
			item.Text = note.Text;
			item.Title = note.Title;
			item.SetId (note.ID);
		}
		
		void INoteCollectionRepo.Remove (ITaskCore container, INoteCore item)
		{
			string taskSeriesId, taskId;
			backend.DecodeTaskId (container, out taskSeriesId, out taskId);
			backend.Rtm.NotesDelete (backend.Timeline, item.Id);
		}
		
		void INoteCollectionRepo.ClearAll (ITaskCore container)
		{
			throw new NotImplementedException ();
		}

		#endregion

		#region Nested tasks
		
		bool ITaskTaskCollectionRepo.SupportsSharingItemsWithOtherCollections {
			get { throw new NotSupportedException (NestedTasksErrMsg); }
		}

		IEnumerable<ITaskCore> ITaskTaskCollectionRepo.GetAll (
			ITaskCore container)
		{
			throw new NotSupportedException (NestedTasksErrMsg);
		}

		ITaskCore ITaskTaskCollectionRepo.GetBy (ITaskCore container,
		                                         string id)
		{
			throw new NotSupportedException (NestedTasksErrMsg);
		}

		void ITaskTaskCollectionRepo.Add (ITaskCore container, ITaskCore item)
		{
			throw new NotSupportedException (NestedTasksErrMsg);
		}

		void ITaskTaskCollectionRepo.AddNew (ITaskCore container,
		                                     ITaskCore item)
		{
			throw new NotSupportedException (NestedTasksErrMsg);
		}

		void ITaskTaskCollectionRepo.Remove (ITaskCore container,
		                                     ITaskCore item)
		{
			throw new NotSupportedException (NestedTasksErrMsg);
		}

		void ITaskTaskCollectionRepo.ClearAll (ITaskCore container)
		{
			throw new NotSupportedException (NestedTasksErrMsg);
		}

		#endregion

		RtmBackend backend;
	}
}
