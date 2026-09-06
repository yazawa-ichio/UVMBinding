using System;
using NUnit.Framework;
using UVMBinding.Core;

namespace UVMBinding.Tests
{
	[TestFixture]
	public class ReflectionTests
	{
		private class TestDataSource
		{
			public int IntValue { get; set; }
			public string StringValue { get; set; }
			public bool BoolValue { get; set; }

			public void TestMethod()
			{
				IntValue = 100;
			}

			public void TestMethodWithArgs(int value)
			{
				IntValue = value;
			}
		}

		private class TestDataSourceWithDirty : INotifyDirtyEvent
		{
			public int IntValue { get; set; }
			public string StringValue { get; set; }

			public NotifyDirtyEventHandler DitryHandler { get; set; }

			public void RaiseDirty(string path = null)
			{
				DitryHandler?.Invoke(new NotifyDirtyEvent
				{
					Path = path
				});
			}
		}

		[Test]
		public void Constructor_ThrowsArgumentNullException_WhenDataSourceIsNull()
		{
			Assert.Throws<ArgumentNullException>(() => new ReflectionViewModel(null));
		}

		[Test]
		public void OnBind_ReadsPropertyValueFromDataSource()
		{
			var dataSource = new TestDataSource
			{
				IntValue = 42, StringValue = "test"
			};
			var viewModel = new ReflectionViewModel(dataSource) as IViewModel;

			viewModel.OnBind();
			var intProp = viewModel.Property.GetOrCreate<int>("IntValue");
			var stringProp = viewModel.Property.GetOrCreate<string>("StringValue");

			Assert.AreEqual(42, intProp.GetValue<int>());
			Assert.AreEqual("test", stringProp.GetValue<string>());

			viewModel.OnUnbind();
		}

		[Test]
		public void PropertyChange_UpdatesDataSource()
		{
			var dataSource = new TestDataSource
			{
				IntValue = 42
			};
			var viewModel = new ReflectionViewModel(dataSource) as IViewModel;

			viewModel.OnBind();
			var intProp = viewModel.Property.GetOrCreate<int>("IntValue");
			intProp.TrySetValue<int>(100);

			Assert.AreEqual(100, dataSource.IntValue);

			viewModel.OnUnbind();
		}

		[Test]
		public void RegisteredGetter_IsUsedForPropertyRead()
		{
			var dataSource = new TestDataSource
			{
				IntValue = 42
			};
			ReflectionViewModel.Register<TestDataSource, int>("CustomValue", ds => ds.IntValue * 2);

			var viewModel = new ReflectionViewModel(dataSource) as IViewModel;
			viewModel.OnBind();
			Assert.Throws<InvalidOperationException>(() =>
			{
				var prop = viewModel.Property.GetOrCreate<int>("CustomValue");
				Assert.Fail();
			});

			viewModel.OnUnbind();
		}

		[Test]
		public void RegisteredSetter_IsUsedForPropertyWrite()
		{
			var dataSource = new TestDataSource
			{
				IntValue = 0
			};
			int capturedValue = 0;
			ReflectionViewModel.Register<TestDataSource, int>("CustomSetValue",
				(ds, val) =>
				{
					capturedValue = val;
					ds.IntValue = val * 2;
				},
				ds => ds.IntValue);

			var viewModel = new ReflectionViewModel(dataSource) as IViewModel;
			viewModel.OnBind();
			var prop = viewModel.Property.GetOrCreate<int>("CustomSetValue");
			prop.TrySetValue<int>(10);

			Assert.AreEqual(10, capturedValue);
			Assert.AreEqual(20, dataSource.IntValue);

			viewModel.OnUnbind();
		}

		[Test]
		public void DirtyEvent_UpdatesAllProperties()
		{
			var dataSource = new TestDataSourceWithDirty
			{
				IntValue = 42, StringValue = "initial"
			};
			var viewModel = new ReflectionViewModel(dataSource) as IViewModel;

			viewModel.OnBind();
			var intProp = viewModel.Property.GetOrCreate<int>("IntValue");
			var stringProp = viewModel.Property.GetOrCreate<string>("StringValue");

			dataSource.IntValue = 100;
			dataSource.StringValue = "updated";
			dataSource.RaiseDirty();

			Assert.AreEqual(100, intProp.GetValue<int>());
			Assert.AreEqual("updated", stringProp.GetValue<string>());

			viewModel.OnUnbind();
		}

		[Test]
		public void DirtyEventWithPath_UpdatesSpecificProperty()
		{
			var dataSource = new TestDataSourceWithDirty
			{
				IntValue = 42, StringValue = "initial"
			};
			var viewModel = new ReflectionViewModel(dataSource) as IViewModel;

			viewModel.OnBind();
			var intProp = viewModel.Property.GetOrCreate<int>("IntValue");
			var stringProp = viewModel.Property.GetOrCreate<string>("StringValue");

			Assert.AreEqual(42, intProp.GetValue<int>());
			Assert.AreEqual("initial", stringProp.GetValue<string>());

			dataSource.IntValue = 100;
			dataSource.StringValue = "updated";
			dataSource.RaiseDirty("IntValue");

			intProp.SetDirty();

			Assert.AreEqual(100, intProp.GetValue<int>());
			Assert.AreEqual("initial", stringProp.GetValue<string>());

			viewModel.OnUnbind();
		}

		[Test]
		public void PublishEvent_InvokesMethodOnDataSource()
		{
			var dataSource = new TestDataSource
			{
				IntValue = 0
			};
			var viewModel = new ReflectionViewModel(dataSource) as IViewModel;

			viewModel.OnBind();
			viewModel.Event.Publish("TestMethod");

			Assert.AreEqual(100, dataSource.IntValue);

			viewModel.OnUnbind();
		}

		[Test]
		public void Unbind_RemovesEventHandlers()
		{
			var dataSource = new TestDataSourceWithDirty
			{
				IntValue = 42
			};
			var viewModel = new ReflectionViewModel(dataSource) as IViewModel;

			viewModel.OnBind();
			var intProp = viewModel.Property.GetOrCreate<int>("IntValue");
			viewModel.OnUnbind();

			dataSource.IntValue = 100;
			dataSource.RaiseDirty();

			Assert.AreEqual(42, intProp.GetValue<int>());
		}
	}
}