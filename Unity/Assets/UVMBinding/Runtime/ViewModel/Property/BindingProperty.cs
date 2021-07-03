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
		internal BindingProperty Next;
		protected Action m_OnPostChanged;

		public event Action OnPostChanged
		{
			add => m_OnPostChanged += value;
			remove => m_OnPostChanged -= value;
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

	internal class BindingProperty<TValue> : BindingProperty, IBindingProperty<TValue>
	{
		static EqualityComparer<TValue> s_EqualityComparer = EqualityComparer<TValue>.Default;

		TValue m_Value;
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
				m_OnPostChanged?.Invoke();
			}
		}

		public BindingProperty(string path) : base(path) { m_Value = default; }

		public event Action<TValue> OnChanged;

		public override void SetDirty()
		{
			Log.Trace("BindingProperty<{0}> SetDitry Path:{1}", typeof(TValue), Path);
			Hash++;
			OnChanged?.Invoke(m_Value);
			m_OnPostChanged?.Invoke();
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