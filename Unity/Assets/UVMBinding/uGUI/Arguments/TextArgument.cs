using UnityEngine;
using UnityEngine.UI;

namespace UVMBinding.Arguments
{
	public class TextArgument : EventArgument<string>
	{
		[SerializeField]
		Text m_Target = default;

		public override string GetValue()
		{
			return m_Target.text;
		}

	}

}