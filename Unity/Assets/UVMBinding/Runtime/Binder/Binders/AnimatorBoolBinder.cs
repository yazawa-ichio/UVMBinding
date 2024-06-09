using UnityEngine;

namespace UVMBinding.Binders
{
	public class AnimatorBoolBinder : Binder<bool, Animator>
	{
		[SerializeField]
		string m_ParameterName;
		int m_ParameterHash;

		protected override void OnBind()
		{
			m_ParameterHash = Animator.StringToHash(m_ParameterName);
		}

		protected override void UpdateValue(bool value)
		{
			Target.SetBool(m_ParameterHash, value);
		}
	}
}