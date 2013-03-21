//
// NoteTest.cs
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
using NUnit.Framework;
using Moq;
using Tasque.Data;

namespace Tasque.Core.Impl
{
	[TestFixture]
	public class NoteTest
	{
		[SetUp]
		public void Setup ()
		{
			noteRepoMock = new Mock<INoteRepository> ();
			note = new Note (noteRepoMock.Object);
		}

		[Test]
		public void CreateNote ()
		{
			note = new Note (noteRepoMock.Object);
			Assert.AreEqual (noteRepoMock.Object, note.Repository, "#A0");
			Assert.IsNull (note.Id, "#A1");
			Assert.IsNull (note.Text, "#A2");
			Assert.IsEmpty (note.InternalContainers, "#A3");
			Assert.IsTrue (note.IsBackendDetached, "#A4");
		}

		[Test]
		public void CreateNoteFromRepo ()
		{
			var id = "x344";
			var note = Note.CreateNote (id, noteRepoMock.Object);

			Assert.AreEqual (noteRepoMock.Object, note.Repository, "#A0");
			Assert.AreEqual (note.Id, id, "#A1");
			Assert.IsNull (note.Text, "#A2");
			Assert.IsEmpty (note.InternalContainers, "#A3");
			Assert.IsTrue (note.IsBackendDetached, "#A4");
		}

		[Test]
		public void SetTextWithDetachedBackend ()
		{
			Assert.IsTrue (note.IsBackendDetached, "#A0");
			var s = string.Empty;
			noteRepoMock.Setup (r => r.UpdateText (note, s));

			var text1 = "This is the note text.";
			note.Text = text1;
			Assert.AreEqual (text1, note.Text, "#A1");
			note.Text = null;
			Assert.IsNull (note.Text, "#A2");
			noteRepoMock.Verify (r => r.UpdateText (note, s),
			                     Times.Never ());
		}

		[Test]
		public void SetTextWithAttachedBackend ()
		{
			int propChangedCount = 0, propChangingCount = 0;
			note.PropertyChanged += (sender, e) => {
				Assert.AreEqual ("Text", e.PropertyName, "#A0");
				propChangingCount++;
			};
			note.PropertyChanging += (sender, e) => {
				Assert.AreEqual ("Text", e.PropertyName, "#A1");
				propChangedCount++;
			};

			note.AttachBackend (null);
			Assert.IsFalse (note.IsBackendDetached, "#B0");
			
			var text1 = "This is the note text.";
			noteRepoMock.Setup (
				r => r.UpdateText (note, text1)).Returns (text1).Verifiable ("#C0");
			note.Text = text1;
			Assert.AreEqual (text1, note.Text, "#C1");
			Assert.AreEqual (1, propChangedCount, "#C2");
			Assert.AreEqual (1, propChangingCount, "#C3");

			string text2 = null;
			noteRepoMock.Setup (
				r => r.UpdateText (note, text2)).Returns (text2).Verifiable ("#D0");
			note.Text = text2;
			Assert.IsNull (note.Text, "#D1");
			Assert.AreEqual (2, propChangedCount, "#D2");
			Assert.AreEqual (2, propChangingCount, "#D3");

			noteRepoMock.Setup (r => r.UpdateText (note, text2));
			note.Text = text2;
			noteRepoMock.Verify (r => r.UpdateText (note, text2),
			                     Times.Once (), "#E0");
			Assert.IsNull (note.Text, "#E1");
			Assert.AreEqual (2, propChangedCount, "#E2");
			Assert.AreEqual (2, propChangingCount, "#E3");
			
			noteRepoMock.Verify ();
		}

		Note note;
		Mock<INoteRepository> noteRepoMock;
	}
}
