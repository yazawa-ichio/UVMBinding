using NUnit.Framework;
using UnityEngine;
using UVMBinding.Core;

namespace UVMBinding.Tests
{
	public class IdleUpdateTests
	{
		public class BenchViewModel : ViewModel
		{
			[Bind] public string Text { get; set; } = "A";
		}

		public class CountingBinder : Binder<string>
		{
			public static int TotalUpdates;

			protected override void UpdateValue(string value)
			{
				TotalUpdates++;
			}
		}

		GameObject m_Root;

		[TearDown]
		public void TearDown()
		{
			if (m_Root != null) Object.DestroyImmediate(m_Root);
			m_Root = null;
		}

		[Test]
		public void 変更の無いフレームの走査量を測る()
		{
			const int BinderCount = 200;
			const int Frames = 100;

			m_Root = new GameObject("Root");
			var view = m_Root.AddComponent<View>();
			for (int i = 0; i < BinderCount; i++)
			{
				var child = new GameObject("Element" + i);
				child.transform.SetParent(m_Root.transform, false);
				child.AddComponent<CountingBinder>().ChangePath("Text");
			}
			var vm = view.Attach<BenchViewModel>();
			view.TryUpdate();

			CountingBinder.TotalUpdates = 0;
			for (int f = 0; f < Frames; f++)
			{
				view.TryUpdate();
			}
			var idle = CountingBinder.TotalUpdates;

			CountingBinder.TotalUpdates = 0;
			for (int f = 0; f < Frames; f++)
			{
				vm.Text = "V" + f;
				view.TryUpdate();
			}
			var changing = CountingBinder.TotalUpdates;

			Debug.Log($"[VersionGate] binders={BinderCount} frames={Frames} idle={idle} changing={changing}");
			Assert.AreEqual(0, idle, "変更が無ければ 1 つも走査しない");
			Assert.AreEqual(BinderCount * Frames, changing, "変更があれば従来どおり全件");
		}
	}
}
