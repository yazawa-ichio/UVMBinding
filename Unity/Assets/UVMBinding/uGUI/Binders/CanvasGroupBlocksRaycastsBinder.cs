using UnityEngine;
namespace UVMBinding.Binders
{
	[SemanticRole(SemanticRole.None)]
	public class CanvasGroupBlocksRaycastsBinder : Binder<bool, CanvasGroup>
	{
		protected override void UpdateValue(bool value)
		{
			Target.blocksRaycasts = value;
		}
	}
}