using NUnit.Framework;
using UVMBinding.Drawer;
using UVMBinding.Lint;

namespace UVMBinding.EditorTests
{
	public class DependsOnMetadataTests
	{
		public class MetadataViewModel : ViewModel
		{
			[Bind] public string First { get; set; }
			[Bind] public string Last { get; set; }

			[Bind, DependsOn(nameof(First), nameof(Last))]
			public string FullName => $"{First} {Last}";

			[Bind] public string Plain { get; set; }
		}

		[Test]
		public void 宣言された依存元が取得できる()
		{
			var data = ViewModelData.Get(typeof(MetadataViewModel));

			var full = System.Array.Find(data.Properties, x => x.Path == "FullName");
			Assert.IsNotNull(full);
			CollectionAssert.AreEqual(new[] { "First", "Last" }, full.SourcePaths);
		}

		[Test]
		public void 宣言が無ければ空になる()
		{
			var data = ViewModelData.Get(typeof(MetadataViewModel));

			var plain = System.Array.Find(data.Properties, x => x.Path == "Plain");
			CollectionAssert.IsEmpty(plain.SourcePaths);
		}

		[Test]
		public void バインド元としても依存元を持つ()
		{
			var source = new ViewModelBindSource(typeof(MetadataViewModel));

			Assert.IsTrue(source.DependsOn.ContainsKey("FullName"));
			CollectionAssert.AreEqual(new[] { "First", "Last" }, source.DependsOn["FullName"]);
			Assert.IsTrue(source.HasBind("FullName"), "派生値もバインドできる");
		}
	}
}
