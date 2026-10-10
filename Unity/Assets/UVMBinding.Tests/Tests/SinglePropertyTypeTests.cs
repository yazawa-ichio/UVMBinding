using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using UVMBinding.Core;
using AllocIs = UnityEngine.TestTools.Constraints.Is;

namespace UVMBinding.Tests
{
	public class SinglePropertyTypeTests
	{
		static readonly Regex Mismatch = new Regex("Type Mismatch");

		public SinglePropertyTypeTests()
		{
			Logger.Log.Level = Logger.LogLevel.All;
		}

		[Test]
		public void 代入できない型の書き込みは無視され値が壊れない()
		{
			PropertyContainer container = new PropertyContainer();
			container.TrySetValue<int>("P1", 10);

			Assert.IsFalse(container.TrySetValue<string>("P1", "Text"));
			Assert.AreEqual(10, container.GetOrCreate<int>("P1").GetValue<int>());
			Assert.AreEqual(1, container.GetAll().Count());
		}

		[Test]
		public void 代入できない型のSubscribeは例外になる()
		{
			PropertyContainer container = new PropertyContainer();
			container.TrySetValue<int>("P1", 10);

			Assert.Throws<System.InvalidCastException>(
				() => container.Subscribe<string>("P1", _ => { }));
		}

		[Test]
		public void 未登録のPathへの購読は例外にならない()
		{
			PropertyContainer container = new PropertyContainer();

			Assert.DoesNotThrow(() => container.Subscribe<string>("P1", _ => { }));
		}

		[Test]
		public void 購読していないUnsubscribeは例外にならない()
		{
			PropertyContainer container = new PropertyContainer();
			container.TrySetValue<int>("P1", 10);

			Assert.DoesNotThrow(() => container.Unsubscribe<string>("P1", _ => { }));
		}

		[Test]
		public void ObjectのPropertyは任意の型の書き込みを受け入れる()
		{
			PropertyContainer container = new PropertyContainer();
			var property = container.GetOrCreate<object>("P1");

			property.SetObject(10);
			Assert.AreEqual(10, property.GetObject());

			property.SetObject("Text");
			Assert.AreEqual("Text", property.GetObject());

			Assert.IsTrue(property.IsAssignable<string>());
			Assert.IsFalse(property.IsAssignable<int>());
		}

		[Test]
		public void SubscribeAssignableは完全一致で受け取る()
		{
			PropertyContainer container = new PropertyContainer();
			int received = 0;
			container.Subscribe<int>("P1", x => received = x, notifyCurrent: false);

			container.TrySetValue<int>("P1", 42);

			Assert.AreEqual(42, received);
		}

		[Test]
		public void SubscribeAssignableは代入可能な型で受け取る()
		{
			PropertyContainer container = new PropertyContainer();
			object received = null;
			container.Subscribe<object>("P1", x => received = x, notifyCurrent: false);

			container.TrySetValue<int>("P1", 42);

			Assert.AreEqual(42, received);
		}

		[Test]
		public void SubscribeAssignableは代入できない型では受け取らない()
		{
			PropertyContainer container = new PropertyContainer();
			bool called = false;
			container.Subscribe<string>("P1", _ => called = true, notifyCurrent: false);

			container.TrySetValue<int>("P1", 42);

			Assert.IsFalse(called);
		}

		[Test]
		public void Pathごとにプロパティは1つしか作られない()
		{
			PropertyContainer container = new PropertyContainer();
			container.TrySetValue<int>("P1", 1);
			container.TrySetValue<int>("P2", 2);

			Assert.IsFalse(container.TrySetValue<float>("P1", 1f));

			Assert.AreEqual(2, container.GetAll().Count());
		}

		[Test]
		public void 完全一致の書き込みでボックス化しない()
		{
			PropertyContainer container = new PropertyContainer();
			var property = container.GetOrCreate<int>("P1");

			var level = Logger.Log.Level;
			Logger.Log.Level = Logger.LogLevel.Error;
			try
			{
				property.TrySetValue<int>(1);
				property.TrySetValue<int>(2);

				int next = 3;
				Assert.That(() => { property.TrySetValue<int>(next); }, AllocIs.Not.AllocatingGCMemory());
			}
			finally
			{
				Logger.Log.Level = level;
			}
		}
	}
}