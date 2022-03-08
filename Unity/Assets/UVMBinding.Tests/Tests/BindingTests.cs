using NUnit.Framework;
using System;
using System.Collections.Generic;
using UVMBinding.Core;

namespace UVMBinding.Tests
{

	public class BindingTests
	{
		public BindingTests()
		{
			Logger.Log.Level = Logger.LogLevel.All;
		}

		[Test]
		public void Bind属性使用時のViewModel()
		{
			var vm = new BindTestViewModel();
			var ivm = (IViewModel)vm;
			// 初期値が正しいか？
			{
				Assert.AreEqual(0, vm.IntValue);
				Assert.AreEqual(20, vm.IntValue2);
				Assert.AreEqual(20, ivm.Get<int>(nameof(BindTestViewModel.IntValue2)));
				Assert.AreEqual(vm.IntValue2, vm.IntValue2Property.Value, "Pathが同じIntValue2と同じ");

				Assert.AreEqual(0, vm.FloatValue);
				Assert.AreEqual(0.5f, vm.FloatValue2);

				Assert.AreEqual(null, vm.StringValue);
				Assert.AreEqual("Test", vm.StringValue2);
			}
			// 値の設定
			{
				int change = 0;
				int count = 0;
				vm.IntValue2Property.OnChanged += (x) =>
				{
					count++;
					change = x;
				};
				vm.IntValue2 = 10;
				Assert.AreEqual(10, change);
				Assert.AreEqual(1, count);
				vm.IntValue2Property.Value = 30;
				Assert.AreEqual(30, vm.IntValue2);
				Assert.AreEqual(2, count);

				vm.IntValue2 = 30;
				Assert.AreEqual(2, count, "同じ値なので変わらない");

				vm.StringValue = "String";
				Assert.AreEqual("String", vm.StringValue);
			}
			{
				int collectionCount = 0;

				var handle = ivm.Property.Subscribe<List<int>>("Collection", (x) => collectionCount = x.Count);

				vm.Collection.Add(1);
				Assert.AreEqual(1, collectionCount);
				vm.Collection.Add(2);
				Assert.AreEqual(2, collectionCount);
				vm.Collection.Remove(1);
				Assert.AreEqual(1, collectionCount);
				vm.Collection.AddRange(new int[] { 2, 3, 4, 5 });
				Assert.AreEqual(5, collectionCount);
				vm.Collection.RemoveAt(4);
				Assert.AreEqual(4, collectionCount);

				handle.Dispose();
				vm.Collection.Clear();
				Assert.AreEqual(4, collectionCount, "イベントの購読を解除しているので発火されない");
				Assert.AreEqual(0, vm.Collection.Count, "イベントの購読を解除しているので発火されない");

			}
		}

		[Test]
		public void Event属性使用時のViewModel()
		{
			int propertyActionCount = 0;
			int propertyValueResult = 0;
			var vm = new BindTestViewModel()
			{
				PropertyAction = () => { propertyActionCount++; },
				PropertyValueAction = (x) => { propertyValueResult = x; }
			};

			int eventActionCount = 0;
			float eventValueResult = 0;
			Action eventAction = () => { eventActionCount++; };
			vm.EventAction += eventAction;
			vm.EventValueAction += (x) => { eventValueResult = x; };

			vm.Event.Publish("Func");

			Assert.AreEqual(1, propertyActionCount);
			Assert.AreEqual(1, eventActionCount);
			Assert.AreEqual(1, vm.FuncCount);

			vm.PropertyAction = null;
			vm.EventAction -= eventAction;
			vm.Event.Publish("Func");

			Assert.AreEqual(1, propertyActionCount);
			Assert.AreEqual(1, eventActionCount);
			Assert.AreEqual(2, vm.FuncCount);

			vm.Event.Publish(nameof(BindTestViewModel.PropertyValueAction), 10);
			Assert.AreEqual(10, propertyValueResult);
			vm.Event.Publish(nameof(BindTestViewModel.PropertyValueAction), 20);
			Assert.AreEqual(20, propertyValueResult);

			vm.Event.Publish(nameof(BindTestViewModel.EventValueAction), 0.5f);
			Assert.AreEqual(0.5f, eventValueResult);

			vm.Event.Publish(nameof(BindTestViewModel.FuncValue), "Test");
			Assert.AreEqual("Test", vm.FuncValueList[0]);

		}

	}

}