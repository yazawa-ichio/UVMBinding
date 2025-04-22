using UnityEngine;
using UnityEngine.Events;

namespace UVMBinding
{
	public abstract class BindValueProxy<T> : Binder<T>
	{
		public interface IReceiver
		{
			void OnBind(T value);
		}

		[SerializeField]
		UnityEvent<T> m_Event;

		IReceiver m_Receiver;

		protected override void OnBind()
		{
			if (TryGetComponent<IReceiver>(out var receiver))
			{
				m_Receiver = receiver;
			}
		}

		protected override void UpdateValue(T value)
		{
			m_Receiver?.OnBind(value);
			m_Event?.Invoke(value);
		}
	}
}