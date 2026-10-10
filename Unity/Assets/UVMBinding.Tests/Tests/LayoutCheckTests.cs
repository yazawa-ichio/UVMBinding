using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UVMBinding.Semantics;

namespace UVMBinding.Tests
{
	public class LayoutCheckTests
	{
		public class LayoutViewModel : ViewModel
		{
			[Bind] public string Caption { get; set; } = "文字";
		}

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

		View Build()
		{
			m_Root = new GameObject("Screen", typeof(RectTransform));
			return m_Root.AddComponent<View>();
		}

		GameObject Text(GameObject parent, string name, Vector2 position, Vector2 size)
		{
			var go = new GameObject(name, typeof(RectTransform));
			go.transform.SetParent(parent.transform, false);
			var rect = (RectTransform)go.transform;
			rect.anchorMin = rect.anchorMax = new Vector2(0, 0);
			rect.pivot = Vector2.zero;
			rect.anchoredPosition = position;
			rect.sizeDelta = size;
			go.AddComponent<UnityEngine.UI.Text>();
			go.AddComponent<Binders.TextBinder>().ChangePath("Caption");
			return go;
		}

		static List<LayoutIssue> Check(View view)
		{
			var issues = new List<LayoutIssue>();
			LayoutCheck.Run(ViewTraversal.Capture(view, ViewTraversal.Options.ForExport), issues);
			return issues;
		}

		[Test]
		public void 大きさが0の要素を拾う()
		{
			var view = Build();
			Text(m_Root, "Empty", Vector2.zero, Vector2.zero);

			var issues = Check(view);

			Assert.AreEqual(1, issues.Count, string.Join(" / ", issues));
			Assert.AreEqual(LayoutIssueKind.ZeroSize, issues[0].Kind);
			Assert.AreEqual("Screen/Empty", issues[0].Id);
		}

		[Test]
		public void 出ていない要素は見ない()
		{
			var view = Build();
			Text(m_Root, "Empty", Vector2.zero, Vector2.zero).SetActive(false);

			CollectionAssert.IsEmpty(Check(view));
		}

		[Test]
		public void 兄弟の文字どうしの重なりを拾う()
		{
			var view = Build();
			Text(m_Root, "Label", new Vector2(0, 0), new Vector2(100, 20));
			Text(m_Root, "Value", new Vector2(50, 0), new Vector2(100, 20));

			var issues = Check(view);

			Assert.AreEqual(1, issues.Count, string.Join(" / ", issues));
			Assert.AreEqual(LayoutIssueKind.Overlap, issues[0].Kind);
		}

		[Test]
		public void 離れていれば拾わない()
		{
			var view = Build();
			Text(m_Root, "Label", new Vector2(0, 0), new Vector2(100, 20));
			Text(m_Root, "Value", new Vector2(120, 0), new Vector2(100, 20));

			CollectionAssert.IsEmpty(Check(view));
		}

		[Test]
		public void 文字以外との重なりは拾わない()
		{
			var view = Build();
			Text(m_Root, "Label", new Vector2(0, 0), new Vector2(100, 20));
			var panel = new GameObject("Panel", typeof(RectTransform));
			panel.transform.SetParent(m_Root.transform, false);
			var rect = (RectTransform)panel.transform;
			rect.anchorMin = rect.anchorMax = Vector2.zero;
			rect.pivot = Vector2.zero;
			rect.sizeDelta = new Vector2(200, 40);
			panel.AddComponent<Image>();
			panel.AddComponent<Binders.ImageSpriteBinder>().ChangePath("Caption");

			CollectionAssert.IsEmpty(Check(view));
		}

		[Test]
		public void 親が違う文字は拾わない()
		{
			var view = Build();
			var box = new GameObject("Box", typeof(RectTransform));
			box.transform.SetParent(m_Root.transform, false);
			Text(m_Root, "Label", new Vector2(0, 0), new Vector2(100, 20));
			Text(box, "Value", new Vector2(50, 0), new Vector2(100, 20));

			CollectionAssert.IsEmpty(Check(view));
		}

		[Test]
		public void nullでも落ちない()
		{
			var issues = new List<LayoutIssue>();
			Assert.DoesNotThrow(() => LayoutCheck.Run(null, issues));
			CollectionAssert.IsEmpty(issues);
		}
	}
}
