//
// Note.cs
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Tasque.Data;

namespace Tasque.Core.Impl
{
	public class Note
		: TasqueObject<INoteRepository>, INote, IInternalContainee<Task, Note>
	{
		public static Note CreateNote (string id, INoteRepository repository)
		{
			return new Note (repository) { Id = id };
		}
		
		public Note (INoteRepository repository) : base (repository)
		{
			isBackendDetached = true;
		}

		public string Title {
			get { return title; }
			set {
				this.SetProperty<string, Note> ("Title", value, title,
					x => title = x, Repository.UpdateTitle);
			}
		}

		public string Text {
			get { return text; }
			set {
				this.SetProperty<string, Note> ("Text", value, text,
					x => text = x, Repository.UpdateText);
			}
		}

		public override bool IsBackendDetached {
			get { return isBackendDetached; }
		}

		public override void AttachBackend (ITasqueObject container)
		{
			isBackendDetached = false;
		}
		
		public override void DetachBackend (ITasqueObject container)
		{
			if (!InternalContainers.Any (
				t => t != container && !t.IsBackendDetached))
				isBackendDetached = true;
		}

		public Collection<Task> InternalContainers {
			get {
				return containers ?? (containers = new Collection<Task> ());
			}
		}

		public override void Merge (ITasqueCore source)
		{
			var sourceNote = (INoteCore)source;
			var wasBackendDetached = isBackendDetached;
			isBackendDetached = true;
			Text = sourceNote.Text;
			isBackendDetached = wasBackendDetached;
		}

		public override void Refresh () {}

		IEnumerable<Task> IContainee<Task>.Containers {
			get { return InternalContainers; }
		}

		IEnumerable<ITaskCore> IContainee<ITaskCore>.Containers {
			get { return InternalContainers; }
		}

		bool isBackendDetached;
		string title, text;
		Collection<Task> containers;
	}
}
