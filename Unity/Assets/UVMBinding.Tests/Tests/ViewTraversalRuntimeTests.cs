using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UVMBinding.Semantics;

namespace UVMBinding.Tests
{
	public class ViewTraversalRuntimeTests
	{
		public class TraversalViewModel : ViewModel
		{
			[Bind] public string Caption { get; set; } = "見出し";
			[Bind] public bool Visible { get; set; } = true;
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
			m_Root = new GameObject("Root");
			var view = m_Root.AddComponent<View>();

			var label = new GameObject("Label");
			label.transform.SetParent(m_Root.transform, false);
			label.AddComponent<Text>();
			var binder = label.AddComponent<Binders.TextBinder>();
			binder.ChangePath("Caption");

			var panel = new GameObject("Panel");
			panel.transform.SetParent(m_Root.transform, false);
			panel.AddComponent<Binders.ActiveBinder>().ChangePath("Visible");
			return view;
		}

		[Test]
		public void 動的モードでは値が入る()
		{
			var view = Build();
			view.Attach<TraversalViewModel>(vm => vm.Caption = "実値");
			view.TryUpdate();

			var node = ViewTraversal.Capture(view, ViewTraversal.Options.ForExport);

			Assert.AreEqual(1, node.Elements.Count, "役割を持たない ActiveBinder は落ちる");
			var element = node.Elements[0];
			Assert.AreEqual(SemanticRole.Text, element.Role);
			Assert.AreEqual("Caption", element.BindPath);
			Assert.AreEqual("実値", element.Value);
			Assert.AreEqual("実値", element.Label, "表示している文字列も取れる");
		}

		[Test]
		public void 動的モードはAttachされた型を返す()
		{
			var view = Build();
			view.Attach<TraversalViewModel>();

			var node = ViewTraversal.Capture(view, ViewTraversal.Options.ForExport);

			Assert.AreEqual(typeof(TraversalViewModel).FullName, node.ViewModelTypeName);
		}

		[Test]
		public void 静的モードは値を取らない()
		{
			var view = Build();
			view.Attach<TraversalViewModel>(vm => vm.Caption = "実値");
			view.TryUpdate();

			var node = ViewTraversal.Capture(view, ViewTraversal.Options.ForLint);

			var element = node.Elements.Find(x => x.BindPath == "Caption");
			Assert.IsNull(element.Value, "静的モードでは値を見ない");
		}
	}
}
