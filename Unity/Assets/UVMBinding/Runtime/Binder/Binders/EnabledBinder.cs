using UnityEngine;

namespace UVMBinding.Binders
{
	[SemanticRole(SemanticRole.None)]
	public class EnabledBinder : Binder<bool>
	{
		[SerializeField, SelfComponentTarget]
		Behaviour m_Target;

		protected override void UpdateValue(bool value)
		{
			if (m_Target)
			{
				m_Target.enabled = value;
			}
		}
	}
}