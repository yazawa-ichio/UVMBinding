using System;
using System.Collections.Generic;
using UVMBinding.Core;
using UVMBinding.Logger;

namespace UVMBinding
{
	[UnityEngine.Scripting.Preserve]
	internal abstract class BindingProperty : IBindingProperty
	{
		public string Path { get; private set; }
		public int Hash { get; protected set; }
		protected Action<string, IValueHolder> m_OnChangedValue;

		public event Action<string, IValueHolder> OnChangedValue
		{
			add => m_OnChangedValue += value;
			remove => m_OnChangedValue -= value;
		}

		public BindingProperty(string path)
		{
			Path = path;
			Hash = GetHashCode();
		}

		public abstract Type GetBindType();
		public abstract bool IsAssignable<T>();
		public abstract object GetObject();
		public abstract void SetObject(object val);
		public abstract void SetDirty();

	}

	internal class ObjectBindingProperty : BindingProperty<object>
	{
		public ObjectBindingProperty(string path) : base(path) { }

		public override bool IsAssignable<T>()
		{
			if (m_Value == null)
			{
				return AssignableNull<T>.IsNullable;
			}
			return Assignable.Is(m_Value.GetType(), typeof(T));
		}
	}

	internal class BindingProperty<TValue> : BindingProperty, IBindingProperty<TValue>
	{
		static EqualityComparer<TValue> s_EqualityComparer = EqualityComparer<TValue>.Default;

		protected TValue m_Value;
		public TValue Value
		{
			get
			{
				return m_Value;
			}
			set
			{
				if (s_EqualityComparer.Equals(m_Value, value))
				{
					return;
				}
				Log.Trace("BindingProperty<{0}> Update Path:{1} Value {2} to {3}", typeof(TValue), Path, m_Value, value);
				m_Value = value;
				Hash++;
				OnChanged?.Invoke(value);
				//購読者が居ない場合は ValueHolder のプール操作を行わない
				if (m_OnChangedValue != null)
				{
					using (ValueHolder<TValue>.Use(value, out var holder))
					{
						m_OnChangedValue.Invoke(Path, holder);
					}
				}
			}
		}

		public BindingProperty(string path) : base(path) { m_Value = default; }

		public event Action<TValue> OnChanged;

		public override void SetDirty()
		{
			Log.Trace("BindingProperty<{0}> SetDitry Path:{1}", typeof(TValue), Path);
			Hash++;
			OnChanged?.Invoke(m_Value);
			//購読者が居ない場合は ValueHolder のプール操作を行わない
			if (m_OnChangedValue != null)
			{
				using (ValueHolder<TValue>.Use(m_Value, out var holder))
				{
					m_OnChangedValue.Invoke(Path, holder);
				}
			}
		}

		public override Type GetBindType()
		{
			return typeof(TValue);
		}

		public override bool IsAssignable<T>()
		{
			return Assignable<TValue, T>.Is;
		}

		public override object GetObject()
		{
			return m_Value;
		}

		public override void SetObject(object val)
		{
			Value = (TValue)val;
		}

		public override string ToString()
		{
			return $"{m_Value}";
		}

	}
}