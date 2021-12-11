using UnityEngine.UI;

namespace UVMBinding.Binders
{
	public class InteractableBinder : Binder<bool, Selectable>
	{
		protected override void UpdateValue(bool value)
		{
			Target.interactable = value;
		}
	}
}
