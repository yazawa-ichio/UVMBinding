using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UVMBinding.Semantics;

namespace UVMBinding.EditorTests
{
	public class ViewTraversalTests
	{
		class TraversalViewModel : ViewModel
		{
			[Bind] public bool Visible { get; set; }
			[Bind] public string Label { get; set; }
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
				p => p.stringValue = typeof(TraversalViewModel).FullName);
			return view;
		}

		static Binders.ActiveBinder AddBinder(GameObject go, string path)
		{
			var binder = go.AddComponent<Binders.ActiveBinder>();
			SetPrivate(binder, "m_Path", p => p.stringValue = path);
			return binder;
		}

		[Test]
		public void 入れ子のViewが階層になる()
		{
			m_Root = new GameObject("Root");
			var view = AddView(m_Root);
			var childGo = Child(m_Root, "Child");
			AddView(childGo);

			var node = ViewTraversal.Capture(view, ViewTraversal.Options.ForLint);

			Assert.AreEqual(1, node.Children.Count, node.ToString());
			Assert.AreEqual("Root/Child", node.Children[0].Id);
		}

		[Test]
		public void 子Viewの要素は親に現れない()
		{
			m_Root = new GameObject("Root");
			var view = AddView(m_Root);
			AddBinder(Child(m_Root, "Panel"), "Visible");
			var childGo = Child(m_Root, "Child");
			AddView(childGo);
			AddBinder(Child(childGo, "Inner"), "Visible");

			var node = ViewTraversal.Capture(view, ViewTraversal.Options.ForLint);

			Assert.AreEqual(1, node.Elements.Count, "親が持つのは自分の分だけ");
			Assert.AreEqual("Root/Panel", node.Elements[0].Id);
			Assert.AreEqual(1, node.Children[0].Elements.Count);
			Assert.AreEqual("Root/Child/Inner", node.Children[0].Elements[0].Id);
		}

		[Test]
		public void GameObjectの入れ子が要素の階層になる()
		{
			m_Root = new GameObject("Root");
			var view = AddView(m_Root);
			var outer = Child(m_Root, "Outer");
			AddBinder(outer, "Visible");
			AddBinder(Child(outer, "Inner"), "Visible");

			var node = ViewTraversal.Capture(view, ViewTraversal.Options.ForLint);

			Assert.AreEqual(1, node.Elements.Count);
			Assert.AreEqual(1, node.Elements[0].Children.Count);
			Assert.AreEqual("Root/Outer/Inner", node.Elements[0].Children[0].Id);
		}

		[Test]
		public void 役割を持たない要素を落とせる()
		{
			m_Root = new GameObject("Root");
			var view = AddView(m_Root);
			AddBinder(Child(m_Root, "Panel"), "Visible");

			var kept = ViewTraversal.Capture(view, ViewTraversal.Options.ForLint);
			var dropped = ViewTraversal.Capture(view, ViewTraversal.Options.ForExport);

			Assert.AreEqual(1, kept.Elements.Count, "検査は全要素を見る");
			CollectionAssert.IsEmpty(dropped.Elements, "出力は役割を持つものだけ");
		}

		[Test]
		public void 静的モードでは値が入らない()
		{
			m_Root = new GameObject("Root");
			var view = AddView(m_Root);
			AddBinder(Child(m_Root, "Panel"), "Visible");

			var node = ViewTraversal.Capture(view, ViewTraversal.Options.ForLint);

			Assert.IsNull(node.Elements[0].Value);
			Assert.AreEqual("Visible", node.Elements[0].BindPath);
		}

		[Test]
		public void 型宣言が読める()
		{
			m_Root = new GameObject("Root");
			var view = AddView(m_Root);

			var node = ViewTraversal.Capture(view, ViewTraversal.Options.ForLint);

			Assert.AreEqual(typeof(TraversalViewModel).FullName, node.ViewModelTypeName);
			Assert.AreEqual("Root", node.Label);
			Assert.IsFalse(node.IsModal);
		}

		[Test]
		public void 非活性の要素に印が付く()
		{
			m_Root = new GameObject("Root");
			var view = AddView(m_Root);
			var panel = Child(m_Root, "Panel");
			AddBinder(panel, "Visible");
			panel.SetActive(false);

			var node = ViewTraversal.Capture(view, ViewTraversal.Options.ForLint);

			Assert.IsTrue((node.Elements[0].State & ElementState.Hidden) != 0);
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

		[Test]
		public void 押せるかどうかを決めているpathが要素に残る()
		{
			m_Root = new GameObject("Root");
			var view = AddView(m_Root);
			var button = Child(m_Root, "Button");
			var gate = button.AddComponent<GateBinder>();
			SetPrivate(gate, "m_Path", p => p.stringValue = "CanPress");
			AddEvent(button, "OnPress");

			var node = ViewTraversal.Capture(view, ViewTraversal.Options.ForExport);

			Assert.AreEqual(1, node.Elements.Count, "残るのは役割を持つイベントだけ");
			Assert.AreEqual("OnPress", node.Elements[0].EventName);
			Assert.AreEqual("CanPress", node.Elements[0].InteractablePath,
				"押せるかどうかが結線で決まっていることは残る");
			Assert.IsNull(node.Elements[0].VisiblePath, "決めていない状態は空のまま");
		}

		[Test]
		public void 出ているかどうかを決めているpathが要素に残る()
		{
			m_Root = new GameObject("Root");
			var view = AddView(m_Root);
			var panel = Child(m_Root, "Panel");
			AddBinder(panel, "Visible");
			AddEvent(panel, "OnPress");

			var node = ViewTraversal.Capture(view, ViewTraversal.Options.ForExport);

			Assert.AreEqual(1, node.Elements.Count);
			Assert.AreEqual("Visible", node.Elements[0].VisiblePath);
			Assert.IsNull(node.Elements[0].InteractablePath);
		}

		[Test]
		public void イベントの引数は型と値の両方が残る()
		{
			m_Root = new GameObject("Root");
			var view = AddView(m_Root);
			var press = AddEvent(Child(m_Root, "Button"), "OnChoose");
			SetPrivate(press, nameof(PressEvent.m_Argument), p =>
				p.managedReferenceValue = new Arguments.CollectionIndexArgument { Index = 2 });

			var node = ViewTraversal.Capture(view, ViewTraversal.Options.ForExport);

			Assert.AreEqual(nameof(Arguments.CollectionIndexArgument), node.Elements[0].EventArgumentType);
			Assert.AreEqual("2", node.Elements[0].EventArgumentValue);
		}

		[Test]
		public void 読めない引数は値なしになるが型は残る()
		{
			m_Root = new GameObject("Root");
			var view = AddView(m_Root);
			var press = AddEvent(Child(m_Root, "Button"), "OnToggle");
			SetPrivate(press, nameof(PressEvent.m_Argument), p =>
				p.managedReferenceValue = new Arguments.ComponentEnabledArgument());

			var node = ViewTraversal.Capture(view, ViewTraversal.Options.ForExport);

			Assert.AreEqual(nameof(Arguments.ComponentEnabledArgument), node.Elements[0].EventArgumentType);
			Assert.IsNull(node.Elements[0].EventArgumentValue);
		}

		[Test]
		public void 同じ名前の兄弟はidが分かれる()
		{
			m_Root = new GameObject("Root");
			var view = AddView(m_Root);
			AddBinder(Child(m_Root, "Row"), "Visible");
			AddBinder(Child(m_Root, "Row"), "Visible");
			AddBinder(Child(m_Root, "Only"), "Visible");

			var node = ViewTraversal.Capture(view, ViewTraversal.Options.ForLint);

			Assert.AreEqual("Root/Row[0]", node.Elements[0].Id);
			Assert.AreEqual("Root/Row[1]", node.Elements[1].Id);
			Assert.AreEqual("Root/Only", node.Elements[2].Id);
		}

		[Test]
		public void 根のidは自分の名前だけになる()
		{
			m_Root = new GameObject("Canvas");
			var screen = Child(m_Root, "Screen");
			var view = AddView(screen);
			AddBinder(Child(screen, "Panel"), "Visible");

			var node = ViewTraversal.Capture(view, ViewTraversal.Options.ForLint);

			Assert.AreEqual("Screen", node.Id);
			Assert.AreEqual("Screen/Panel", node.Elements[0].Id);
		}

		[Test]
		public void ルートがnullなら空を返す()
		{
			Assert.IsNull(ViewTraversal.Capture(null, ViewTraversal.Options.ForLint));
			CollectionAssert.IsEmpty(ViewTraversal.CaptureAll(null, ViewTraversal.Options.ForLint));
		}

		[Test]
		public void CaptureAllは入れ子を保ったまま根だけを返す()
		{
			m_Root = new GameObject("Root");
			var first = Child(m_Root, "First");
			AddView(first);
			AddView(Child(first, "Nested"));
			AddView(Child(m_Root, "Second"));

			var roots = ViewTraversal.CaptureAll(m_Root, ViewTraversal.Options.ForLint);

			Assert.AreEqual(2, roots.Count, "根は 2 つ");
			var firstNode = roots.Find(x => x.Label == "First");
			Assert.AreEqual(1, firstNode.Children.Count);
			Assert.AreEqual("Nested", firstNode.Children[0].Label);
		}
	}
}
