using UnityEngine;
namespace UVMBinding.Binders
{
	public class CanvasGroupAlphaBinder : Binder<float, CanvasGroup>
	{
		protected override void UpdateValue(float value)
		{
			Target.alpha = value;
		}
	}
}