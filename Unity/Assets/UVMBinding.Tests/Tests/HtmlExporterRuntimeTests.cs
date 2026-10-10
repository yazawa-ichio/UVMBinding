using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UVMBinding.Semantics;
using UVMBinding.Semantics.Export;

#if UNITY_EDITOR || UVMBINDING_EXPORT
namespace UVMBinding.Tests
{
	public class HtmlExporterRuntimeTests
	{
		public class ExportViewModel : ViewModel
		{
			[Bind] public string Caption { get; set; } = "見出し";
			[Bind] public bool CanPress { get; set; } = true;
			[Bind] public bool Visible { get; set; } = true;
			[Bind] public Sprite Picture { get; set; }
			[Bind] public Collection<RowViewModel> Rows { get; private set; }

			[Event] public System.Action OnPress { get; set; }
		}

		public class RowViewModel : ViewModel
		{
			[Bind] public string Caption { get; set; }
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

		static GameObject Child(GameObject parent, string name)
		{
			var go = new GameObject(name);
			go.transform.SetParent(parent.transform, false);
			return go;
		}

		View Build()
		{
			m_Root = new GameObject("Screen");
			var view = m_Root.AddComponent<View>();

			var label = Child(m_Root, "Caption");
			label.AddComponent<Text>();
			label.AddComponent<Binders.TextBinder>().ChangePath("Caption");

			var button = Child(m_Root, "Go");
			button.AddComponent<Image>();
			button.AddComponent<Button>();
			button.AddComponent<Binders.InteractableBinder>().ChangePath("CanPress");
			var press = button.AddComponent<Events.EventButton>();
			SetPrivate(press, "m_EventName", "OnPress");

			var panel = Child(m_Root, "Panel");
			panel.AddComponent<Text>();
			panel.AddComponent<Binders.TextBinder>().ChangePath("Caption");
			panel.AddComponent<Binders.ActiveBinder>().ChangePath("Visible");
			return view;
		}

		static string Export(View view)
		{
			return HtmlExporter.Export(ViewTraversal.Capture(view, ViewTraversal.Options.ForExport));
		}

		[Test]
		public void 表示している文字列が中身になる()
		{
			var view = Build();
			view.Attach<ExportViewModel>(vm => vm.Caption = "実値");
			view.TryUpdate();

			StringAssert.Contains("<output id=\"Screen/Caption\" name=\"Caption\">実値</output>", Export(view));
		}

		[Test]
		public void 押せない結線はdisabledになる()
		{
			var view = Build();
			var vm = view.Attach<ExportViewModel>();
			vm.CanPress = false;
			view.TryUpdate();

			var html = Export(view);
			StringAssert.Contains("name=\"OnPress\" type=\"button\" data-consequence=\"unknown\" disabled>", html);

			vm.CanPress = true;
			view.TryUpdate();
			StringAssert.DoesNotContain(" disabled", Export(view));
		}

		[Test]
		public void 出ていない結線はhiddenになる()
		{
			var view = Build();
			var vm = view.Attach<ExportViewModel>();
			vm.Visible = false;
			view.TryUpdate();

			var html = Export(view);
			StringAssert.Contains("id=\"Screen/Panel\"", html);
			StringAssert.Contains(" hidden>", html);
		}

		[Test]
		public void 画像のaltはsprite名になる()
		{
			var view = Build();
			var image = Child(m_Root, "Picture");
			image.AddComponent<Image>();
			image.AddComponent<Binders.ImageSpriteBinder>().ChangePath("Picture");
			var sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
			sprite.name = "scene_shop";
			view.Attach<ExportViewModel>(vm => vm.Picture = sprite);
			view.TryUpdate();

			StringAssert.Contains("alt=\"scene_shop\"", Export(view));
			Object.DestroyImmediate(sprite);
		}

		[Test]
		public void 実行中の行はulの中に件数付きで出る()
		{
			var view = Build();
			var list = Child(m_Root, "Rows");
			var template = Child(m_Root, "RowTemplate");
			template.AddComponent<View>();
			var rowLabel = Child(template, "Caption");
			rowLabel.AddComponent<Text>();
			rowLabel.AddComponent<Binders.TextBinder>().ChangePath("Caption");
			template.SetActive(false);
			var binder = list.AddComponent<Binders.CollectionBinder>();
			binder.ChangePath("Rows");
			SetPrivate(binder, "m_Preafb", template);

			var vm = view.Attach<ExportViewModel>();
			vm.Rows.Add(new RowViewModel { Caption = "1 行目" });
			vm.Rows.Add(new RowViewModel { Caption = "2 行目" });
			view.TryUpdate();

			var html = Export(view);
			StringAssert.Contains("aria-posinset=\"1\" aria-setsize=\"2\"", html);
			StringAssert.Contains("aria-posinset=\"2\" aria-setsize=\"2\"", html);
			StringAssert.Contains("1 行目", html);
			StringAssert.Contains("<!-- row template: shows the shape of a row, not a row -->", html);
			Assert.Less(html.IndexOf("<li ", System.StringComparison.Ordinal),
				html.IndexOf("</ul>", System.StringComparison.Ordinal), "行が ul の中にある");
		}

		[Test]
		public void 減らした行は件数に入らない()
		{
			var view = Build();
			var list = Child(m_Root, "Rows");
			var template = Child(m_Root, "RowTemplate");
			template.AddComponent<View>();
			var rowLabel = Child(template, "Caption");
			rowLabel.AddComponent<Text>();
			rowLabel.AddComponent<Binders.TextBinder>().ChangePath("Caption");
			template.SetActive(false);
			var binder = list.AddComponent<Binders.CollectionBinder>();
			binder.ChangePath("Rows");
			SetPrivate(binder, "m_Preafb", template);

			var vm = view.Attach<ExportViewModel>();
			vm.Rows.Add(new RowViewModel { Caption = "1 行目" });
			vm.Rows.Add(new RowViewModel { Caption = "2 行目" });
			vm.Rows.Add(new RowViewModel { Caption = "3 行目" });
			view.TryUpdate();
			vm.Rows.Clear();
			vm.Rows.Add(new RowViewModel { Caption = "残った行" });
			view.TryUpdate();

			var html = Export(view);
			StringAssert.Contains("aria-posinset=\"1\" aria-setsize=\"1\"", html);
			StringAssert.DoesNotContain("aria-setsize=\"3\"", html);
			StringAssert.Contains("<!-- pooled row: kept for reuse, not a row -->", html);
		}

		[Test]
		public void 入っている状態はcheckedで出る()
		{
			var view = Build();
			var toggle = Child(m_Root, "Toggle");
			toggle.AddComponent<Image>();
			toggle.AddComponent<Toggle>();
			toggle.AddComponent<Binders.ToggleBinder>().ChangePath("CanPress");

			var vm = view.Attach<ExportViewModel>();
			vm.CanPress = true;
			view.TryUpdate();
			StringAssert.Contains("type=\"checkbox\" role=\"switch\" checked>", Export(view));

			vm.CanPress = false;
			view.TryUpdate();
			StringAssert.DoesNotContain(" checked", Export(view));
		}

		[Test]
		public void 行の並べ替えでidが追随する()
		{
			var view = Build();
			var list = Child(m_Root, "Rows");
			var template = Child(m_Root, "RowTemplate");
			template.AddComponent<View>();
			var rowLabel = Child(template, "Caption");
			rowLabel.AddComponent<Text>();
			rowLabel.AddComponent<Binders.TextBinder>().ChangePath("Caption");
			template.SetActive(false);
			var binder = list.AddComponent<Binders.CollectionBinder>();
			binder.ChangePath("Rows");
			SetPrivate(binder, "m_Preafb", template);

			var vm = view.Attach<ExportViewModel>();
			var first = new RowViewModel { Caption = "1 行目" };
			var second = new RowViewModel { Caption = "2 行目" };
			vm.Rows.Add(first);
			vm.Rows.Add(second);
			view.TryUpdate();
			var before = Export(view);

			vm.Rows.Clear();
			vm.Rows.Add(second);
			vm.Rows.Add(first);
			view.TryUpdate();
			var after = Export(view);

			Assert.AreNotEqual(before, after, "並べ替えが出力に出ていない");
			var head = after.Substring(after.IndexOf("aria-posinset=\"1\"", System.StringComparison.Ordinal));
			StringAssert.Contains("2 行目", head.Substring(0, head.IndexOf("</li>", System.StringComparison.Ordinal)));
		}

		static void SetPrivate(object target, string field, object value)
		{
			const System.Reflection.BindingFlags Flags = System.Reflection.BindingFlags.Instance
				| System.Reflection.BindingFlags.NonPublic;
			for (var type = target.GetType(); type != null; type = type.BaseType)
			{
				var info = type.GetField(field, Flags);
				if (info != null)
				{
					info.SetValue(target, value);
					return;
				}
			}
			Assert.Fail($"field not found: {field}");
		}
	}
}

#endif
