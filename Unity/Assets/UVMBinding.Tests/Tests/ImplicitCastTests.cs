using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UVMBinding.Converters;
using UVMBinding.Core;

namespace UVMBinding.Tests
{
	public class ImplicitCastTests
	{
		public readonly struct OrderId
		{
			public readonly int Value;

			public OrderId(int value)
			{
				Value = value;
			}
		}

		public readonly struct TicketId
		{
			public readonly int Value;

			public TicketId(int value)
			{
				Value = value;
			}
		}

		public class NumberViewModel : ViewModel
		{
			[Bind] public int Count { get; set; }
			[Bind] public byte Small { get; set; }
			[Bind] public float Ratio { get; set; }
			[Bind] public double Precise { get; set; }
			[Bind] public string Name { get; set; }
			[Bind] public OrderId Id { get; set; }
			[Bind] public TicketId Ticket { get; set; }
		}

		public class RecordBinderBase<T> : Binder<T>
		{
			public T Value;
			public int UpdateCount;

			public void Write(T value) => Set(value);

			protected override void UpdateValue(T value)
			{
				Value = value;
				UpdateCount++;
			}
		}

		public class IntRecordBinder : RecordBinderBase<int> { }
		public class LongRecordBinder : RecordBinderBase<long> { }
		public class FloatRecordBinder : RecordBinderBase<float> { }
		public class DoubleRecordBinder : RecordBinderBase<double> { }
		public class BoolRecordBinder : RecordBinderBase<bool> { }

		readonly List<GameObject> m_Objects = new List<GameObject>();

		[TearDown]
		public void TearDown()
		{
			foreach (var obj in m_Objects)
			{
				if (obj != null) Object.DestroyImmediate(obj);
			}
			m_Objects.Clear();
		}

		TBinder CreateBinder<TBinder, TValue>(out View view, string path, IConverter converter = null)
			where TBinder : Binder<TValue>
		{
			var root = new GameObject("Root");
			m_Objects.Add(root);
			view = root.AddComponent<View>();
			var child = new GameObject("Element");
			child.transform.SetParent(root.transform, false);
			var binder = child.AddComponent<TBinder>();
			binder.ChangePath(path);
			if (converter != null)
			{
				SetConverters(binder, converter);
			}
			return binder;
		}

		static void SetConverters(object binder, params IConverter[] converters)
		{
			for (var type = binder.GetType(); type != null; type = type.BaseType)
			{
				var field = type.GetField("m_Converters",
					System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
				if (field != null)
				{
					field.SetValue(binder, converters);
					return;
				}
			}
			Assert.Fail("m_Converters not found");
		}

		static void AllowCast(object binder)
		{
			for (var type = binder.GetType(); type != null; type = type.BaseType)
			{
				var field = type.GetField("m_AllowCast",
					System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
				if (field != null)
				{
					field.SetValue(binder, true);
					return;
				}
			}
			Assert.Fail("m_AllowCast not found");
		}

		[Test]
		public void 値が保たれる整数の拡大は自動で挟まる()
		{
			var binder = CreateBinder<LongRecordBinder, long>(out var view, "Count");
			var vm = view.Attach<NumberViewModel>(x => x.Count = 7);
			view.TryUpdate();

			Assert.AreEqual(7L, binder.Value, "int の path が Binder<long> に繋がる");

			vm.Count = 42;
			view.TryUpdate();
			Assert.AreEqual(42L, binder.Value, "変更も流れる");
		}

		[Test]
		public void byteからintも自動で挟まる()
		{
			var binder = CreateBinder<IntRecordBinder, int>(out var view, "Small");
			view.Attach<NumberViewModel>(x => x.Small = 200);
			view.TryUpdate();

			Assert.AreEqual(200, binder.Value);
		}

		[Test]
		public void floatからdoubleも自動で挟まる()
		{
			var binder = CreateBinder<DoubleRecordBinder, double>(out var view, "Ratio");
			view.Attach<NumberViewModel>(x => x.Ratio = 0.5f);
			view.TryUpdate();

			Assert.AreEqual(0.5d, binder.Value, 1e-9);
		}

		[Test]
		public void 精度が落ちうる変換は自動で挟まらない()
		{
			Assert.IsFalse(CastRegistry.IsImplicit(typeof(int), typeof(float)));

			var binder = CreateBinder<FloatRecordBinder, float>(out var view, "Count");
			view.Attach<NumberViewModel>(x => x.Count = 7);
			view.TryUpdate();

			Assert.AreEqual(0, binder.UpdateCount, "int -> float は自動では通らない");
		}

		[Test]
		public void 値域を超える変換は自動で挟まらない()
		{
			Assert.IsFalse(CastRegistry.IsImplicit(typeof(double), typeof(int)));

			var binder = CreateBinder<IntRecordBinder, int>(out var view, "Precise");
			view.Attach<NumberViewModel>(x => x.Precise = 3.9d);
			view.TryUpdate();

			Assert.AreEqual(0, binder.UpdateCount, "double -> int は自動では通らない");
		}

		[Test]
		public void 許可すれば値が変わりうる変換も段を作らずに通る()
		{
			var binder = CreateBinder<FloatRecordBinder, float>(out var view, "Count");
			AllowCast(binder);
			view.Attach<NumberViewModel>(x => x.Count = 7);
			view.TryUpdate();

			Assert.AreEqual(7f, binder.Value, 1e-6f);
			Assert.AreEqual(BindKind.Cast, ((IBinderInfo)binder).Kind, "値が変わりうる変換だと読める");

			var field = typeof(Binder<float>).GetField("m_Property",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			var property = field.GetValue(binder) as IBindingProperty;
			Assert.AreEqual(typeof(int), property.GetBindType(), "段を挟まずソースへ直結する");
		}

		[Test]
		public void 許可していなければ値が変わりうる変換は通らない()
		{
			var binder = CreateBinder<FloatRecordBinder, float>(out var view, "Count");
			view.Attach<NumberViewModel>(x => x.Count = 7);
			view.TryUpdate();

			Assert.AreEqual(0, binder.UpdateCount);
			Assert.AreEqual(BindKind.None, ((IBinderInfo)binder).Kind);
		}

		[Test]
		public void 許可は書き戻しにも効く()
		{
			var binder = CreateBinder<LongRecordBinder, long>(out var view, "Count");
			AllowCast(binder);
			var vm = view.Attach<NumberViewModel>(x => x.Count = 7);
			view.TryUpdate();

			binder.Write(99L);

			Assert.AreEqual(99, vm.Count, "long -> int も許可すれば通る");
		}

		[Test]
		public void 数値以外の不一致は結線せず両端の型をログに出す()
		{
			LogAssert.Expect(LogType.Log,
				new System.Text.RegularExpressions.Regex("Type mismatch.*String.*Int32"));
			var level = Logger.Log.Level;
			Logger.Log.Level = Logger.LogLevel.Debug;
			try
			{
				var binder = CreateBinder<IntRecordBinder, int>(out var view, "Name");
				view.Attach<NumberViewModel>(x => x.Name = "abc");
				view.TryUpdate();
				Assert.AreEqual(0, binder.UpdateCount);
			}
			finally
			{
				Logger.Log.Level = level;
			}
		}

		[Test]
		public void Converterの入力型が違っても自動で挟まる()
		{
			var binder = CreateBinder<BoolRecordBinder, bool>(out var view, "Count", new LongComparison());
			var vm = view.Attach<NumberViewModel>(x => x.Count = 0);
			view.TryUpdate();

			Assert.IsTrue(binder.Value, "int -> long が挟まって Converter に届く");

			vm.Count = 8;
			view.TryUpdate();
			Assert.IsFalse(binder.Value, "挟んだ変換も毎回進む");
		}

		[Test]
		public void 利用側の登録は既定では自動にならない()
		{
			CastRegistry.Register<TicketId, int>(x => x.Value);
			Assert.IsTrue(CastRegistry.IsRegistered(typeof(TicketId), typeof(int)));
			Assert.IsFalse(CastRegistry.IsImplicit(typeof(TicketId), typeof(int)), "既定は非自動");

			var binder = CreateBinder<IntRecordBinder, int>(out var view, "Ticket");
			view.Attach<NumberViewModel>(x => x.Ticket = new TicketId(3));
			view.TryUpdate();

			Assert.AreEqual(0, binder.UpdateCount, "登録しただけでは通らない");
		}

		[Test]
		public void RegisterImplicitで登録すると自動になる()
		{
			CastRegistry.RegisterImplicit<OrderId, int>(x => x.Value);
			Assert.IsTrue(CastRegistry.IsImplicit(typeof(OrderId), typeof(int)));

			var binder = CreateBinder<IntRecordBinder, int>(out var view, "Id");
			view.Attach<NumberViewModel>(x => x.Id = new OrderId(3));
			view.TryUpdate();

			Assert.AreEqual(3, binder.Value, "段を作らずに繋がる");
		}

		[Test]
		public void 値が保たれない向きは書き戻らない()
		{
			var binder = CreateBinder<LongRecordBinder, long>(out var view, "Count");
			var vm = view.Attach<NumberViewModel>(x => x.Count = 7);
			view.TryUpdate();

			binder.Write(99L);

			Assert.AreEqual(7, vm.Count);
		}

		[Test]
		public void 結線がどう成立したかを外から読める()
		{
			var exact = CreateBinder<IntRecordBinder, int>(out var v1, "Count");
			v1.Attach<NumberViewModel>(x => x.Count = 7);
			v1.TryUpdate();
			Assert.AreEqual(BindKind.Exact, ((IBinderInfo)exact).Kind);

			var converted = CreateBinder<LongRecordBinder, long>(out var v2, "Count");
			v2.Attach<NumberViewModel>(x => x.Count = 7);
			v2.TryUpdate();
			Assert.AreEqual(BindKind.Converted, ((IBinderInfo)converted).Kind, "int -> long は層 1 の変換");

			var none = CreateBinder<FloatRecordBinder, float>(out var v3, "Count");
			v3.Attach<NumberViewModel>(x => x.Count = 7);
			v3.TryUpdate();
			Assert.AreEqual(BindKind.None, ((IBinderInfo)none).Kind, "int -> float は繋がらない");
		}

		[Test]
		public void 段を作らずにソースへ直結する()
		{
			var binder = CreateBinder<LongRecordBinder, long>(out var view, "Count");
			var vm = view.Attach<NumberViewModel>(x => x.Count = 7);
			view.TryUpdate();

			var field = typeof(Binder<long>).GetField("m_Property",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			var bound = (IBindingProperty)field.GetValue(binder);

			Assert.AreEqual(typeof(int), bound.GetBindType(), "ソースのプロパティに直結する");
			Assert.AreEqual(7L, binder.Value);
		}
	}
}
