using System;
using UVMBinding.Logger;

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
		Action<T> m_Action;

		internal SubscribeHandle(IBindingProperty<T> owner, Action<T> action)
		{
			m_Property = owner;
			m_Event = null;
			m_Action = action;
			Log.Trace("Create Property SubscribeHandle<{0}> Path:{1}", typeof(T), owner.Path);
		}

		internal SubscribeHandle(Event<T> owner, Action<T> action)
		{
			m_Property = null;
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
			if (m_Event != null)
			{
				Log.Trace("Dispose Event SubscribeHandle<{0}> Name:{1}", typeof(T), m_Event.Name);
				m_Event.OnEvent -= m_Action;
				m_Event = null;
				m_Action = null;
			}
		}
	}

}