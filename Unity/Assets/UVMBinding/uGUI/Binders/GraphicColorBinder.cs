using UnityEngine;
using UnityEngine.UI;
namespace UVMBinding.Binders
{
	public class GraphicColorBinder : Binder<Color, Graphic>
	{
		protected override void UpdateValue(Color value)
		{
			Target.color = value;
		}
	}
}