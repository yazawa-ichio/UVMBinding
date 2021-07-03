using UnityEngine.UI;

namespace UVMBinding.Binders
{
	public class InputFieldBinder : Binder<string>
	{
		InputField m_Input;

		protected override void OnBind()
		{
			if (TryGetComponent(out m_Input))
			{
				m_Input.onValueChanged.AddListener(Set);
			}
		}

		protected override void UpdateValue(string value)
		{
			if (m_Input != null)
			{
				m_Input.text = value;
			}
		}
	}
}