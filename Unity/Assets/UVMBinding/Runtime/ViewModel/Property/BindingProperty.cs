using System;
using System.Collections.Generic;
using UVMBinding.Core;
using UVMBinding.Logger;

namespace UVMBinding
{
	internal sealed class PropertyVersion
	{
		public int Value;
	}

	internal abstract class PropertySubscriber
	{
		public abstract void Invoke(IValueHolder holder);
		public abstract bool Match(object action);
	}

	internal sealed class PropertySubscriber<T> : PropertySubscriber
	{
		public Action<T> Action;
		public bool Strict;

		public override void Invoke(IValueHolder holder)
		{
			if (holder == null)
			{
				return;
			}
			if (holder is ValueHolder<T> typed)
			{
				Action(typed.Value);
				return;
			}
			if (Strict)
			{
				return;
			}
			if (holder.TryGetValue<T>(out var value))
			{
				Action(value);
			}
		}

		public override bool Match(object action)
		{
			return (object)Action == action;
		}
	}

	[UnityEngine.Scripting.Preserve]
	internal class BindingProperty : IBindingProperty
	{
		public string Path { get; private set; }
		public int Hash { get; private set; }

		IValueHolder m_Holder;
		List<PropertySubscriber> m_Subscribers;
		Action<string, IValueHolder> m_OnChangedValue;

		internal IValueHolder Holder => m_Holder;

		public event Action<string, IValueHolder> OnChangedValue
		{
			add => m_OnChangedValue += value;
			remove => m_OnChangedValue -= value;
		}

		PropertyVersion m_Version;

		public BindingProperty(string path, IValueHolder holder)
		{
			Path = path;
			Hash = GetHashCode();
			m_Holder = holder;
		}

		internal void SetVersion(PropertyVersion version)
		{
			m_Version = version;
		}

		void Changed()
		{
			Hash++;
			if (m_Version != null)
			{
				m_Version.Value++;
			}
			Notify();
		}

		internal void SwapHolder(IValueHolder holder)
		{
			m_Holder = holder;
			SetDirty();
		}

		public Type GetBindType()
		{
			return m_Holder != null ? m_Holder.GetValueType() : typeof(void);
		}

		public bool IsAssignable<T>()
		{
			return m_Holder != null && m_Holder.IsAssignable<T>();
		}

		public bool CanReceive<T>(bool allowCast)
		{
			return m_Holder != null && m_Holder.CanReceive<T>(allowCast);
		}

		public BindKind GetBindKind<T>(bool allowCast)
		{
			return m_Holder != null ? m_Holder.GetBindKind<T>(allowCast) : BindKind.None;
		}

		public object GetObject()
		{
			return m_Holder?.GetObject();
		}

		public void SetObject(object val)
		{
			m_Holder?.SetObjectValue(val);
			Changed();
		}

		public bool TrySetObject(object val)
		{
			if (m_Holder == null || !m_Holder.TrySetValueAs(val))
			{
				return false;
			}
			Changed();
			return true;
		}

		public bool TryGetValue<T>(out T value)
		{
			if (m_Holder is ValueHolder<T> typed)
			{
				value = typed.Value;
				return true;
			}
			if (m_Holder != null)
			{
				return m_Holder.TryGetValue(out value);
			}
			value = default;
			return false;
		}

		public bool TryGetValueCast<T>(out T value)
		{
			if (m_Holder is ValueHolder<T> typed)
			{
				value = typed.Value;
				return true;
			}
			if (m_Holder != null)
			{
				return m_Holder.TryGetValueCast(out value);
			}
			value = default;
			return false;
		}

		internal T GetValue<T>()
		{
			TryGetValue<T>(out var value);
			return value;
		}

		public bool TryGetValueAs<T>(out T value)
		{
			if (m_Holder != null)
			{
				return m_Holder.TryGetValueAs(out value);
			}
			value = default;
			return false;
		}

		public bool TrySetValue<T>(T value)
		{
			bool changed;
			if (m_Holder is ValueHolder<T> typed)
			{
				changed = !EqualityComparer<T>.Default.Equals(typed.Value, value);
				typed.Value = value;
			}
			else if (m_Holder == null || !m_Holder.TrySetValue(value, out changed))
			{
				return false;
			}
			if (changed)
			{
				Log.Trace("BindingProperty Update Path:{0} Value {1}", Path, value);
				Changed();
			}
			return true;
		}

		public bool TrySetValueCast<T>(T value)
		{
			bool changed;
			if (m_Holder is ValueHolder<T> typed)
			{
				changed = !EqualityComparer<T>.Default.Equals(typed.Value, value);
				typed.Value = value;
			}
			else if (m_Holder == null || !m_Holder.TrySetValueCast(value, out changed))
			{
				return false;
			}
			if (changed)
			{
				Log.Trace("BindingProperty Update Path:{0} Value {1}", Path, value);
				Changed();
			}
			return true;
		}

		public void SetDirty()
		{
			Log.Trace("BindingProperty SetDirty Path:{0}", Path);
			Changed();
		}

		void Notify()
		{
			if (m_Subscribers != null)
			{
				for (int i = 0; i < m_Subscribers.Count; i++)
				{
					m_Subscribers[i].Invoke(m_Holder);
				}
			}
			m_OnChangedValue?.Invoke(Path, m_Holder);
		}

		internal void AddSubscriber(PropertySubscriber subscriber)
		{
			if (m_Subscribers == null)
			{
				m_Subscribers = new List<PropertySubscriber>();
			}
			m_Subscribers.Add(subscriber);
		}

		internal void RemoveSubscriber(PropertySubscriber subscriber)
		{
			m_Subscribers?.Remove(subscriber);
		}

		internal void RemoveSubscriber(object action)
		{
			if (m_Subscribers == null)
			{
				return;
			}
			for (int i = m_Subscribers.Count - 1; i >= 0; i--)
			{
				if (m_Subscribers[i].Match(action))
				{
					m_Subscribers.RemoveAt(i);
					return;
				}
			}
		}

		public override string ToString()
		{
			return $"{GetObject()}";
		}

	}
}