using UnityEngine;

namespace UVMBinding.Binders
{
	public class PositionBinder : Binder<Vector3>
	{
		[SerializeField]
		bool m_Global;
		[SerializeField]
		Vector3 m_Offset;

		protected override void UpdateValue(Vector3 value)
		{
			if (m_Global)
			{
				transform.position = m_Offset + value;
			}
			else
			{
				transform.localPosition = m_Offset + value;
			}
		}
	}
}