using System;
using System.Collections.Generic;
using UVMBinding.Logger;

namespace UVMBinding.Core
{

	[UnityEngine.Scripting.Preserve]
	public class PropertyContainer
	{

		Dictionary<string, BindingProperty> m_Properties = new();

		readonly PropertyVersion m_Version = new PropertyVersion();

		internal int Version => m_Version.Value;

		internal event Action<IBindingProperty> OnNewProperty;

		internal event Action<string, IValueHolder> OnChangedValue;

		internal bool ShouldCreateProperty;

		HashSet<string> m_SubscribePaths;

		Action<string, IValueHolder> m_InvokeChangedValueCache;
		Action<string, IValueHolder> InvokeChangedValueCache
		{
			get
			{
				if (m_InvokeChangedValueCache == null)
				{
					m_InvokeChangedValueCache = InvokeChangedValue;
				}
				return m_InvokeChangedValueCache;
			}
		}

		void InvokeChangedValue(string path, IValueHolder holder)
		{
			OnChangedValue?.Invoke(path, holder);
		}

		public SubscribeHandle<T> Subscribe<T>(string path, Action<T> notify, bool notifyCurrent = false)
		{
			return SubscribeImpl(path, notify, false, notifyCurrent);
		}

		internal SubscribeHandle<T> SubscribeStrict<T>(string path, Action<T> notify, bool notifyCurrent = false)
		{
			return SubscribeImpl(path, notify, true, notifyCurrent);
		}

		SubscribeHandle<T> SubscribeImpl<T>(string path, Action<T> notify, bool strict, bool notifyCurrent)
		{
			Log.Debug("Property Subscribe<{0}> Path:{1} Strict:{2}", typeof(T), path, strict);
			var property = GetOrCreate<T>(path);
			if (property.Holder != null && !CanReceive<T>(property, strict))
			{
				throw CreateMismatchException<T>(path);
			}
			var subscriber = new PropertySubscriber<T> { Action = notify, Strict = strict };
			property.AddSubscriber(subscriber);
			if (notifyCurrent)
			{
				subscriber.Invoke(property.Holder);
			}
			return new SubscribeHandle<T>(property, subscriber);
		}

		public void Unsubscribe<T>(string path, Action<T> notify)
		{
			Log.Debug("Property Unsubscribe<{0}> Path:{1}", typeof(T), path);
			if (!m_Properties.TryGetValue(path, out var property))
			{
				return;
			}
			property.RemoveSubscriber(notify);
		}

		static bool CanReceive<T>(BindingProperty property, bool strict)
		{
			if (property.GetBindType() == typeof(T))
			{
				return true;
			}
			return !strict && property.IsAssignable<T>();
		}

		internal InvalidCastException CreateMismatchException<T>(string path)
		{
			var registered = m_Properties.TryGetValue(path, out var property) ? property.GetBindType() : null;
			return new InvalidCastException(
				$"Property Type Mismatch Path:{path} Registered:{registered} Requested:{typeof(T)}");
		}

		internal BindingProperty GetOrCreate<T>(string path)
		{
			if (m_Properties.TryGetValue(path, out var property))
			{
				if (property.Holder == null)
				{
					property.SwapHolder(new ValueHolder<T>());
				}
				return property;
			}
			return NewProperty<T>(path);
		}

		internal bool TryGet(string path, out IBindingProperty property)
		{
			if (m_Properties.TryGetValue(path, out BindingProperty prop))
			{
				property = prop;
				return true;
			}
			if (ShouldCreateProperty)
			{
				property = NewProperty<object>(path);
				return true;
			}
			property = null;
			return false;
		}

		BindingProperty NewProperty<T>(string path)
		{
			Log.Trace("Register New Property<{0}> Path:{1}", typeof(T), path);
			var ret = new BindingProperty(path, new ValueHolder<T>());
			ret.SetVersion(m_Version);
			m_Properties[path] = ret;
			OnNewProperty?.Invoke(ret);
			if (m_SubscribePaths != null && m_SubscribePaths.Contains(path))
			{
				ret.OnChangedValue += InvokeChangedValueCache;
			}
			return ret;
		}

		internal bool UnSet(string path)
		{
			if (!m_Properties.TryGetValue(path, out var property) || property.Holder == null)
			{
				return false;
			}
			Log.Debug("UnSet Property Path:{0} Type:{1}", path, property.GetBindType());
			property.SwapHolder(null);
			return true;
		}

		internal bool TryGetValue<T>(string path, out T value)
		{
			if (m_Properties.TryGetValue(path, out var property))
			{
				return property.TryGetValue(out value);
			}
			GetOrCreate<T>(path);
			value = default;
			return true;
		}

		internal bool TryGetValueAs<T>(string path, out T value)
		{
			if (m_Properties.TryGetValue(path, out var property))
			{
				return property.TryGetValueAs(out value);
			}
			value = default;
			return false;
		}

		internal bool TrySetValue<T>(string path, T value)
		{
			return GetOrCreate<T>(path).TrySetValue(value);
		}

		public void SetDirty(string path)
		{
			Log.Trace("SetDirty {0}", path);
			if (m_Properties.TryGetValue(path, out BindingProperty property))
			{
				property.SetDirty();
			}
		}

		public void SetAllDirty()
		{
			Log.Trace("SetAllDirty");
			foreach (var property in m_Properties.Values)
			{
				property.SetDirty();
			}
		}

		internal IBindingProperty[] GetAll()
		{
			using (ListPool<IBindingProperty>.Use(out var list))
			{
				return GetAllImpl(list).ToArray();
			}
		}

		internal List<IBindingProperty> GetAllImpl(List<IBindingProperty> ret)
		{
			foreach (var property in m_Properties.Values)
			{
				ret.Add(property);
			}
			return ret;
		}

		internal void RegisterErasedPath(string path)
		{
			if (m_SubscribePaths == null)
			{
				m_SubscribePaths = new HashSet<string>();
			}
			if (m_SubscribePaths.Add(path) && m_Properties.TryGetValue(path, out var property))
			{
				property.OnChangedValue += InvokeChangedValueCache;
			}
		}

	}

}