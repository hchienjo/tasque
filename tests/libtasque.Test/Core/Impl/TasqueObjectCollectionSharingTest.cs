//
// TasqueObjectCollectionTest2.cs
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
	using IContainerRepo = ICollectionRepository<ITasqueCore, ITasqueCore>;
	using Container =
		TasqueObject<ICollectionRepository<ITasqueCore, ITasqueCore>>;
	using TasqueCollection = TasqueObjectCollection<ITasqueCore,
	TasqueObject<ICollectionRepository<ITasqueCore, ITasqueCore>>,
	ICollectionRepository<ITasqueCore, ITasqueCore>>;

	[TestFixture]
	public class TasqueObjectCollectionSharingTest
	{
		[SetUp]
		public void Setup ()
		{

		}

		[Test()]
		public void GetAll ()
		{
			Assert.Inconclusive ();
		}

		[Test()]
		public void GetBy ()
		{

		}

		Mock<Container> container;
		Mock<IContainerRepo> containerRepo;
		TasqueCollection collection;
	}
}
