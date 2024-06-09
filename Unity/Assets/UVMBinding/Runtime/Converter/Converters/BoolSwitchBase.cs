using System.Collections.Generic;
using UnityEngine;

namespace UVMBinding.Converters
{
	public abstract class BoolSwitchBase<T> : ConverterBase<bool, T>
	{
		static EqualityComparer<T> s_EqualityComparer = EqualityComparer<T>.Default;

		[SerializeField]
		T m_On;
		[SerializeField]
		T m_Off;

		public override T Convert(bool input)
		{
			return input ? m_On : m_Off;
		}

		public override bool TryInverseConvert(T value, ref bool ret)
		{
			if (s_EqualityComparer.Equals(m_On))
			{
				ret = true;
				return true;
			}
			if (s_EqualityComparer.Equals(m_Off))
			{
				ret = false;
				return true;
			}
			return false;
		}

	}

}