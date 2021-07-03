using UnityEngine;
using UnityEngine.UI;

namespace UVMBinding.Arguments
{

	public class InputFieldArgument : EventArgument<string>
	{
		[SerializeField]
		InputField m_Target = default;

		public override string GetValue()
		{
			return m_Target.text;
		}

	}

}