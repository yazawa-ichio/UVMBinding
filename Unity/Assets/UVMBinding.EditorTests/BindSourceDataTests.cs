using NUnit.Framework;
using UVMBinding.Drawer;

namespace UVMBinding.EditorTests
{
	public class BindSourceDataTests
	{
		class SourceViewModel : ViewModel
		{
			[Bind] public bool Visible { get; set; }
			[Bind("別名")] public string Label { get; set; }
			[Event] public System.Action OnClick { get; set; }
			public string 宣言していない { get; set; }
		}

		public class SourceData
		{
			public bool Field;
			public string Property { get; set; }
			public void OnTap() { }
			public void OnValue(int value) { }
			public void 引数が2つ(int a, int b) { }
			bool 非公開 { get; set; }
		}

		public class DerivedData : SourceData
		{
			public int Extra;
		}

		[Test]
		public void 型名はIViewModel派生に限定されず解決できる()
		{
			Assert.AreEqual(typeof(SourceData), BindSourceData.FindType(typeof(SourceData).FullName));
			Assert.AreEqual(typeof(GeneralViewModel), BindSourceData.FindType(typeof(GeneralViewModel).FullName));
			Assert.IsNull(BindSourceData.FindType("Not.Exist.Type"));
			Assert.IsNull(BindSourceData.FindType(""));
		}

		[Test]
		public void ViewModelはBind属性のパスを出す()
		{
			var paths = BindSourceData.GetBindPaths(typeof(SourceViewModel));

			CollectionAssert.Contains(paths, "Visible");
			CollectionAssert.Contains(paths, "別名", "属性のパスを使う");
			CollectionAssert.DoesNotContain(paths, "Label", "プロパティ名ではない");
			CollectionAssert.DoesNotContain(paths, "宣言していない");
		}

		[Test]
		public void ViewModelはEvent属性のパスを出す()
		{
			var paths = BindSourceData.GetEventPaths(typeof(SourceViewModel));

			CollectionAssert.Contains(paths, "OnClick");
			CollectionAssert.DoesNotContain(paths, "Visible");
		}

		[Test]
		public void POCOはフィールドとプロパティを出す()
		{
			var paths = BindSourceData.GetBindPaths(typeof(SourceData));

			CollectionAssert.Contains(paths, "Field");
			CollectionAssert.Contains(paths, "Property");
			CollectionAssert.DoesNotContain(paths, "非公開", "候補には公開メンバだけを出す");
		}

		[Test]
		public void POCOの自動プロパティの裏フィールドは出さない()
		{
			var paths = BindSourceData.GetBindPaths(typeof(SourceData));

			foreach (var path in paths)
			{
				Assert.IsFalse(path.Contains("<"), $"裏フィールドが出ている: {path}");
			}
		}

		[Test]
		public void POCOは基底のメンバも出す()
		{
			var paths = BindSourceData.GetBindPaths(typeof(DerivedData));

			CollectionAssert.Contains(paths, "Extra");
			CollectionAssert.Contains(paths, "Field", "基底のメンバも候補になる");
		}

		[Test]
		public void POCOは引数が1つ以下のメソッドを出す()
		{
			var paths = BindSourceData.GetEventPaths(typeof(SourceData));

			CollectionAssert.Contains(paths, "OnTap");
			CollectionAssert.Contains(paths, "OnValue");
			CollectionAssert.DoesNotContain(paths, "引数が2つ", "ReflectionViewModel が呼べない");
			CollectionAssert.DoesNotContain(paths, "get_Property", "プロパティの実体は出さない");
		}

		[Test]
		public void 型がnullなら空を返す()
		{
			CollectionAssert.IsEmpty(BindSourceData.GetBindPaths(null));
			CollectionAssert.IsEmpty(BindSourceData.GetEventPaths(null));
		}
	}
}
