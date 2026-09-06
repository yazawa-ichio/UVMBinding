using NUnit.Framework;
using UVMBinding.Core;

namespace UVMBinding.Tests
{
	public class DependsOnTests
	{
		public class NameViewModel : ViewModel
		{
			[Bind] public string First { get; set; } = "A";
			[Bind] public string Last { get; set; } = "B";

			[Bind, DependsOn(nameof(First), nameof(Last))]
			public string FullName => $"{First} {Last}";
		}

		public class ChainViewModel : ViewModel
		{
			[Bind] public int Source { get; set; } = 1;

			[Bind, DependsOn(nameof(Source))]
			public int Doubled => Source * 2;

			[Bind, DependsOn(nameof(Doubled))]
			public string Text => $"={Doubled}";
		}

		static object Stored(IViewModel vm, string path)
		{
			return vm.Property.TryGet(path, out var prop) ? prop.GetObject() : null;
		}

		[Test]
		public void 派生値がパスとして登録される()
		{
			var vm = new NameViewModel();

			Assert.AreEqual("A B", Stored(vm, "FullName"), "初期値が流れている");
		}

		[Test]
		public void 依存元を変えると派生値が更新される()
		{
			var vm = new NameViewModel();

			vm.First = "X";

			Assert.AreEqual("X B", Stored(vm, "FullName"));
		}

		[Test]
		public void 依存元が2つならどちらでも更新される()
		{
			var vm = new NameViewModel();

			vm.Last = "Y";
			Assert.AreEqual("A Y", Stored(vm, "FullName"));

			vm.First = "X";
			Assert.AreEqual("X Y", Stored(vm, "FullName"));
		}

		[Test]
		public void 連鎖が伝播する()
		{
			var vm = new ChainViewModel();
			Assert.AreEqual(2, Stored(vm, "Doubled"), "初期値");
			Assert.AreEqual("=2", Stored(vm, "Text"), "初期値");

			vm.Source = 5;

			Assert.AreEqual(10, Stored(vm, "Doubled"));
			Assert.AreEqual("=10", Stored(vm, "Text"), "連鎖の先まで届く");
		}

		[Test]
		public void 派生値は購読できる()
		{
			var vm = new NameViewModel();
			string received = null;
			((IViewModel)vm).Property.Subscribe<string>("FullName", x => received = x);

			vm.First = "Z";

			Assert.AreEqual("Z B", received);
		}

		[Test]
		public void 直接読んでも同じ値になる()
		{
			var vm = new NameViewModel();
			vm.First = "Q";

			Assert.AreEqual(vm.FullName, Stored(vm, "FullName"));
		}
	}
}
