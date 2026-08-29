using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
//AllocatingGCMemory() は拡張メソッドのため名前空間の using が要る
using UnityEngine.TestTools.Constraints;
using UVMBinding.Core;
using AllocIs = UnityEngine.TestTools.Constraints.Is;

namespace UVMBinding.Tests
{
	//1 path = 1 型の制約（TASK-003）を検証する。
	//型が合わない場合は例外を投げず Log.Error して無視する。
	//出荷後に落とさないための方針であり、不一致は静的検査で防ぐ。
	public class SinglePropertyTypeTests
	{
		static readonly Regex Mismatch = new Regex("Type Mismatch");

		public SinglePropertyTypeTests()
		{
			Logger.Log.Level = Logger.LogLevel.All;
		}

		[Test]
		public void 同じPathに違う型を要求するとErrorが出て無視される()
		{
			PropertyContainer container = new PropertyContainer();
			container.Get<int>("P1").Value = 10;

			LogAssert.Expect(LogType.Error, Mismatch);
			var mismatch = container.Get<string>("P1");

			Assert.IsNull(mismatch);
			//登録済みの値は壊れない
			Assert.AreEqual(10, container.Get<int>("P1").Value);
			Assert.AreEqual(1, container.GetAll().Count());
		}

		[Test]
		public void 型不一致のSubscribeは例外にならない()
		{
			PropertyContainer container = new PropertyContainer();
			container.Get<int>("P1").Value = 10;

			LogAssert.Expect(LogType.Error, Mismatch);
			Assert.DoesNotThrow(() => container.Subscribe<string>("P1", _ => { }));

			Assert.AreEqual(10, container.Get<int>("P1").Value);
		}

		[Test]
		public void 型不一致のUnsubscribeは例外にならない()
		{
			PropertyContainer container = new PropertyContainer();
			container.Get<int>("P1").Value = 10;

			LogAssert.Expect(LogType.Error, Mismatch);
			Assert.DoesNotThrow(() => container.Unsubscribe<string>("P1", _ => { }));
		}

		[Test]
		public void ObjectのPropertyは任意の型の書き込みを受け入れる()
		{
			PropertyContainer container = new PropertyContainer();
			var property = container.Get<object>("P1");

			property.SetObject(10);
			Assert.AreEqual(10, property.GetObject());

			property.SetObject("Text");
			Assert.AreEqual("Text", property.GetObject());

			//実行時の値の型で代入可否が決まる
			Assert.IsTrue(property.IsAssignable<string>());
			Assert.IsFalse(property.IsAssignable<int>());
		}

		[Test]
		public void SubscribeAssignableは完全一致で受け取る()
		{
			PropertyContainer container = new PropertyContainer();
			int received = 0;
			container.SubscribeAssignable<int>("P1", x => received = x, notifyCurrent: false);

			container.Get<int>("P1").Value = 42;

			Assert.AreEqual(42, received);
		}

		[Test]
		public void SubscribeAssignableは代入可能な型で受け取る()
		{
			PropertyContainer container = new PropertyContainer();
			object received = null;
			container.SubscribeAssignable<object>("P1", x => received = x, notifyCurrent: false);

			container.Get<int>("P1").Value = 42;

			Assert.AreEqual(42, received);
		}

		[Test]
		public void SubscribeAssignableは代入できない型では受け取らない()
		{
			PropertyContainer container = new PropertyContainer();
			bool called = false;
			container.SubscribeAssignable<string>("P1", _ => called = true, notifyCurrent: false);

			container.Get<int>("P1").Value = 42;

			Assert.IsFalse(called);
		}

		[Test]
		public void Pathごとにプロパティは1つしか作られない()
		{
			PropertyContainer container = new PropertyContainer();
			container.Get<int>("P1").Value = 1;
			container.Get<int>("P2").Value = 2;

			LogAssert.Expect(LogType.Error, Mismatch);
			container.Get<float>("P1");

			Assert.AreEqual(2, container.GetAll().Count());
		}

		[Test]
		public void 完全一致の書き込みでボックス化しない()
		{
			PropertyContainer container = new PropertyContainer();
			var property = container.Get<int>("P1");

			//Trace が有効だとログの整形でアロケーションするため計測中は落とす。
			//既定は Editor で Debug、プレイヤーで Warning なので通常は通らない経路
			var level = Logger.Log.Level;
			Logger.Log.Level = Logger.LogLevel.Error;
			try
			{
				//初回は計測系の初期化が入るため一度動かしておく
				property.Value = 1;
				property.Value = 2;

				int next = 3;
				Assert.That(() => { property.Value = next; }, AllocIs.Not.AllocatingGCMemory());
			}
			finally
			{
				Logger.Log.Level = level;
			}
		}
	}
}