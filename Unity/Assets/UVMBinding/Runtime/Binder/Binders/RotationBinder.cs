using UnityEngine;

namespace UVMBinding.Binders
{
	public class RotationBinder : Binder<Quaternion>
	{
		[SerializeField]
		bool m_Global;

		protected override void UpdateValue(Quaternion value)
		{
			if (m_Global)
			{
				transform.rotation = value;
			}
			else
			{
				transform.localRotation = value;
			}
		}
	}
}