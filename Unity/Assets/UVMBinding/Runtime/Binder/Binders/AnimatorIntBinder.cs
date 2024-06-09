using UnityEngine;

namespace UVMBinding.Binders
{
	public class AnimatorIntBinder : Binder<int, Animator>
	{
		[SerializeField]
		string m_ParameterName;
		int m_ParameterHash;

		protected override void OnBind()
		{
			m_ParameterHash = Animator.StringToHash(m_ParameterName);
		}

		protected override void UpdateValue(int value)
		{
			Target.SetInteger(m_ParameterHash, value);
		}
	}
}