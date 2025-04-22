using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

namespace UVMBinding.Binders
{
	[RequireComponent(typeof(LocalizeStringEvent))]
	public class LocalizeVariableBinder : Binder<IVariable, LocalizeStringEvent>
	{
		[SerializeField]
		string m_VariableName;

		protected override void UpdateValue(IVariable value)
		{
			Target.StringReference[m_VariableName] = value;
		}
	}
}