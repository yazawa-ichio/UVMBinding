using UnityEngine;
using UVMBinding.Logger;

namespace UVMBinding.Core
{
	public abstract class ViewEventBase : MonoBehaviour, IViewEvent
	{
		string IViewEvent.Name => m_EventName;

		[SerializeField, EventNameSelect]
		string m_EventName = default;

		internal IViewEventDispatcher m_Dispatcher;

		public abstract System.Type EventType();

		bool IViewElement.CanUse(IView view) => CanUse(view);

		protected virtual bool CanUse(IView view) => true;

		void IViewEvent.Bind(IViewEventDispatcher dispatcher)
		{
			Log.Trace(this, "{0} Bind Dispatcher {1}", this, m_EventName);
			m_Dispatcher = dispatcher;
		}

		public void Dispatch()
		{
			Debug.Assert(m_Dispatcher != null, "not found Dispatcher");
			m_Dispatcher?.Dispatch(m_EventName);
		}

		public void Dispatch<T>(T args)
		{
			Debug.Assert(m_Dispatcher != null, "not found Dispatcher");
			m_Dispatcher?.Dispatch(m_EventName, args);
		}

	}

}