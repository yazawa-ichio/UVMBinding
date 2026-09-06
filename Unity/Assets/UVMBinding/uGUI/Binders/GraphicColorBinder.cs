using UnityEngine;
using UnityEngine.UI;
namespace UVMBinding.Binders
{
	[SemanticRole(SemanticRole.None)]
	public class GraphicColorBinder : Binder<Color, Graphic>
	{
		protected override void UpdateValue(Color value)
		{
			Target.color = value;
		}
	}
}