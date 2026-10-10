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
		Action<T> m_Action;
		BindingProperty m_Property;
		PropertySubscriber m_Subscriber;

		internal SubscribeHandle(BindingProperty owner, PropertySubscriber subscriber) : this()
		{
			m_Property = owner;
			m_Subscriber = subscriber;
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
				m_Property.RemoveSubscriber(m_Subscriber);
				m_Property = null;
				m_Subscriber = null;
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