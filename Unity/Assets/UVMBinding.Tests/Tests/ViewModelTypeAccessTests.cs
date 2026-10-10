using NUnit.Framework;
using UVMBinding.Core;

namespace UVMBinding.Tests
{
	public class ViewModelTypeAccessTests
	{
		class Animal { }
		class Dog : Animal { }
		class Cat : Animal { }

		[Test]
		public void 完全一致で読み書きできる()
		{
			var vm = new GeneralViewModel();
			vm.Set("P1", 10);

			Assert.AreEqual(10, vm.Get<int>("P1"));
		}

		[Test]
		public void 代入できる向きの書き込みは通る()
		{
			var vm = new GeneralViewModel();
			vm.Set<Animal>("Pet", new Animal());

			Assert.DoesNotThrow(() => vm.Set("Pet", new Dog()));
			Assert.IsInstanceOf<Dog>(vm.Get<Animal>("Pet"));
		}

		[Test]
		public void 代入できない向きの書き込みは例外になる()
		{
			var vm = new GeneralViewModel();
			vm.Set("Pet", new Dog());

			Assert.Throws<System.InvalidCastException>(() => vm.Set<Animal>("Pet", new Animal()));
		}

		[Test]
		public void 代入できる向きの読み取りは通る()
		{
			var vm = new GeneralViewModel();
			vm.Set("Pet", new Dog());

			Assert.IsInstanceOf<Dog>(vm.Get<Animal>("Pet"));
		}

		[Test]
		public void 代入できない向きの読み取りは例外になる()
		{
			var vm = new GeneralViewModel();
			vm.Set<Animal>("Pet", new Dog());

			Assert.Throws<System.InvalidCastException>(() => vm.Get<Dog>("Pet"));
		}

		[Test]
		public void 無関係な型は例外になる()
		{
			var vm = new GeneralViewModel();
			vm.Set("P1", 10);

			Assert.Throws<System.InvalidCastException>(() => vm.Set("P1", "Text"));
			Assert.Throws<System.InvalidCastException>(() => vm.Get<string>("P1"));
		}

		[Test]
		public void TryGetAsは実行時の値でダウンキャストできる()
		{
			var vm = new GeneralViewModel();
			vm.Set<Animal>("Pet", new Dog());

			Assert.IsTrue(vm.TryGetAs<Dog>("Pet", out var dog));
			Assert.IsNotNull(dog);
		}

		[Test]
		public void TryGetAsは実行時の値が違えば失敗する()
		{
			var vm = new GeneralViewModel();
			vm.Set<Animal>("Pet", new Cat());

			Assert.IsFalse(vm.TryGetAs<Dog>("Pet", out _));
		}

		[Test]
		public void TrySetは型が合わなければfalseを返す()
		{
			var vm = new GeneralViewModel();
			vm.Set("P1", 10);

			Assert.IsFalse(vm.TrySet("P1", "Text"));
			Assert.AreEqual(10, vm.Get<int>("P1"));
		}

		[Test]
		public void TrySetは代入できる向きなら通る()
		{
			var vm = new GeneralViewModel();
			vm.Set<Animal>("Pet", new Animal());

			Assert.IsTrue(vm.TrySet("Pet", new Dog()));
		}

		[Test]
		public void UnSetすると別の型で登録し直せる()
		{
			var vm = new GeneralViewModel();
			vm.Set("P1", 10);

			Assert.IsTrue(vm.UnSet("P1"));

			Assert.DoesNotThrow(() => vm.Set("P1", "Text"));
			Assert.AreEqual("Text", vm.Get<string>("P1"));
		}

		[Test]
		public void UnSetしていなければ別の型で登録できない()
		{
			var vm = new GeneralViewModel();
			vm.Set("P1", 10);

			Assert.Throws<System.InvalidCastException>(() => vm.Set("P1", "Text"));
		}

		[Test]
		public void UnSetは購読を解除しない()
		{
			var vm = new GeneralViewModel();
			IViewModel ivm = vm;
			vm.Set("P1", 10);

			int received = 0;
			ivm.Property.Subscribe<int>("P1", x => received = x);

			Assert.IsTrue(vm.UnSet("P1"));
			vm.Set("P1", 20);

			Assert.AreEqual(20, received);
		}

		[Test]
		public void 未登録のPathのUnSetはfalseを返す()
		{
			var vm = new GeneralViewModel();

			Assert.IsFalse(vm.UnSet("P1"));
		}
	}
}