using UnityEngine;
using UnityEngine.UI;

namespace UVMBinding.Arguments
{
	public class GraphicColorArgument : EventArgument<Color>
	{
		[SerializeField]
		Graphic m_Target = default;

		public override Color GetValue()
		{
			return m_Target.color;
		}

	}

}