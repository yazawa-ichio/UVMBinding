using System;
using System.Collections.Generic;
using UVMBinding.Logger;

namespace UVMBinding.Core
{

	public class EventBroker
	{
		EventBase m_Event;

		public SubscribeHandle Subscribe(string name, Action onViewEvent)
		{
			Log.Debug("Event Subscribe Name:{0}", name);
			var e = Get<Event>(name);
			if (e == null)
			{
				e = new Event(name);
				SetNewEvent(e);
			}
			e.OnEvent += onViewEvent;
			return new SubscribeHandle(e, onViewEvent);
		}

		public SubscribeHandle SubscribeByFunc(string name, Func<Action> onViewEvent)
		{
			return Subscribe(name, () =>
			{
				var action = onViewEvent();
				action?.Invoke();
			});
		}

		public void Unsubscribe(string name, Action onViewEvent)
		{
			Log.Debug("Event Unsubscribe Name:{0}", name);
			var e = Get<Event>(name);
			if (e != null)
			{
				e.OnEvent -= onViewEvent;
			}
		}

		public SubscribeHandle<T> Subscribe<T>(string name, Action<T> onViewEvent)
		{
			Log.Debug("Event Subscribe<{0}> Name:{1}", typeof(T), name);
			var e = Get<Event<T>>(name);
			if (e == null)
			{
				e = new Event<T>(name);
				SetNewEvent(e);
			}
			e.OnEvent += onViewEvent;
			return new SubscribeHandle<T>(e, onViewEvent);
		}

		public SubscribeHandle<T> SubscribeByFunc<T>(string name, Func<System.Action<T>> onViewEvent)
		{
			return Subscribe<T>(name, (x) =>
			{
				var action = onViewEvent();
				action?.Invoke(x);
			});
		}

		public void Unsubscribe<T>(string name, Action<T> onViewEvent)
		{
			Log.Debug("Event Unsubscribe<{0}> Name:{1}", typeof(T), name);
			var e = Get<Event<T>>(name);
			if (e != null)
			{
				e.OnEvent -= onViewEvent;
			}
		}

		public void Publish(string name)
		{
			var e = Get<Event>(name);
			if (e != null)
			{
				Log.Trace("Publish {0}", name);
				e.Invoke();
			}
			else
			{
				Log.Warning("Not Found Publish Event Name:{0}", name);
			}
		}

		public void Publish<T>(string name, T args)
		{
			var e = Get<Event<T>>(name);
			if (e != null)
			{
				Log.Trace("Publish<{0}> Name:{1} arg {2}", typeof(T), name, args);
				e.Invoke(args);
			}
			else
			{
				Log.Warning("Not Found Publish<0> Event Name:{0}", typeof(T), name);
			}
		}

		T Get<T>(string name) where T : EventBase
		{
			var e = m_Event;
			while (e != null)
			{
				if (e is T target && target.Name == name)
				{
					return target;
				}
				e = e.Next;
			}
			Log.Trace("NotFound Event Name:{0}", name);
			return null;
		}

		void SetNewEvent(EventBase newEvent)
		{
			if (m_Event == null)
			{
				m_Event = newEvent;
				return;
			}
			var e = m_Event;
			while (e.Next != null)
			{
				e = e.Next;
			}
			e.Next = newEvent;
			Log.Trace("New Event {0} Name:{1}", newEvent, newEvent.Name);
		}

#if UNITY_EDITOR
		internal IEnumerable<EventBase> GetAll()
		{
			var e = m_Event;
			while (e != null)
			{
				yield return e;
				e = e.Next;
			}
		}
#endif
	}
}