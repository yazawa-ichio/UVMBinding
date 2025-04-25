using UnityEngine;
using UnityEngine.Events;

namespace UVMBinding
{
	public abstract class BindValueProxy<T> : Binder<T>
	{
		[SerializeField]
		UnityEvent<T> m_Event;

		protected override void UpdateValue(T value)
		{
			m_Event?.Invoke(value);
		}
	}
}