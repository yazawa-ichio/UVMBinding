using System;
using UVMBinding.Logger;
using UVMBinding.Core;

namespace UVMBinding
{
	public struct SubscribeHandle : IDisposable
	{
		Event m_Owner;
		Action m_Action;

		internal SubscribeHandle(Event owner, Action action)
		{
			m_Owner = owner;
			m_Action = action;
			Log.Trace("Create Event SubscribeHandle Name:{0}", owner.Name);
		}

		public void Dispose()
		{
			if (m_Owner == null)
			{
				return;
			}
			Log.Trace("Dispose Event SubscribeHandle Name:{0}", m_Owner.Name);
			m_Owner.OnEvent -= m_Action;
			m_Owner = null;
			m_Action = null;
		}
	}

	public struct SubscribeHandle<T> : IDisposable
	{
		Event<T> m_Event;
		IBindingProperty<T> m_Property;
		IBindingProperty m_ObjectProperty;
		Action<T> m_Action;
		Action<string, IValueHolder> m_ObjectAction;

		internal SubscribeHandle(IBindingProperty<T> owner, Action<T> action) : this()
		{
			m_Property = owner;
			m_Action = action;
			Log.Trace("Create Property SubscribeHandle<{0}> Path:{1}", typeof(T), owner.Path);
		}

		internal SubscribeHandle(IBindingProperty owner, Action<string, IValueHolder> action) : this()
		{
			m_ObjectProperty = owner;
			m_ObjectAction = action;
			Log.Trace("Create Property SubscribeHandle<{0}> Path:{1}", typeof(T), owner.Path);
		}

		internal SubscribeHandle(Event<T> owner, Action<T> action) : this()
		{
			m_Event = owner;
			m_Action = action;
			Log.Trace("Create Event SubscribeHandle<{0}> Name:{1}", typeof(T), owner.Name);
		}

		public void Dispose()
		{
			if (m_Property != null)
			{
				Log.Trace("Dispose Property SubscribeHandle<{0}> Path:{1}", typeof(T), m_Property.Path);
				m_Property.OnChanged -= m_Action;
				m_Property = null;
				m_Action = null;
			}
			if (m_ObjectProperty != null)
			{
				m_ObjectProperty.OnChangedValue -= m_ObjectAction;
				m_ObjectProperty = null;
				m_ObjectAction = null;
			}
			if (m_Event != null)
			{
				Log.Trace("Dispose Event SubscribeHandle<{0}> Name:{1}", typeof(T), m_Event.Name);
				m_Event.OnEvent -= m_Action;
				m_Event = null;
				m_Action = null;
			}
		}
	}


	public struct SubscribeAssignableHandle<T> : IDisposable
	{
		PropertyContainer m_Owner;
		string m_Path;
		Action<string, IValueHolder> m_Action;

		internal SubscribeAssignableHandle(PropertyContainer owner, string path, Action<T> action) : this()
		{
			m_Owner = owner;
			m_Path = path;
			m_Action = (p, value) =>
			{
				if (p == path)
				{
					if (value.TryGetValue<T>(out T ret))
					{
						action(ret);
					}
				}
			};
			owner.OnChangedValue += m_Action;
		}

		public void Dispose()
		{
			if (m_Owner != null)
			{
				Log.Trace("Dispose Property SubscribeAssignableHandle<{0}> Path:{1}", typeof(T), m_Path);
				m_Owner.OnChangedValue -= m_Action;
				m_Action = null;
			}
		}
	}

}