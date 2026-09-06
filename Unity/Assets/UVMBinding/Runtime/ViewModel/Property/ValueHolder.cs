using System;
using System.Collections.Generic;

namespace UVMBinding.Core
{
	interface IValueHolder
	{
		System.Type GetValueType();
		object GetObject();
		bool IsAssignable<T>();

		bool CanReceive<T>(bool allowCast);

		BindKind GetBindKind<T>(bool allowCast);

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

		bool TryGetValueAs<T>(out T value)
		{
			if (this is ValueHolder<T> valueHolder)
			{
				value = valueHolder.Value;
				return true;
			}
			if (GetObject() is T casted)
			{
				value = casted;
				return true;
			}
			value = default;
			return false;
		}

		bool TryGetValueCast<T>(out T value);
		bool TrySetValueCast<T>(T value, out bool changed);

		bool TrySetValue<T>(T value, out bool changed);

		void SetObjectValue(object value);

		bool TrySetValueAs(object value);
	}

	internal class ValueHolder<TValue> : IValueHolder
	{
		static EqualityComparer<TValue> s_EqualityComparer = EqualityComparer<TValue>.Default;
		static readonly bool s_IsErased = typeof(TValue) == typeof(object);

		public TValue Value;

		public Type GetValueType()
		{
			return typeof(TValue);
		}

		public object GetObject()
		{
			return Value;
		}

		public bool CanReceive<T>(bool allowCast)
		{
			return GetBindKind<T>(allowCast) != BindKind.None;
		}

		public BindKind GetBindKind<T>(bool allowCast)
		{
			if (this is ValueHolder<T>) return BindKind.Exact;
			if (IsAssignable<T>()) return BindKind.Assignable;
			if (NumericCast<TValue, T>.Is) return BindKind.Converted;
			if (allowCast && ExplicitCast<TValue, T>.Is) return BindKind.Cast;
			return BindKind.None;
		}

		public bool TryGetValue<T>(out T value)
		{
			if (this is ValueHolder<T> typed)
			{
				value = typed.Value;
				return true;
			}
			if (NumericCast<TValue, T>.Is)
			{
				value = NumericCast<TValue, T>.Convert(Value);
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

		public bool TryGetValueCast<T>(out T value)
		{
			if (TryGetValue(out value))
			{
				return true;
			}
			if (ExplicitCast<TValue, T>.Is)
			{
				value = ExplicitCast<TValue, T>.Convert(Value);
				return true;
			}
			return false;
		}

		public bool IsAssignable<T>()
		{
			if (s_IsErased)
			{
				var value = Value;
				if (value == null)
				{
					return AssignableNull<T>.IsNullable;
				}
				return Assignable.Is(value.GetType(), typeof(T));
			}
			return Assignable<TValue, T>.Is;
		}

		public void SetObjectValue(object value)
		{
			Value = (TValue)value;
		}

		public bool TrySetValueAs(object value)
		{
			if (value is TValue typed)
			{
				Value = typed;
				return true;
			}
			if (value == null && AssignableNull<TValue>.IsNullable)
			{
				Value = default;
				return true;
			}
			return false;
		}

		public bool TrySetValue<T>(T value, out bool changed)
		{
			if (this is ValueHolder<T> holder)
			{
				changed = !EqualityComparer<T>.Default.Equals(holder.Value, value);
				holder.Value = value;
				return true;
			}
			if (Assignable<T, TValue>.Is)
			{
				var converted = (TValue)(object)value;
				changed = !s_EqualityComparer.Equals(Value, converted);
				Value = converted;
				return true;
			}
			if (NumericCast<T, TValue>.Is)
			{
				var converted = NumericCast<T, TValue>.Convert(value);
				changed = !s_EqualityComparer.Equals(Value, converted);
				Value = converted;
				return true;
			}
			changed = false;
			return false;
		}

		public bool TrySetValueCast<T>(T value, out bool changed)
		{
			if (TrySetValue(value, out changed))
			{
				return true;
			}
			if (ExplicitCast<T, TValue>.Is)
			{
				var converted = ExplicitCast<T, TValue>.Convert(value);
				changed = !s_EqualityComparer.Equals(Value, converted);
				Value = converted;
				return true;
			}
			return false;
		}
	}
}