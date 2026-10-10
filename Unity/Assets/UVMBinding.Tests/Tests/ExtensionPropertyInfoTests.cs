using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UVMBinding.PropertyProvider;

namespace UVMBinding.Tests
{
	public class ExtensionPropertyInfoTests
	{
		static void SetField(object target, string name, object value)
		{
			for (var type = target.GetType(); type != null; type = type.BaseType)
			{
				var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
				if (field != null)
				{
					field.SetValue(target, value);
					return;
				}
			}
			Assert.Fail($"フィールドが見つからない: {name}");
		}

		class AddOneProvider : PropertyProvider<int, int>
		{
			public override int Provide(int value1) => value1 + 1;
		}

		class SumProvider : PropertyProvider<int, int, int>
		{
			public override int Combine(int value1, int value2) => value1 + value2;
		}

		[Test]
		public void 依存元が1件のProviderから取得できる()
		{
			var provider = new AddOneProvider();
			SetField(provider, "m_ValueKey", "Result");
			SetField(provider, "m_SourceKey1", "Src1");

			var info = provider.GetInfos().Single();

			Assert.AreEqual("Result", info.Path);
			Assert.AreEqual(typeof(int), info.Type);
			CollectionAssert.AreEqual(new[] { "Src1" }, info.SourcePaths);
		}

		[Test]
		public void 依存元が2件のProviderから取得できる()
		{
			var provider = new SumProvider();
			SetField(provider, "m_ValueKey", "Result");
			SetField(provider, "m_SourceKey1", "Src1");
			SetField(provider, "m_SourceKey2", "Src2");

			var info = provider.GetInfos().Single();

			CollectionAssert.AreEqual(new[] { "Src1", "Src2" }, info.SourcePaths);
		}

		[Test]
		public void CombineBoolProviderから取得できる()
		{
			var provider = new CombineBoolProvider();
			SetField(provider, "m_ValueKey", "Result");
			SetField(provider, "m_SourceKeys", new[] { "A", "B", "C" });

			var info = provider.GetInfos().Single();

			Assert.AreEqual("Result", info.Path);
			Assert.AreEqual(typeof(bool), info.Type);
			CollectionAssert.AreEqual(new[] { "A", "B", "C" }, info.SourcePaths);
		}

		[Test]
		public void CombineStringProviderから取得できる()
		{
			var provider = new CombineStringProvider();
			SetField(provider, "m_ValueKey", "Result");
			SetField(provider, "m_SourceKeys", new[] { "A", "B" });

			var info = provider.GetInfos().Single();

			Assert.AreEqual(typeof(string), info.Type);
			CollectionAssert.AreEqual(new[] { "A", "B" }, info.SourcePaths);
		}

		[Test]
		public void 依存元を持たない場合は空配列を返す()
		{
			var info = new ExtensionPropertyInfo("Result", typeof(int));

			Assert.IsNotNull(info.SourcePaths);
			Assert.IsEmpty(info.SourcePaths);
		}

		[Test]
		public void nullを渡しても空配列になる()
		{
			var info = new ExtensionPropertyInfo("Result", typeof(int), null);

			Assert.IsNotNull(info.SourcePaths);
			Assert.IsEmpty(info.SourcePaths);
		}
	}
}