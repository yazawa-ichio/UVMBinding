using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UVMBinding.Export;
using UVMBinding.Semantics;
using UVMBinding.Semantics.Export;

namespace UVMBinding.EditorTests
{
	public class ScriptExporterTests
	{
		const string ShowcaseScene = "Assets/UVMBinding.Showcase/Scenes/Showcase.unity";
		const string SampleScene = "Assets/UVMBinding.Sample/Scenes/Sample.unity";

		GameObject m_Root;

		[TearDown]
		public void TearDown()
		{
			if (m_Root != null)
			{
				Object.DestroyImmediate(m_Root);
				m_Root = null;
			}
		}

		static string Page(string scenePath, string idEnds)
		{
			var pages = HtmlExportCommand.Export(EditorSceneManager.OpenScene(scenePath));
			var page = pages.Find(x => x.Id.EndsWith(idEnds, System.StringComparison.Ordinal));
			Assert.IsNotNull(page.Html, idEnds);
			return page.Html;
		}

		static string Graph(string html)
		{
			var match = Regex.Match(html, "<script type=\"application/json\"[^>]*>(.*?)</script>",
				RegexOptions.Singleline);
			Assert.IsTrue(match.Success, "グラフが無い");
			return match.Groups[1].Value;
		}

		[Test]
		public void 計算する側を出さない()
		{
			foreach (var page in HtmlExportCommand.Export(EditorSceneManager.OpenScene(ShowcaseScene)))
			{
				StringAssert.DoesNotContain("addEventListener", page.Html, page.Id);
				StringAssert.DoesNotContain("<script>", page.Html, page.Id);
			}
		}

		[Test]
		public void グラフがJSONとして読める()
		{
			foreach (var page in HtmlExportCommand.Export(EditorSceneManager.OpenScene(ShowcaseScene)))
			{
				var text = Graph(page.Html);
				Assert.AreEqual(CountOf(text, '{'), CountOf(text, '}'), page.Id);
				Assert.AreEqual(CountOf(text, '['), CountOf(text, ']'), page.Id);
				StringAssert.StartsWith("\n{", text, page.Id);
			}
		}

		static int CountOf(string text, char c)
		{
			var count = 0;
			var inString = false;
			for (int i = 0; i < text.Length; i++)
			{
				if (text[i] == '"' && (i == 0 || text[i - 1] != (char)92))
				{
					inString = !inString;
				}
				else if (!inString && text[i] == c)
				{
					count++;
				}
			}
			return count;
		}

		[Test]
		public void Providerの演算は宣言として出る()
		{
			var graph = Graph(Page(ShowcaseScene, "05 派生値"));

			StringAssert.Contains(
				"{ \"path\": \"BothFlags\", \"op\": \"all\", \"sources\": [\"FlagA\", \"FlagB\"] }", graph);
			StringAssert.Contains("\"format\": \"{0} / Lv.{1}\"", graph);
		}

		[Test]
		public void DependsOnは依存だけが出て計算は出ない()
		{
			var graph = Graph(Page(SampleScene, "GrowthView"));

			StringAssert.Contains(
				"{ \"path\": \"Attack\", \"sources\": [\"Strength\", \"WeaponAttack\"] }", graph);
			StringAssert.Contains("\"derived\": []", graph);
			StringAssert.DoesNotContain("\"op\"", graph);
		}

		[Test]
		public void イベントの効果は書かない()
		{
			var graph = Graph(Page(SampleScene, "FieldView"));

			StringAssert.DoesNotContain("OnChooseRoute", graph);
			StringAssert.DoesNotContain("OnCamp", graph);
		}

		class ParentViewModel : ViewModel
		{
			[Bind] public string Label { get; set; }
		}

		[SemanticRole(SemanticRole.Text)]
		class LabelBinder : Binder<string>
		{
			protected override void UpdateValue(string value) { }
		}

		static void SetPrivate(Object target, string field, System.Action<SerializedProperty> apply)
		{
			var so = new SerializedObject(target);
			apply(so.FindProperty(field));
			so.ApplyModifiedPropertiesWithoutUndo();
		}

		GameObject Child(GameObject parent, string name)
		{
			var go = new GameObject(name);
			go.transform.SetParent(parent.transform, false);
			return go;
		}

		View AddView(GameObject go)
		{
			var view = go.AddComponent<View>();
			SetPrivate(view, View.ReservationViewModelFieldName,
				p => p.stringValue = typeof(ParentViewModel).FullName);
			return view;
		}

		void AddLabel(GameObject go, string path)
		{
			var binder = go.AddComponent<LabelBinder>();
			SetPrivate(binder, "m_Path", p => p.stringValue = path);
		}

		[Test]
		public void 同じpathを読む要素は組として出る()
		{
			m_Root = new GameObject("Screen");
			var view = AddView(m_Root);
			AddLabel(Child(m_Root, "A"), "Label");
			AddLabel(Child(m_Root, "B"), "Label");

			var graph = ScriptExporter.Export(
				ViewTraversal.Capture(view, ViewTraversal.Options.ForExport));

			StringAssert.Contains("\"mirrors\": [\"Label\"]", graph);
		}

		[Test]
		public void 子Viewのpathを親に混ぜない()
		{
			m_Root = new GameObject("Screen");
			var view = AddView(m_Root);
			AddLabel(Child(m_Root, "A"), "Label");
			var row = Child(m_Root, "Row");
			AddView(row);
			AddLabel(Child(row, "B"), "Label");

			var graph = ScriptExporter.Export(
				ViewTraversal.Capture(view, ViewTraversal.Options.ForExport));

			StringAssert.Contains("\"mirrors\": []", graph);
			StringAssert.Contains("\"id\": \"Screen/Row\"", graph);
			StringAssert.Contains("Screen/Row/B", graph);
		}

		[Test]
		public void 記号は逃がした形で書かれる()
		{
			var quote = typeof(ScriptExporter).GetMethod("Quote",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
			var backslash = ((char)92).ToString();

			Assert.AreEqual("\"a" + backslash + "nb\"",
				quote.Invoke(null, new object[] { "a\nb" }));
			Assert.AreEqual("\"a" + backslash + "u003c/script\"",
				quote.Invoke(null, new object[] { "a</script" }));
			Assert.AreEqual("\"a" + backslash + backslash + "b\"",
				quote.Invoke(null, new object[] { "a" + backslash + "b" }));
		}
	}
}
