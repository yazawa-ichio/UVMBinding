using System;
using UnityEngine;

namespace UVMBinding.Converters
{
	[Serializable]
	[DispName("Core/DelaySet/Bool")]
	public class DelaySetBoolConverter : DelaySetConverter<bool> { }
	[Serializable]
	[DispName("Core/DelaySet/Float")]
	public class DelaySetFloatConverter : DelaySetConverter<float> { }
	[Serializable]
	[DispName("Core/DelaySet/Int")]
	public class DelaySetIntConverter : DelaySetConverter<int> { }

	[Serializable]
	public abstract class DelaySetConverter<T> : ConverterBase<T, T>, IUpdatableConverter
	{
		[SerializeField]
		T m_DefaultValue;

		[SerializeField]
		float m_Delay = 0f;

		bool m_Pendding = false;
		T m_DelayValue;
		T m_CurrentValue;
		bool m_HasCurrentValue;
		float m_LastTime = 0f;

		float GetCurrentTime()
		{
			return Time.unscaledTime;
		}

		void IUpdatableConverter.OnUpdate()
		{
			if (!m_Pendding) return;

			if (GetCurrentTime() - m_LastTime > m_Delay)
			{
				m_Pendding = false;
				m_CurrentValue = m_DelayValue;
				m_HasCurrentValue = true;
				m_DelayValue = default(T);
				SetImmediate(m_CurrentValue);
			}
		}

		public override T Convert(T input)
		{
			m_Pendding = true;
			m_LastTime = GetCurrentTime();
			m_DelayValue = input;
			return m_HasCurrentValue ? m_CurrentValue : m_DefaultValue;
		}

	}
}