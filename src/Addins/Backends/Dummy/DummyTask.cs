//
// DummyTask.cs
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

namespace Tasque.Backends.Dummy
{
	public class DummyTask : DummyObject
	{
		public DummyTask ()
		{
			DueDate = DateTime.MaxValue;
			CompletionDate = DateTime.MaxValue;
			TaskNotes = new List<DummyNote> ();
		}

		public DummyTask (string text) : this ()
		{
			Text = text;
		}

		public bool IsCompleted { get; private set; }

		public string Text { get; set; }

		public DateTime CompletionDate { get; private set; }

		public DateTime DueDate { get; set; }

		public int Priority { get; set; }

		public List<DummyNote> TaskNotes { get; private set; }

		public void CompleteTask ()
		{
			IsCompleted = true;
			CompletionDate = DateTime.Now;
		}

		public void RevertCompletion ()
		{
			IsCompleted = false;
			CompletionDate = DateTime.MaxValue;
		}
	}
}
