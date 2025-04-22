using UnityEngine;

namespace UVMBinding
{

	public abstract class BindSourceProvider : MonoBehaviour
	{
		[SerializeField]
		BindSourceTargetData m_Target = new BindSourceTargetData();

		public virtual bool IsTarget(Component component)
		{
			return m_Target.IsTarget(component);
		}
	}
}