//
// RtmNoteRepository.cs
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
using Tasque.Data;

namespace Tasque.Backends.Rtm
{
	public class RtmNoteRepository : INoteRepository
	{
		public RtmNoteRepository (RtmBackend backend)
		{
			if (backend == null)
				throw new ArgumentNullException ("backend");
			this.backend = backend;
		}

		public string UpdateTitle (INoteCore note, string title)
		{
			throw new NotImplementedException ();
		}

		string INoteRepository.UpdateText (INoteCore note, string text)
		{

		}

		void UpdateNote (INoteCore note, string title, string text)
		{
			var rtmNote = backend.Rtm.NotesEdit (backend.Timeline, note.Id, note.Title, note.Text);
			note.Title =
		}

		internal void SaveNote (RtmTask rtmTask, RtmNote note)
		{
			if (rtm != null) {
				try {
					rtm.NotesEdit (timeline, note.ID, String.Empty, note.Text);
				} catch (Exception e) {
					Logger.Debug ("RtmBackend.SaveNote: Unable to save note");
					Logger.Debug (e.ToString ());
				}
			} else
				throw new Exception ("Unable to communicate with Remember The Milk");
		}

		RtmBackend backend;
	}
}
