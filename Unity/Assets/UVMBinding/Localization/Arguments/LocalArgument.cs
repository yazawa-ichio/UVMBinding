using UnityEngine;
using UnityEngine.Localization;

namespace UVMBinding.Arguments
{
	public class LocalArgument : EventArgument<Locale>
	{
		[SerializeField]
		Locale m_Locale;

		public override Locale GetValue()
		{
			return m_Locale;
		}
	}

}