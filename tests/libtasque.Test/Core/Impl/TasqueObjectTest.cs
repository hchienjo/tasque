//
// TasqueObjectTest.cs
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
	using TasqueObjectMock = Mock<TasqueObject<IRepository>>;

	[TestFixture]
	public class TasqueObjectTest
	{
		[SetUp]
		public void Setup ()
		{
			repoMock = new Mock<IRepository> ();
			tasqueObjectMock = new TasqueObjectMock (repoMock.Object) {
				CallBase = true
			};
			propChangingCount = propChangedCount = 0;
		}

		[Test]
		public void CreateTasqueObject ()
		{
			var tasqueObject = tasqueObjectMock.Object;
			Assert.IsNull (tasqueObject.Id, "#A0");
			Assert.AreEqual (repoMock.Object, tasqueObject.Repository, "#A1");
		}

		[Test]
		public void SetId ()
		{
			var tasqueObject = tasqueObjectMock.Object;
			var id = "l558";
			tasqueObject.SetId (id);
			Assert.AreEqual (id, tasqueObject.Id, "#A0");
		}

		[Test]
		public void SetProperty ()
		{
			var tasqueObject = tasqueObjectMock.Object;
			tasqueObjectMock.SetupGet (t => t.IsBackendDetached)
				.Returns (false);
			var propertyName = "Property";
			var curValue = "oldValue";
			var repoMock = new Mock<IRepo> ();
			Action<string> setVal = x => curValue = x;
			Func<TasqueObject<IRepository>, string, string> update
				= repoMock.Object.Update;

			SetupNotifyEventsTest (tasqueObject, propertyName, "#A");

			var value = "newValue";
			repoMock.Setup (r => r.Update (tasqueObject, value))
				.Returns (value);
			tasqueObject.SetProperty<string, TasqueObject<IRepository>> (
				propertyName, value, curValue, setVal, update);
			repoMock.Verify (r => r.Update (tasqueObject, value),
			                 Times.Once (), "#B0");
			Assert.AreEqual (curValue, value, "#B1");
			Assert.AreEqual (1, propChangingCount, "#B2");
			Assert.AreEqual (1, propChangedCount, "#B3");

			// try setting the same value again
			tasqueObject.SetProperty<string, TasqueObject<IRepository>> (
				propertyName, value, curValue, setVal, update);
			repoMock.Verify (r => r.Update (tasqueObject, value),
			                 Times.Once (), "#C0");
			Assert.AreEqual (curValue, value, "#C1");
			Assert.AreEqual (1, propChangingCount, "#C2");
			Assert.AreEqual (1, propChangedCount, "#C3");
		}

		[Test]
		public void SetPropertyWithRepoIntervention ()
		{
			var tasqueObject = tasqueObjectMock.Object;
			tasqueObjectMock.SetupGet (t => t.IsBackendDetached)
				.Returns (false);
			var propertyName = "Property";
			var curValue = "oldValue";
			var repoMock = new Mock<IRepo> ();
			Action<string> setVal = x => curValue = x;
			Func<TasqueObject<IRepository>, string, string> update
				= repoMock.Object.Update;

			SetupNotifyEventsTest (tasqueObject, propertyName, "#A");

			// setting a new value that is changed by the repo
			var newValue = "I'll be changed";
			var changedNewValue = newValue.Replace ("'", "\\'");
			repoMock.Setup (r => r.Update (tasqueObject, newValue))
				.Returns (changedNewValue);
			tasqueObject.SetProperty<string, TasqueObject<IRepository>> (
				propertyName, newValue, curValue, setVal, update);
			repoMock.Verify (r => r.Update (tasqueObject, newValue),
			                 Times.Once (), "#B0");
			Assert.AreEqual (curValue, changedNewValue, "#B1");
			Assert.AreEqual (1, propChangingCount, "#B2");
			Assert.AreEqual (1, propChangedCount, "#B3");
			
			// setting a new value that is changed to the old value by the repo
			tasqueObject.SetProperty<string, TasqueObject<IRepository>> (
				propertyName, newValue, curValue, setVal, update);
			repoMock.Verify (r => r.Update (tasqueObject, newValue),
			                 Times.Exactly (2), "#C0");
			Assert.AreEqual (curValue, changedNewValue, "#C1");
			Assert.AreEqual (1, propChangingCount, "#C2");
			Assert.AreEqual (1, propChangedCount, "#C3");
		}

		[Test]
		public void SetPropertyWithBackendDetached ()
		{
			var tasqueObject = tasqueObjectMock.Object;
			tasqueObjectMock.SetupGet (t => t.IsBackendDetached)
				.Returns (true);
			var propertyName = "Property";
			var curValue = "oldValue";
			var repoMock = new Mock<IRepo> ();
			Action<string> setVal = x => curValue = x;
			Func<TasqueObject<IRepository>, string, string> update
				= repoMock.Object.Update;
			
			SetupNotifyEventsTest (tasqueObject, propertyName, "#A");

			var newValue = "value 378";
			repoMock.Setup (r => r.Update (tasqueObject, newValue));
			tasqueObject.SetProperty<string, TasqueObject<IRepository>> (
				propertyName, newValue, curValue, setVal, update);
			repoMock.Verify (r => r.Update (tasqueObject, newValue),
			                 Times.Never (), "#B0");
			Assert.AreEqual (curValue, newValue, "#B1");
			Assert.AreEqual (1, propChangingCount, "#B2");
			Assert.AreEqual (1, propChangedCount, "#B3");
		}

		void SetupNotifyEventsTest (TasqueObject<IRepository> tasqueObject,
			string propertyName, string msgPrefix)
		{
			tasqueObject.PropertyChanging += (sender, e) => {
				Assert.AreEqual (
					propertyName, e.PropertyName, msgPrefix + "0");
				Assert.AreEqual (propChangedCount, propChangingCount++,
				                 msgPrefix + "1");
			};
			tasqueObject.PropertyChanged += (sender, e) => {
				Assert.AreEqual (
					propertyName, e.PropertyName, msgPrefix + "2");
				Assert.AreEqual (propChangingCount, ++propChangedCount,
				                 msgPrefix + "#3");
			};
		}

		int propChangingCount, propChangedCount;
		Mock<IRepository> repoMock;
		TasqueObjectMock tasqueObjectMock;

		public interface IRepo
		{
			string Update (TasqueObject<IRepository> obj, string value);
		}
	}
}
