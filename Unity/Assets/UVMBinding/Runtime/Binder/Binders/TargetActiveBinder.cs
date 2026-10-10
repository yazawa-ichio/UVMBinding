using UnityEngine;

namespace UVMBinding.Binders
{
	[SemanticRole(SemanticRole.None)]
	public class TargetActiveBinder : Binder<bool>
	{
		[SerializeField]
		Behaviour m_Target = null;
		[SerializeField]
		bool m_ChangeGameObject;

		protected override void UpdateValue(bool value)
		{
			if (m_Target != null)
			{
				if (m_ChangeGameObject)
				{
					m_Target.gameObject.SetActive(value);
				}
				else
				{
					m_Target.enabled = value;
				}
			}
		}

	}

}