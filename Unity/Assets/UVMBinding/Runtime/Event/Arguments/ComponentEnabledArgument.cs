using UnityEngine;

namespace UVMBinding.Arguments
{
	public class ComponentEnabledArgument : EventArgument<bool>
	{
		[SerializeField]
		MonoBehaviour m_Target = default;

		public override bool GetValue()
		{
			return m_Target.enabled;
		}

	}

}