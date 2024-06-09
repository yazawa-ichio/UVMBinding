using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

namespace UVMBinding.Binders
{
	[RequireComponent(typeof(LocalizeStringEvent))]
	public class LocalizeVariableBinder : Binder<LocalizeTextKey, LocalizeStringEvent>
	{
		[SerializeField]
		string m_VariableName;
		[SerializeField]
		LocalizedString m_LocalizedString;

		protected override void UpdateValue(LocalizeTextKey value)
		{
			Target.StringReference[m_VariableName] = m_LocalizedString;
		}
	}

}