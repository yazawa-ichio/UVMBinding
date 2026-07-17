using System;
using System.Collections.Generic;
using System.Threading;

namespace UVMBinding.Core
{
	interface IValueHolder
	{
		System.Type GetValueType();
		object GetObject();
		bool IsAssignable<T>();
		bool TryGetValue<T>(out T value)
		{
			if (this is ValueHolder<T> valueHolder)
			{
				value = valueHolder.Value;
				return true;
			}
			if (IsAssignable<T>())
			{
				value = (T)GetObject();
				return true;
			}
			value = default;
			return false;
		}
	}

	internal class ValueHolder<TValue> : IValueHolder
	{
		static ThreadLocal<Queue<ValueHolder<TValue>>> s_Pool = new ThreadLocal<Queue<ValueHolder<TValue>>>(() => new Queue<ValueHolder<TValue>>());

		public struct UseScope : IDisposable
		{
			public readonly ValueHolder<TValue> Holder;

			public UseScope(ValueHolder<TValue> holder)
			{
				Holder = holder;
			}

			public void Dispose()
			{
				var pool = s_Pool.Value;
				if (pool.Count < 5)
				{
					Holder.Value = default;
					pool.Enqueue(Holder);
				}
			}
		}

		public static UseScope Use(TValue value, out ValueHolder<TValue> holder)
		{
			var pool = s_Pool.Value;
			if (pool.Count > 0)
			{
				holder = pool.Dequeue();
			}
			else
			{
				holder = new ValueHolder<TValue>();
			}
			holder.Value = value;
			return new UseScope(holder);
		}

		public TValue Value;

		public Type GetValueType()
		{
			return typeof(TValue);
		}

		public object GetObject()
		{
			return Value;
		}

		public bool IsAssignable<T>()
		{
			return Assignable<TValue, T>.Is;
		}
	}
}