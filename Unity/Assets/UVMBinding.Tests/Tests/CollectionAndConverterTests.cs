using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UVMBinding.Converters;
using UVMBinding.Core;

namespace UVMBinding.Tests
{

	public class CollectionAndConverterTests
	{
		public CollectionAndConverterTests()
		{
			Logger.Log.Level = Logger.LogLevel.All;
		}

		[Test]
		public void ValueCollection_Enumerates_WhenUsedInForeach()
		{
			var vm = new BindTestViewModel();
			vm.Collection.Add(1);
			vm.Collection.Add(2);
			vm.Collection.Add(3);

			var ret = new List<int>();
			foreach (var value in vm.Collection)
			{
				ret.Add(value);
			}
			CollectionAssert.AreEqual(new[] { 1, 2, 3 }, ret);
		}

		[Test]
		public void Collection_Enumerates_WhenUsedAsNonGenericEnumerable()
		{
			var vm = new CollectionTestViewModel();
			vm.List.Add(new CollectionItemViewModel { No = 1 });
			vm.List.Add(new CollectionItemViewModel { No = 2 });

			int count = 0;
			foreach (var item in (IEnumerable)vm.List)
			{
				Assert.IsInstanceOf<CollectionItemViewModel>(item, "非ジェネリックの列挙でも要素型が保たれる");
				count++;
			}
			Assert.AreEqual(2, count);
		}

		[Test]
		public void Collection_OnChanged_IsRaised_WhenItemAdded()
		{
			var vm = new CollectionTestViewModel();
			int count = 0;
			vm.List.OnChanged += (_) => count++;

			vm.List.Add(new CollectionItemViewModel());
			Assert.Greater(count, 0, "追加で通知される");

			int afterFirst = count;
			vm.List.Add(new CollectionItemViewModel());
			Assert.AreEqual(afterFirst + 1, count, "2 回目以降は 1 追加につき 1 回");

			vm.List.RemoveAt(0);
			Assert.AreEqual(afterFirst + 2, count, "削除でも通知される");
		}

		[Test]
		public void Collection_RegistersSingleProperty_WhenSubscribedAndWritten()
		{
			var vm = new CollectionTestViewModel();
			vm.List.Add(new CollectionItemViewModel());

			var ivm = (IViewModel)vm;
			int count = 0;
			foreach (var prop in ivm.Property.GetAll())
			{
				if (prop.Path == nameof(CollectionTestViewModel.List)) count++;
			}
			Assert.AreEqual(1, count, "購読と書き込みの型が揃っており、同一パスに 1 つだけ登録される");
		}

		[Test]
		public void CopyTo_DoesNotNotify_BecauseItIsReadOperation()
		{
			var vm = new CollectionTestViewModel();
			vm.List.Add(new CollectionItemViewModel());

			int count = 0;
			vm.List.OnChanged += (_) => count++;

			var array = new CollectionItemViewModel[1];
			vm.List.CopyTo(array, 0);

			Assert.AreEqual(0, count, "読み取り操作では通知しない");
			Assert.IsNotNull(array[0]);
		}
	}

	public class BoolSwitchTests
	{
		class IntSwitch : BoolSwitchBase<int> { }

		[Test]
		public void BoolSwitch_InverseConvert_Succeeds_WhenValueMatches()
		{
			var converter = new IntSwitch();
			int on = converter.Convert(true);

			bool ret = false;
			Assert.IsTrue(converter.TryInverseConvert(on, ref ret), "On 側の値から逆変換できる");
			Assert.IsTrue(ret);
		}

		[Test]
		public void BoolSwitch_InverseConvert_Fails_WhenValueIsUnknown()
		{
			var converter = new IntSwitch();
			bool ret = false;
			Assert.IsFalse(converter.TryInverseConvert(12345, ref ret), "未知の値では false を返す");
		}
	}

	public class PropertyProviderTests
	{
		class AddOneProvider : PropertyProvider.PropertyProvider<int, int>
		{
			public override int Provide(int value1) => value1 + 1;
		}

		[Test]
		public void PropertyProvider_Init_DoesNotThrow_WhenSourceIsSingle()
		{
			var extension = new StubPropertyExtension();
			var provider = new AddOneProvider();

			Assert.DoesNotThrow(() => provider.Init(extension));
			Assert.AreEqual(1, extension.SubscribeCount, "依存元が購読される");
		}

		class StubPropertyExtension : PropertyProvider.IViewModelPropertyExtension
		{
			public int SubscribeCount;

			public void Set<T>(string name, T value) { }

			public void Subscribe<T>(string name, System.Action<T> action)
			{
				SubscribeCount++;
			}
		}
	}

}
