using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UVMBinding.Binders;
using UVMBinding.Core;

namespace UVMBinding.Tests
{
	public class DisplayTextTests
	{
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

		T Create<T>() where T : Component
		{
			m_Root = new GameObject("Element");
			return m_Root.AddComponent<T>();
		}

		static string GetDisplayText(Component binder)
		{
			return ((IBinderInfo)binder).GetDisplayText();
		}

		[Test]
		public void TextBinderは表示中の文字列を返す()
		{
			var text = Create<Text>();
			text.text = "こんにちは";
			var binder = m_Root.AddComponent<TextBinder>();

			Assert.AreEqual("こんにちは", GetDisplayText(binder));
		}

		[Test]
		public void InputFieldBinderは入力中の文字列を返す()
		{
			var input = Create<InputField>();
			input.text = "入力値";
			var binder = m_Root.AddComponent<InputFieldBinder>();

			Assert.AreEqual("入力値", GetDisplayText(binder));
		}

		[Test]
		public void ImageSpriteBinderはスプライト名を返す()
		{
			var image = Create<Image>();
			var texture = new Texture2D(4, 4);
			var sprite = Sprite.Create(texture, new Rect(0, 0, 4, 4), Vector2.zero);
			sprite.name = "Icon";
			image.sprite = sprite;
			var binder = m_Root.AddComponent<ImageSpriteBinder>();
			try
			{
				Assert.AreEqual("Icon", GetDisplayText(binder));
			}
			finally
			{
				Object.DestroyImmediate(sprite);
				Object.DestroyImmediate(texture);
			}
		}

		[Test]
		public void スプライトが無ければnull()
		{
			Create<Image>();
			var binder = m_Root.AddComponent<ImageSpriteBinder>();

			Assert.IsNull(GetDisplayText(binder));
		}

		[Test]
		public void 対象コンポーネントが無くても落ちない()
		{
			m_Root = new GameObject("Element");
			var binder = m_Root.AddComponent<TextBinder>();

			Assert.IsNull(GetDisplayText(binder));
		}

		[Test]
		public void 表示テキストを持たないBinderはnullを返す()
		{
			Create<Image>();
			var color = m_Root.AddComponent<GraphicColorBinder>();
			var active = m_Root.AddComponent<ActiveBinder>();

			Assert.IsNull(GetDisplayText(color));
			Assert.IsNull(GetDisplayText(active));
		}

		[Test]
		public void 取得しても対象を初期化しない()
		{
			var dropdown = Create<Dropdown>();
			dropdown.options.Add(new Dropdown.OptionData("A"));
			dropdown.options.Add(new Dropdown.OptionData("B"));
			var binder = m_Root.AddComponent<DropdownStringDataBinder>();

			Assert.IsNull(GetDisplayText(binder));
			Assert.AreEqual(2, dropdown.options.Count, "選択肢が消えていない");
			Assert.AreEqual(0, dropdown.onValueChanged.GetPersistentEventCount());
		}

		[Test]
		public void 入力系でも対象を初期化しない()
		{
			var input = Create<InputField>();
			input.text = "before";
			var binder = m_Root.AddComponent<InputFieldBinder>();

			Assert.AreEqual("before", GetDisplayText(binder));
			input.text = "after";
			Assert.AreEqual("after", GetDisplayText(binder), "値の変更が Binder 経由で戻っていない");
		}
	}
}
