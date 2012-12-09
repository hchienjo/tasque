//
// BackendTest.cs
//
// Author:
//       Antonius Riha <antoniusriha@gmail.com>
//
// Copyright (c) 2012 Antonius Riha
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
using System.Collections.Specialized;
using System.ComponentModel;
using NUnit.Framework;
using Tasque.Backends;

namespace Tasque.Tests
{
	[TestFixture]
	public class BackendsFixture
	{
		[Test]
		public void TasksCollectionStaticTest ()
		{
			foreach (var item in backends) {
				var collection = item.Tasks;
				var msgPrefix = "Backend=" + item.Name;
				MakeStandardObservableCollectionTest (collection, msgPrefix);
				Assert.IsTrue (collection.IsReadOnly, msgPrefix + " #B");
			}
		}
		
		[Test]
		public void CategoriesCollectionStaticTest ()
		{
			foreach (var item in backends) {
				var collection = item.Categories;
				var msgPrefix = "Backend=" + item.Name;
				MakeStandardObservableCollectionTest (collection, msgPrefix);
				Assert.IsTrue (collection.IsReadOnly, msgPrefix + " #B");
			}
		}
		
		void MakeStandardObservableCollectionTest (object collection, string msgPrefix)
		{
			Assert.IsNotNull (collection, msgPrefix + " #A1");
			Assert.IsTrue (IsINotifyCollectionChanged (collection), msgPrefix + " #A2");
			Assert.IsTrue (IsINotifyPropertyChanged (collection), msgPrefix + " #A3");
		}
		
		bool IsINotifyCollectionChanged (object collection)
		{
			return typeof (INotifyCollectionChanged).IsAssignableFrom (collection.GetType ());
		}
		
		bool IsINotifyPropertyChanged (object collection)
		{
			return typeof (INotifyPropertyChanged).IsAssignableFrom (collection.GetType ());
		}
		
		List<IBackend> backends = TasqueTestsSetup.Backends;
	}
}
