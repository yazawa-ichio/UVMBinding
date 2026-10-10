using NUnit.Framework;
using UVMBinding.Binders;
using UVMBinding.Events;

namespace UVMBinding.Tests
{
	public class SemanticRoleTests
	{
		class DerivedTextBinder : TextBinder { }

		class UndeclaredBinder : Binder<bool>
		{
			protected override void UpdateValue(bool value) { }
		}

		[SemanticRole(SemanticRole.Heading)]
		class DeclaredBinder : Binder<string>
		{
			protected override void UpdateValue(string value) { }
		}

		[Test]
		public void 表示系の役割が引ける()
		{
			Assert.AreEqual(SemanticRole.Text, SemanticRoleLookup.Get(typeof(TextBinder)));
			Assert.AreEqual(SemanticRole.Image, SemanticRoleLookup.Get(typeof(ImageSpriteBinder)));
		}

		[Test]
		public void 入力系の役割が引ける()
		{
			Assert.AreEqual(SemanticRole.TextBox, SemanticRoleLookup.Get(typeof(InputFieldBinder)));
			Assert.AreEqual(SemanticRole.Switch, SemanticRoleLookup.Get(typeof(ToggleBinder)));
			Assert.AreEqual(SemanticRole.Slider, SemanticRoleLookup.Get(typeof(SliderBinder)));
			Assert.AreEqual(SemanticRole.ComboBox, SemanticRoleLookup.Get(typeof(DropdownSelectBinder)));
		}

		[Test]
		public void スクロールバーはスライダーと分けている()
		{
			Assert.AreEqual(SemanticRole.Scrollbar, SemanticRoleLookup.Get(typeof(ScrollbarBinder)));
		}

		[Test]
		public void 構造と操作の役割が引ける()
		{
			Assert.AreEqual(SemanticRole.List, SemanticRoleLookup.Get(typeof(CollectionBinder)));
			Assert.AreEqual(SemanticRole.Button, SemanticRoleLookup.Get(typeof(EventButton)));
		}

		[Test]
		public void 演出系はNoneになっている()
		{
			Assert.AreEqual(SemanticRole.None, SemanticRoleLookup.Get(typeof(GraphicColorBinder)));
			Assert.AreEqual(SemanticRole.None, SemanticRoleLookup.Get(typeof(Position3DBinder)));
			Assert.AreEqual(SemanticRole.None, SemanticRoleLookup.Get(typeof(RotationBinder)));
			Assert.AreEqual(SemanticRole.None, SemanticRoleLookup.Get(typeof(ScaleBinder)));
			Assert.AreEqual(SemanticRole.None, SemanticRoleLookup.Get(typeof(CanvasGroupAlphaBinder)));
			Assert.AreEqual(SemanticRole.None, SemanticRoleLookup.Get(typeof(AnimatorBoolBinder)));
		}

		[Test]
		public void 表示や操作の可否はNoneになっている()
		{
			Assert.AreEqual(SemanticRole.None, SemanticRoleLookup.Get(typeof(ActiveBinder)));
			Assert.AreEqual(SemanticRole.None, SemanticRoleLookup.Get(typeof(InteractableBinder)));
		}

		[Test]
		public void 宣言が無ければNone()
		{
			Assert.AreEqual(SemanticRole.None, SemanticRoleLookup.Get(typeof(UndeclaredBinder)));
			Assert.AreEqual(SemanticRole.None, SemanticRoleLookup.Get((System.Type)null));
			Assert.AreEqual(SemanticRole.None, SemanticRoleLookup.Get((object)null));
		}

		[Test]
		public void 利用側の宣言も引ける()
		{
			Assert.AreEqual(SemanticRole.Heading, SemanticRoleLookup.Get(typeof(DeclaredBinder)));
		}

		[Test]
		public void 派生にも継承される()
		{
			Assert.AreEqual(SemanticRole.Text, SemanticRoleLookup.Get(typeof(DerivedTextBinder)));
		}

		[Test]
		public void 二度目もの同じ結果を返す()
		{
			var first = SemanticRoleLookup.Get(typeof(TextBinder));
			var second = SemanticRoleLookup.Get(typeof(TextBinder));
			Assert.AreEqual(first, second);
			Assert.AreEqual(SemanticRoleLookup.Get(typeof(UndeclaredBinder)),
				SemanticRoleLookup.Get(typeof(UndeclaredBinder)));
		}

		[Test]
		public void インスタンスからも引ける()
		{
			var go = new UnityEngine.GameObject("Binder");
			try
			{
				var binder = go.AddComponent<SliderBinder>();
				Assert.AreEqual(SemanticRole.Slider, SemanticRoleLookup.Get(binder));
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(go);
			}
		}
	}
}
