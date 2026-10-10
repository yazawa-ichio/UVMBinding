using UnityEngine.UI;

namespace UVMBinding.Binders
{
	[SemanticRole(SemanticRole.None)]
	[GovernsState(GovernedState.Interactable)]
	public class InteractableBinder : Binder<bool, Selectable>
	{
		protected override void UpdateValue(bool value)
		{
			Target.interactable = value;
		}
	}
}