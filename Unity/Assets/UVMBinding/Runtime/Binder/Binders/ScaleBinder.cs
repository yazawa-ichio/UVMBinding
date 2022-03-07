using UnityEngine;

namespace UVMBinding.Binders
{
	public class ScaleBinder : Binder<Vector3>
	{
		[SerializeField]
		Vector3 m_Offset;

		protected override void UpdateValue(Vector3 value)
		{
			transform.localScale = m_Offset + value;
		}
	}
}