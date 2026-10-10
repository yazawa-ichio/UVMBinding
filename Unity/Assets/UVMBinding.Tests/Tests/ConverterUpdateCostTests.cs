using NUnit.Framework;
using UnityEngine;
using UVMBinding.Converters;
using UVMBinding.Core;

namespace UVMBinding.Tests
{
	public class ConverterUpdateCostTests
	{
		public class CostViewModel : ViewModel
		{
			[Bind] public bool Flag { get; set; }
			[Bind] public int Value { get; set; }
		}

		public class CountingBinder : Binder<bool>
		{
			public int UpdateCount;

			protected override void UpdateValue(bool value)
			{
				UpdateCount++;
			}
		}

		GameObject m_Root;

		[TearDown]
		public void TearDown()
		{
			if (m_Root != null) Object.DestroyImmediate(m_Root);
			m_Root = null;
		}

		CountingBinder Build(IConverter converter, string path)
		{
			m_Root = new GameObject("Root");
			var view = m_Root.AddComponent<View>();
			var child = new GameObject("Element");
			child.transform.SetParent(m_Root.transform, false);
			var binder = child.AddComponent<CountingBinder>();
			binder.ChangePath(path);
			var field = typeof(Binder<bool>).GetField("m_Converters",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			Assert.IsNotNull(field, "m_Converters が見つからない");
			field.SetValue(binder, converter != null
				? new[] { (Core.IConverter)converter }
				: System.Array.Empty<Core.IConverter>());
			view.Attach<CostViewModel>();
			return binder;
		}

		void Tick()
		{
			m_Root.GetComponent<View>().TryUpdate();
		}

		[Test]
		public void 静的なConverterは値が変わらないフレームで走査されない()
		{
			var binder = Build(new BoolInverse(), "Flag");
			Tick();
			var before = binder.UpdateCount;

			Tick();
			Tick();
			Tick();

			Assert.AreEqual(before, binder.UpdateCount, "止まっている間は回さない");
		}

		[Test]
		public void 毎フレームの処理を持つのはDelaySetだけ()
		{
			Assert.IsTrue(typeof(IUpdatableConverter).IsAssignableFrom(typeof(DelaySetBoolConverter)));
			Assert.IsFalse(typeof(IUpdatableConverter).IsAssignableFrom(typeof(BoolInverse)));
			Assert.IsFalse(typeof(IUpdatableConverter).IsAssignableFrom(typeof(IntComparison)));
			Assert.IsFalse(typeof(IUpdatableConverter).IsAssignableFrom(typeof(ObjectToString)));
		}

		[Test]
		public void 毎フレームの処理を持つConverterは走査され続ける()
		{
			var binder = Build(new DelaySetBoolConverter(), "Flag");
			Tick();
			var before = binder.UpdateCount;

			for (int i = 0; i < 5; i++) Tick();

			Assert.IsTrue(((IBinder)binder).NeedsUpdate, "型で毎フレーム要ると宣言している");
		}

		[Test]
		public void 静的なConverterは走査を要求しない()
		{
			var binder = Build(new BoolInverse(), "Flag");
			Tick();

			Assert.IsFalse(((IBinder)binder).NeedsUpdate);
		}

		[Test]
		public void Converterが無ければ走査を要求しない()
		{
			m_Root = new GameObject("Root");
			var view = m_Root.AddComponent<View>();
			var child = new GameObject("Element");
			child.transform.SetParent(m_Root.transform, false);
			var binder = child.AddComponent<CountingBinder>();
			binder.ChangePath("Flag");
			view.Attach<CostViewModel>();
			Tick();

			Assert.IsFalse(((IBinder)binder).NeedsUpdate);
		}

		[Test]
		public void 値が同じでも下流は更新される()
		{
			var binder = Build(new IntComparison(), "Value");
			var vm = (CostViewModel)m_Root.GetComponent<View>().ViewModel;
			vm.Value = 10;
			Tick();
			var before = binder.UpdateCount;

			vm.Value = 11;
			Tick();

			Assert.AreEqual(before + 1, binder.UpdateCount, "同じ値でも通知する");
		}
	}
}
