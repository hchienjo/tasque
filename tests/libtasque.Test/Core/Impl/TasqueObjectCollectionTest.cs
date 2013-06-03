//
// TasqueObjectCollectionTest.cs
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

namespace Tasque.Core.Tests
{
	using IContainerRepo = ICollectionRepository<ITasqueCore, ITasqueCore>;
	using TasqueCollection = TasqueObjectCollection<ITasqueObject, ITasqueCore,
		TasqueContainerObject, ICollectionRepository<ITasqueCore, ITasqueCore>>;

	[TestFixture]
	public class TasqueObjectCollectionTest
	{
		[SetUp]
		public void Setup ()
		{
			containerRepoMock = new Mock<IContainerRepo> ();
			containerMock = new Mock<TasqueContainerObject> (
				containerRepoMock.Object) {
				CallBase = true
			};
			collection = new TasqueCollection (containerMock.Object);
		}

		[Test]
		public void Add_CannotShareItemsWithOtherCollections ()
		{
			containerRepoMock.SetupGet (r => r
				.SupportsSharingItemsWithOtherCollections).Returns (false);
			var tasqueCoreMock = new Mock<ITasqueObject> ();
			Assert.Throws<NotSupportedException> (delegate {
				collection.Add (tasqueCoreMock.Object);
			});
		}

		[Test]
		public void Add_CanShareItemsWithOtherCollections ()
		{
			Assert.Inconclusive ();
		}

		[Test]
		public void GetAll ()
		{}

		[Test]
		public void GetBy ()
		{

		}

		Mock<TasqueContainerObject> containerMock;
		Mock<IContainerRepo> containerRepoMock;
		TasqueCollection collection;
	}
}
