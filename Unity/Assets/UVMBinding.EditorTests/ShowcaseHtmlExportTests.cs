using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UVMBinding.Export;
using UVMBinding.Semantics.Export;

namespace UVMBinding.EditorTests
{
	public class ShowcaseHtmlExportTests
	{
		const string ShowcaseScene = "Assets/UVMBinding.Showcase/Scenes/Showcase.unity";
		const string SampleScene = "Assets/UVMBinding.Sample/Scenes/Sample.unity";

		static System.Collections.Generic.List<HtmlExportCommand.Page> Export(string path)
		{
			return HtmlExportCommand.Export(EditorSceneManager.OpenScene(path));
		}

		[Test]
		public void ショーケースの全画面が静的モードで出る()
		{
			var pages = Export(ShowcaseScene);

			Assert.Greater(pages.Count, 0, "1 画面も出ないなら走査か写像が壊れている");
			foreach (var page in pages)
			{
				StringAssert.StartsWith("<!doctype html>", page.Html, page.Id);
				StringAssert.Contains("<section ", page.Html, page.Id);
				StringAssert.Contains(HtmlExporter.ViewNameAttribute, page.Html, page.Id);
			}
		}

		[Test]
		public void 独自のdata属性は3種類だけ()
		{
			var allowed = new[]
			{
				" " + HtmlExporter.ViewNameAttribute,
				" " + HtmlExporter.ConsequenceAttribute,
				" " + SourceExporter.SourceAttribute,
			};
			foreach (var page in Export(ShowcaseScene))
			{
				foreach (Match match in Regex.Matches(page.Html, @"\sdata-[a-z-]+"))
				{
					CollectionAssert.Contains(allowed, match.Value, page.Id);
				}
			}
		}

		[Test]
		public void ショーケースに不可逆な操作の例がある()
		{
			var found = false;
			foreach (var page in Export(ShowcaseScene))
			{
				found |= page.Html.Contains("data-consequence=\"destructive\"");
			}
			Assert.IsTrue(found, "消える操作の例が 1 つも無い");
		}

		[Test]
		public void すべてのイベントが結果を持つ()
		{
			foreach (var page in Export(ShowcaseScene))
			{
				foreach (Match match in Regex.Matches(page.Html, @"<button[^>]*>"))
				{
					StringAssert.Contains(HtmlExporter.ConsequenceAttribute, match.Value, page.Id);
				}
			}
		}

		[Test]
		public void 演出系の結線が記録として残る()
		{
			var page = Export(ShowcaseScene).Find(x => x.Id.EndsWith("08 演出系 Binder"));

			foreach (var path in new[] { "Alpha", "Fill", "Tint", "Offset", "Scale", "Rotation", "Visible" })
			{
				StringAssert.Contains("no role: ", page.Html);
				StringAssert.Contains("bind=" + path + " ", page.Html, path);
			}
		}

		[Test]
		public void 画面のタグが閉じている()
		{
			foreach (var page in Export(ShowcaseScene))
			{
				var section = page.Html.Substring(page.Html.IndexOf("<section ", System.StringComparison.Ordinal));
				section = section.Substring(0, section.LastIndexOf("</section>", System.StringComparison.Ordinal) + 10);
				var xml = Regex.Replace(section, @"<(img|input)([^>]*?)>", "<$1$2/>");
				xml = Regex.Replace(xml, @"\s(disabled|hidden)(?=[\s>/])", " $1=\"$1\"");
				Assert.DoesNotThrow(() => System.Xml.Linq.XDocument.Parse(xml), page.Id);
			}
		}
	}
}
