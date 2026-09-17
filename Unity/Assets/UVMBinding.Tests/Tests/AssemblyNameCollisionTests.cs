using NUnit.Framework;
using UVMBinding.Core;
using UVMBinding.Fixtures;

namespace UVMBinding.Tests
{
	public class AssemblyNameCollisionTests
	{
		public class MarkerViewModel : ViewModel
		{
			[Bind] public string Value { get; set; }

			[Event] public System.Action OnMark { get; set; }
		}

		[Test]
		public void 先に並ぶCollision_UVMBindingがあってもBindが生成される()
		{
			Assert.AreEqual("Collision.UVMBinding", CollisionMarker.AssemblyName);

			var vm = new MarkerViewModel { Value = "A" };
			IViewModel model = vm;

			Assert.IsTrue(model.TryGetAs<string>("Value", out var value), "[Bind] のコード生成が走っている");
			Assert.AreEqual("A", value);
		}

		[Test]
		public void 先に並ぶCollision_UVMBindingがあってもEventが生成される()
		{
			var vm = new MarkerViewModel();
			int count = 0;
			vm.OnMark = () => count++;

			vm.Event.Publish("OnMark");

			Assert.AreEqual(1, count, "[Event] のコード生成が走っている");
		}
	}
}
