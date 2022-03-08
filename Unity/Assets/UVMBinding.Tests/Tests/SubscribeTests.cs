using NUnit.Framework;
using System.Linq;
using UVMBinding.Core;

namespace UVMBinding.Tests
{
	public class SubscribeTests
	{
		public SubscribeTests()
		{
			Logger.Log.Level = Logger.LogLevel.All;
		}

		[Test]
		public void EventのUnsubscribeで解除出来る()
		{
			EventBroker broker = new EventBroker();
			int count1 = 0;
			int count2 = 0;
			System.Action action1 = () => { count1++; };
			System.Action<int> action2 = (x) => { count2++; };

			broker.Subscribe("E1", action1);
			broker.Subscribe("E1", action2);

			broker.Publish("E1");
			broker.Publish("E1", 5);
			broker.Publish("E1", 2);

			Assert.AreEqual(1, count1);
			Assert.AreEqual(2, count2);

			broker.Unsubscribe("E1", action1);
			broker.Unsubscribe("E1", action2);

			broker.Publish("E1");
			broker.Publish("E1", 10);

			Assert.AreEqual(1, count1);
			Assert.AreEqual(2, count2);

#if UNITY_EDITOR
			Assert.AreEqual(2, broker.GetAll().Count());
#endif

		}
		[Test]
		public void EventのHandleで解除出来る()
		{
			EventBroker broker = new EventBroker();
			int count1 = 0;
			int count2 = 0;
			var handel1 = broker.Subscribe("E1", () => { count1++; });
			var handel2 = broker.SubscribeByFunc<int>("E1", () => (x) => { count2++; });

			broker.Publish("E1");
			broker.Publish("E1", 5);
			broker.Publish("E1", 2);

			Assert.AreEqual(1, count1);
			Assert.AreEqual(2, count2);

			handel1.Dispose();
			handel2.Dispose();

			broker.Publish("E1");
			broker.Publish("E1", 10);

			Assert.AreEqual(1, count1);
			Assert.AreEqual(2, count2);

		}



		[Test]
		public void PropertyのUnsubscribeで解除出来る()
		{
			PropertyContainer container = new PropertyContainer();
			int count1 = 0;
			int value1 = 0;

			System.Action<int> action = (x) =>
			{
				count1++;
				value1 = x;
			};

			container.Subscribe("P1", action);

			container.Get<int>("P1").Value = 10;

			Assert.AreEqual(1, count1);
			Assert.AreEqual(10, value1);

			container.SetAllDirty();
			container.SetDirty("P1");

			//カウントだけ増える
			Assert.AreEqual(3, count1);
			Assert.AreEqual(10, value1);

			container.Unsubscribe("P1", action);

			container.Get<int>("P1").Value = 20;
			container.SetAllDirty();

			// 解除済みなので増えない
			Assert.AreEqual(3, count1);
			Assert.AreEqual(10, value1);

		}

		[Test]
		public void PropertyのHandleで解除出来る()
		{
			PropertyContainer container = new PropertyContainer();
			int count1 = 0;
			int value1 = 0;
			int count2 = 0;
			string value2 = "";
			var handel1 = container.Subscribe<int>("P1", (x) =>
			{
				count1++;
				value1 = x;
			});
			var handel2 = container.Subscribe<string>("P1", (x) =>
			{
				count2++;
				value2 = x;
			});

			container.Get<int>("P1").Value = 10;
			container.Get<string>("P1").Value = "Test";

			Assert.AreEqual(1, count1);
			Assert.AreEqual(10, value1);
			Assert.AreEqual(1, count2);
			Assert.AreEqual("Test", value2);

			container.SetAllDirty();
			container.SetDirty("P1");

			//カウントだけ増える
			Assert.AreEqual(3, count1);
			Assert.AreEqual(10, value1);
			Assert.AreEqual(3, count2);
			Assert.AreEqual("Test", value2);


			handel1.Dispose();
			handel2.Dispose();

			container.Get<int>("P1").Value = 20;
			container.Get<string>("P1").Value = "Test2";
			container.SetAllDirty();

			// 解除済みなので増えない
			Assert.AreEqual(3, count1);
			Assert.AreEqual(10, value1);
			Assert.AreEqual(3, count2);
			Assert.AreEqual("Test", value2);

#if UNITY_EDITOR
			Assert.AreEqual(2, container.GetAll().Count());
#endif

		}


	}
}