using NUnit.Framework;
using UnityEditor.SceneManagement;
using UVMBinding.Export;
using UVMBinding.Semantics;
using UVMBinding.Semantics.Export;

namespace UVMBinding.EditorTests
{
	public class SourceEmbeddingTests
	{
		const string SampleScene = "Assets/UVMBinding.Sample/Scenes/Sample.unity";

		static string Page(string idEnds)
		{
			var pages = HtmlExportCommand.Export(EditorSceneManager.OpenScene(SampleScene));
			return pages.Find(x => x.Id.EndsWith(idEnds, System.StringComparison.Ordinal)).Html;
		}

		[Test]
		public void 宣言された型のソースが埋まる()
		{
			var html = Page("GrowthView");

			StringAssert.Contains("<script type=\"text/x-csharp\" data-source=\""
				+ "Assets/UVMBinding.Sample/Scripts/SampleViewModels.cs\">", html);
			StringAssert.Contains("public class GrowthViewModel : ViewModel", html);
			StringAssert.Contains("Strength * 2 + WeaponAttack", html);
		}

		[Test]
		public void 呼び出し先には辿らない()
		{
			var html = Page("GrowthView");

			StringAssert.DoesNotContain("class SampleGame", html);
			StringAssert.DoesNotContain("class Journey", html);
			StringAssert.DoesNotContain("class ShopViewModel", html);
		}

		[Test]
		public void 同じ型を二度転記しない()
		{
			var html = Page("ShopView");
			var first = html.IndexOf("class ShopItemViewModel", System.StringComparison.Ordinal);

			Assert.Greater(first, 0, "行の型も転記される");
			Assert.AreEqual(first,
				html.LastIndexOf("class ShopItemViewModel", System.StringComparison.Ordinal));
		}

		[Test]
		public void ソースが見つからなければ記録が残る()
		{
			Assert.AreEqual("<!-- unmapped: source not found for Missing.Type -->",
				SourceExporter.Export("Missing.Type", new ViewModelDeclaration()));
			Assert.AreEqual("<!-- unmapped: source not found for Missing.Type -->",
				SourceExporter.Export("Missing.Type", null));
		}

		[Test]
		public void 製品ビルドでは出さない()
		{
			var path = "Assets/UVMBinding.Export/Runtime/SourceExporter.cs";
			var text = System.IO.File.ReadAllText(path);

			StringAssert.Contains("#if UNITY_EDITOR || UVMBINDING_EXPORT", text);
			StringAssert.Contains("#if UNITY_EDITOR || DEVELOPMENT_BUILD", text);
		}

		[Test]
		public void 出力側は全部シンボルで囲われている()
		{
			foreach (var path in System.IO.Directory.GetFiles(
				"Assets/UVMBinding.Export/Runtime", "*.cs"))
			{
				StringAssert.Contains("#if UNITY_EDITOR || UVMBINDING_EXPORT",
					System.IO.File.ReadAllText(path), path);
			}
		}

		[Test]
		public void 入れ子の括弧で切れない()
		{
			const string Text = "class A { void M() { if (x) { } } }\nclass B { }";

			Assert.AreEqual("class A { void M() { if (x) { } } }",
				ViewModelSourceFinder.Extract(Text, "A"));
		}

		[Test]
		public void 文字列の中の括弧は数えない()
		{
			const string Text = "class A { string S => \"{0} }\"; }\nclass B { }";

			Assert.AreEqual("class A { string S => \"{0} }\"; }",
				ViewModelSourceFinder.Extract(Text, "A"));
		}

		[Test]
		public void 名前が前方一致する別の型を拾わない()
		{
			const string Text = "class AB { int x; }\nclass A { int y; }";

			StringAssert.Contains("int y", ViewModelSourceFinder.Extract(Text, "A"));
			StringAssert.DoesNotContain("int x", ViewModelSourceFinder.Extract(Text, "A"));
		}

		[Test]
		public void 直前の注釈と属性も含める()
		{
			const string Text = "class Z { }\n\n//なぜこうしたか\n[Serializable]\nclass A { }";

			var body = ViewModelSourceFinder.Extract(Text, "A");
			StringAssert.StartsWith("//なぜこうしたか", body);
			StringAssert.Contains("[Serializable]", body);
			StringAssert.DoesNotContain("class Z", body);
		}

		[Test]
		public void 見つからなければnull()
		{
			Assert.IsNull(ViewModelSourceFinder.Extract("class A { }", "B"));
		}
	}
}
