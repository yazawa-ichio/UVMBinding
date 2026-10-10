using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UVMBinding.Semantics;
using UVMBinding.Semantics.Export;

namespace UVMBinding.EditorTests
{
	public class HtmlExporterTests
	{
		class ExportViewModel : ViewModel
		{
			[Bind] public string Label { get; set; }
			[Bind] public bool CanPress { get; set; }
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

		static void SetPrivate(Object target, string field, System.Action<SerializedProperty> apply)
		{
			var so = new SerializedObject(target);
			apply(so.FindProperty(field));
			so.ApplyModifiedPropertiesWithoutUndo();
		}

		static GameObject Child(GameObject parent, string name)
		{
			var go = new GameObject(name);
			go.transform.SetParent(parent.transform, false);
			return go;
		}

		View AddView(GameObject go)
		{
			var view = go.AddComponent<View>();
			SetPrivate(view, View.ReservationViewModelFieldName,
				p => p.stringValue = typeof(ExportViewModel).FullName);
			return view;
		}

		[SemanticRole(SemanticRole.Text)]
		class LabelBinder : Binder<string>
		{
			protected override void UpdateValue(string value) { }
		}

		[SemanticRole(SemanticRole.Image)]
		class PictureBinder : Binder<string>
		{
			protected override void UpdateValue(string value) { }
		}

		[SemanticRole(SemanticRole.None)]
		[GovernsState(GovernedState.Interactable)]
		class GateBinder : Binder<bool>
		{
			protected override void UpdateValue(bool value) { }
		}

		[SemanticRole(SemanticRole.Button)]
		class PressEvent : Core.ViewEventBase, Arguments.IHasEventArgument
		{
			[SerializeReference]
			public Arguments.EventArgument m_Argument = default;

			public override System.Type EventType() => m_Argument?.GetEventType();

			public Arguments.EventArgument GetArgument() => m_Argument;
		}

		static PressEvent AddEvent(GameObject go, string name)
		{
			var press = go.AddComponent<PressEvent>();
			SetPrivate(press, "m_EventName", p => p.stringValue = name);
			return press;
		}

		static T AddBinder<T>(GameObject go, string path) where T : Component
		{
			var binder = go.AddComponent<T>();
			SetPrivate(binder, "m_Path", p => p.stringValue = path);
			return binder;
		}

		string Export()
		{
			return HtmlExporter.Export(ViewTraversal.Capture(m_Root.GetComponent<View>(),
				ViewTraversal.Options.ForExport));
		}

		[Test]
		public void 画面はsectionになり型名を持つ()
		{
			m_Root = new GameObject("Screen");
			AddView(m_Root);

			Assert.AreEqual(
				$"<section id=\"Screen\" data-view-name=\"{typeof(ExportViewModel).FullName}\"></section>",
				Export());
		}

		[Test]
		public void 文字はoutputになりpathがnameに乗る()
		{
			m_Root = new GameObject("Screen");
			AddView(m_Root);
			AddBinder<LabelBinder>(Child(m_Root, "Title"), "Label");

			StringAssert.Contains("<output id=\"Screen/Title\" name=\"Label\">", Export());
		}

		[Test]
		public void イベントはbuttonになり引数がvalueに乗る()
		{
			m_Root = new GameObject("Screen");
			AddView(m_Root);
			var press = Child(m_Root, "Go").AddComponent<PressEvent>();
			SetPrivate(press, "m_EventName", p => p.stringValue = "OnGo");
			SetPrivate(press, nameof(PressEvent.m_Argument), p =>
				p.managedReferenceValue = new Arguments.CollectionIndexArgument { Index = 3 });

			StringAssert.Contains(
				"<button id=\"Screen/Go\" name=\"OnGo\" type=\"button\" value=\"3\""
				+ " data-consequence=\"unknown\">", Export());
		}

		[Test]
		public void 押せない結線はdisabledになる()
		{
			m_Root = new GameObject("Screen");
			var view = AddView(m_Root);
			var go = Child(m_Root, "Go");
			var press = go.AddComponent<PressEvent>();
			SetPrivate(press, "m_EventName", p => p.stringValue = "OnGo");
			var gate = AddBinder<GateBinder>(go, "CanPress");
			Assert.IsNotNull(gate);

			var vm = view.Attach<ExportViewModel>();
			vm.CanPress = false;
			var html = HtmlExporter.Export(ViewTraversal.Capture(view, ViewTraversal.Options.ForExport));

			StringAssert.Contains("name=\"OnGo\"", html);
			StringAssert.Contains(" disabled>", html);
		}

		[Test]
		public void 静的モードでは押せるかどうかが決まらない()
		{
			m_Root = new GameObject("Screen");
			AddView(m_Root);
			var go = Child(m_Root, "Go");
			var press = go.AddComponent<PressEvent>();
			SetPrivate(press, "m_EventName", p => p.stringValue = "OnGo");
			AddBinder<GateBinder>(go, "CanPress");

			StringAssert.DoesNotContain(" disabled", Export());
		}

		[Test]
		public void 画像はimgになりaltに名前が乗る()
		{
			m_Root = new GameObject("Screen");
			AddView(m_Root);
			AddBinder<PictureBinder>(Child(m_Root, "Scenery"), "Label");

			StringAssert.Contains("<img id=\"Screen/Scenery\" name=\"Label\" alt=", Export());
		}

		[Test]
		public void 画面に出ていない依存元は記録として残る()
		{
			m_Root = new GameObject("Screen");
			AddView(m_Root);
			var label = AddBinder<LabelBinder>(Child(m_Root, "Total"), "Label");
			var node = ViewTraversal.CaptureSelf(m_Root.GetComponent<View>(), ViewTraversal.Options.ForExport);
			Assert.IsNotNull(label);
			node.Elements[0].DependsOn = new[] { "Hidden" };

			StringAssert.Contains("<!-- unmapped: depends on Hidden which has no element -->",
				HtmlExporter.Export(node));
		}

		[Test]
		public void 依存元が画面にあればforがidを指す()
		{
			m_Root = new GameObject("Screen");
			AddView(m_Root);
			AddBinder<LabelBinder>(Child(m_Root, "Source"), "CanPress");
			AddBinder<LabelBinder>(Child(m_Root, "Total"), "Label");
			var node = ViewTraversal.CaptureSelf(m_Root.GetComponent<View>(), ViewTraversal.Options.ForExport);
			node.Elements[1].DependsOn = new[] { "CanPress" };

			StringAssert.Contains("for=\"Screen/Source\"", HtmlExporter.Export(node));
		}

		[Test]
		public void 記号は属性でも本文でも潰される()
		{
			m_Root = new GameObject("Screen");
			AddView(m_Root);
			AddBinder<LabelBinder>(Child(m_Root, "Title"), "a<b&\"c\"");

			var html = Export();
			StringAssert.Contains("name=\"a&lt;b&amp;&quot;c&quot;\"", html);
			StringAssert.DoesNotContain("name=\"a<b", html);
		}

		class ConsequenceViewModel : ViewModel
		{
			[Event, Consequence(ConsequenceKind.Purchase)]
			public System.Action OnBuy { get; set; }

			[Event] public System.Action OnGo { get; set; }
		}

		[Test]
		public void 注釈はdataconsequenceとして出る()
		{
			m_Root = new GameObject("Screen");
			var view = m_Root.AddComponent<View>();
			SetPrivate(view, View.ReservationViewModelFieldName,
				p => p.stringValue = typeof(ConsequenceViewModel).FullName);
			AddEvent(Child(m_Root, "Buy"), "OnBuy");
			AddEvent(Child(m_Root, "Go"), "OnGo");

			var options = ViewTraversal.Options.ForStaticExport;
			options.ResolveDeclaration = UVMBinding.Export.HtmlExportCommand.FindDeclaration;
			var html = HtmlExporter.Export(ViewTraversal.Capture(view, options));

			StringAssert.Contains("name=\"OnBuy\" type=\"button\" data-consequence=\"purchase\"", html);
			StringAssert.Contains("name=\"OnGo\" type=\"button\" data-consequence=\"unknown\"", html);
		}

		[Test]
		public void 宣言を引けなければすべてunknownになる()
		{
			m_Root = new GameObject("Screen");
			var view = m_Root.AddComponent<View>();
			SetPrivate(view, View.ReservationViewModelFieldName,
				p => p.stringValue = typeof(ConsequenceViewModel).FullName);
			AddEvent(Child(m_Root, "Buy"), "OnBuy");

			StringAssert.Contains("data-consequence=\"unknown\"",
				HtmlExporter.Export(ViewTraversal.Capture(view, ViewTraversal.Options.ForStaticExport)));
		}

		[SemanticRole(SemanticRole.List)]
		class RowsBinder : Binder<string>
		{
			protected override void UpdateValue(string value) { }
		}

		[Test]
		public void 一覧の下にあるViewは行として中に入る()
		{
			m_Root = new GameObject("Screen");
			AddView(m_Root);
			var list = Child(m_Root, "Items");
			AddBinder<RowsBinder>(list, "Label");
			AddView(Child(list, "Row1"));
			AddView(Child(list, "Row2"));

			var html = Export();

			StringAssert.Contains(
				"<li id=\"Screen/Items/Row1\" data-view-name=\""
				+ typeof(ExportViewModel).FullName + "\" aria-posinset=\"1\" aria-setsize=\"2\">", html);
			StringAssert.Contains("aria-posinset=\"2\" aria-setsize=\"2\"", html);
			Assert.Less(html.IndexOf("<li ", System.StringComparison.Ordinal),
				html.IndexOf("</ul>", System.StringComparison.Ordinal), "行が ul の中にある");
		}

		[Test]
		public void 一覧に属さない子Viewはsectionのまま()
		{
			m_Root = new GameObject("Screen");
			AddView(m_Root);
			AddView(Child(m_Root, "Embedded"));

			StringAssert.Contains("<section id=\"Screen/Embedded\"", Export());
		}

		[SemanticRole(SemanticRole.None)]
		class EffectBinder : Binder<string>
		{
			protected override void UpdateValue(string value) { }
		}

		[Test]
		public void 役割を持たない要素は記録として残る()
		{
			m_Root = new GameObject("Screen");
			AddView(m_Root);
			AddBinder<EffectBinder>(Child(m_Root, "Moving"), "Label");

			StringAssert.Contains(
				"<!-- unmapped: no role: Screen/Moving bind=Label (EffectBinder) -->", Export());
		}

		[Test]
		public void 畳み込まれた状態は記録しない()
		{
			m_Root = new GameObject("Screen");
			AddView(m_Root);
			var button = Child(m_Root, "Go");
			AddEvent(button, "OnGo");
			AddBinder<GateBinder>(button, "CanPress");

			var html = Export();
			StringAssert.Contains("name=\"OnGo\"", html);
			StringAssert.DoesNotContain("no role", html);
		}

		[Test]
		public void 畳み込む先が無い状態は記録として残る()
		{
			m_Root = new GameObject("Screen");
			AddView(m_Root);
			AddBinder<GateBinder>(Child(m_Root, "Panel"), "CanPress");

			StringAssert.Contains(
				"<!-- unmapped: no role: Screen/Panel bind=CanPress (GateBinder) -->", Export());
		}

		[Test]
		public void 同じGameObjectの2つ目はidが分かれる()
		{
			m_Root = new GameObject("Screen");
			AddView(m_Root);
			var go = Child(m_Root, "Both");
			AddBinder<LabelBinder>(go, "Label");
			AddBinder<PictureBinder>(go, "CanPress");

			var html = Export();
			StringAssert.Contains("id=\"Screen/Both\"", html);
			StringAssert.Contains("id=\"Screen/Both#1\"", html);
		}

		[Test]
		public void 記録は本文に混ざらない()
		{
			m_Root = new GameObject("Screen");
			AddView(m_Root);
			AddBinder<LabelBinder>(Child(m_Root, "Total"), "Label");
			var node = ViewTraversal.CaptureSelf(m_Root.GetComponent<View>(), ViewTraversal.Options.ForExport);
			node.Elements[0].DependsOn = new[] { "Hidden" };

			var html = HtmlExporter.Export(node);
			Assert.Less(html.IndexOf("<!-- unmapped: depends on", System.StringComparison.Ordinal),
				html.IndexOf("<output", System.StringComparison.Ordinal));
			StringAssert.DoesNotContain("<!--", html.Substring(
				html.IndexOf("<output", System.StringComparison.Ordinal)));
		}

		[Test]
		public void forは同じpathの要素を全部指す()
		{
			m_Root = new GameObject("Screen");
			AddView(m_Root);
			AddBinder<LabelBinder>(Child(m_Root, "SourceA"), "CanPress");
			AddBinder<LabelBinder>(Child(m_Root, "SourceB"), "CanPress");
			AddBinder<LabelBinder>(Child(m_Root, "Total"), "Label");
			var node = ViewTraversal.CaptureSelf(m_Root.GetComponent<View>(), ViewTraversal.Options.ForExport);
			node.Elements[2].DependsOn = new[] { "CanPress" };

			StringAssert.Contains("for=\"Screen/SourceA Screen/SourceB\"", HtmlExporter.Export(node));
		}

		[Test]
		public void nullでも落ちない()
		{
			Assert.AreEqual(string.Empty, HtmlExporter.Export(null));
		}
	}
}
