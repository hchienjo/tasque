//
// TaskBackendAttachedTest.cs
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
using NUnit.Framework;

namespace Tasque.Core.Tests
{
	[TestFixture]
	public class TaskBackendAttachedTest : TaskTest
	{
		[Test]
		public void Activate ()
		{
			Activate (false);
		}

		[Test]
		public void Complete ()
		{
			Complete (false);
		}

		[Test]
		public void Discard ()
		{
			Discard (false);
		}
		
		[Test]
		public void CreateNote_NoneNoteSupport ()
		{
			CreateNote (false, NoteSupport.None);
		}
		
		[Test]
		public void CreateNote_NoneNoteSupport_WithText ()
		{
			CreateNote (false, NoteSupport.None, true, "Note text");
		}
		
		[Test]
		public void CreateNote_SingleNoteSupport ()
		{
			CreateNote (false, NoteSupport.Single);
		}
		
		[Test]
		public void CreateNote_SingleNoteSupport_WithText ()
		{
			CreateNote (false, NoteSupport.Single, true, "Note text");
		}
		
		[Test]
		public void CreateNote_MultipleNoteSupport ()
		{
			CreateNote (false, NoteSupport.Multiple);
		}
		
		[Test]
		public void CreateNote_MultipleNoteSupport_WithText ()
		{
			CreateNote (false, NoteSupport.Multiple, true, "Note text");
		}

		[Test]
		public void Refresh ()
		{
			Refresh (false);
		}
	}
}
