using UnityEngine.UI;

namespace UVMBinding.Binders
{
	[SemanticRole(SemanticRole.Scrollbar)]
	public class ScrollbarBinder : Binder<float, Scrollbar>
	{
		protected override void OnInit(Scrollbar target)
		{
			target.onValueChanged.AddListener(Set);
		}

		protected override void UpdateValue(float value)
		{
			Target.value = value;
		}
	}
}