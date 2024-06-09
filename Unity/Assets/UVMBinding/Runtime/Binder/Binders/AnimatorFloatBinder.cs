using UnityEngine;

namespace UVMBinding.Binders
{
	public class AnimatorFloatBinder : Binder<float, Animator>
	{
		[SerializeField]
		string m_ParameterName;
		int m_ParameterHash;

		protected override void OnBind()
		{
			m_ParameterHash = Animator.StringToHash(m_ParameterName);
		}

		protected override void UpdateValue(float value)
		{
			Target.SetFloat(m_ParameterHash, value);
		}
	}
}