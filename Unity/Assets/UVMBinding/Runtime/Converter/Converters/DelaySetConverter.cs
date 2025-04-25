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
	public abstract class DelaySetConverter<T> : ConverterBase<T, T>
	{
		[SerializeField]
		T m_DefaultValue;

		[SerializeField]
		float m_Delay = 0f;

		bool m_Pendding = false;
		T m_DelayValue;
		float m_LastTime = 0f;

		float GetCurrentTime()
		{
			return Time.unscaledTime;
		}

		protected override void OnUpdate()
		{
			if (!m_Pendding) return;

			if (GetCurrentTime() - m_LastTime > m_Delay)
			{
				var val = m_DelayValue;
				m_DelayValue = default(T);
				m_DefaultValue = val;
				SetImmediate(m_DefaultValue);
			}
		}

		public override T Convert(T input)
		{
			m_Pendding = true;
			m_LastTime = GetCurrentTime();
			m_DelayValue = input;
			return m_DefaultValue;
		}

	}
}