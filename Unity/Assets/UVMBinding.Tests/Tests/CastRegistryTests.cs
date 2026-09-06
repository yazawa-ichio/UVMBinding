using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UVMBinding.Converters;
using UVMBinding.Core;

namespace UVMBinding.Tests
{
	public class CastRegistryTests
	{
		public readonly struct ItemId
		{
			public readonly int Value;

			public ItemId(int value)
			{
				Value = value;
			}
		}

		public class IdViewModel : ViewModel
		{
			[Bind] public ItemId Id { get; set; }
		}

		GameObject m_Root;

		[SetUp]
		public void SetUp()
		{
			CastRegistry.Register<ItemId, string>(x => $"#{x.Value:000}");
		}

		[TearDown]
		public void TearDown()
		{
			if (m_Root != null) Object.DestroyImmediate(m_Root);
			m_Root = null;
		}

		[Test]
		public void 登録した組が引ける()
		{
			Assert.IsTrue(CastRegistry.IsRegistered(typeof(ItemId), typeof(string)));
		}

		[Test]
		public void 既定の数値変換は残っている()
		{
			Assert.IsTrue(CastRegistry.IsRegistered(typeof(int), typeof(float)));
		}

		[Test]
		public void 登録していない組は引けない()
		{
			Assert.IsFalse(CastRegistry.IsRegistered(typeof(ItemId), typeof(bool)));
		}

		[Test]
		public void nullを登録できない()
		{
			Assert.Throws<System.ArgumentNullException>(
				() => CastRegistry.Register<ItemId, int>(null));
		}

		[Test]
		public void 登録した変換でバインドできる()
		{
			m_Root = new GameObject("Root");
			var view = m_Root.AddComponent<View>();
			var label = new GameObject("Label");
			label.transform.SetParent(m_Root.transform, false);
			label.AddComponent<Text>();
			var binder = label.AddComponent<Binders.TextBinder>();
			binder.ChangePath("Id");
			var field = typeof(Binder<string>).GetField("m_AllowCast",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			field.SetValue(binder, true);

			var vm = view.Attach<IdViewModel>(x => x.Id = new ItemId(7));
			view.TryUpdate();

			Assert.AreEqual("#007", label.GetComponent<Text>().text, "包んだ値が文字列になる");

			vm.Id = new ItemId(42);
			view.TryUpdate();
			Assert.AreEqual("#042", label.GetComponent<Text>().text);
		}

		[Test]
		public void 上書きすると警告が出る()
		{
			UnityEngine.TestTools.LogAssert.Expect(LogType.Warning,
				new System.Text.RegularExpressions.Regex("CastRegistry Overwrite"));
			var level = Logger.Log.Level;
			Logger.Log.Level = Logger.LogLevel.All;
			try
			{
				CastRegistry.Register<ItemId, string>(x => x.Value.ToString());
			}
			finally
			{
				Logger.Log.Level = level;
			}
		}
	}
}
