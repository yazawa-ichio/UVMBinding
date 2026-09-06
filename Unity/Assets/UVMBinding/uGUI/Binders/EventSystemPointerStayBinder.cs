using UnityEngine.EventSystems;

namespace UVMBinding.Events
{
	[SemanticRole(SemanticRole.None)]
	public class EventSystemPointerStayBinder : Binder<bool>, IPointerEnterHandler, IPointerExitHandler
	{
		public void OnPointerEnter(PointerEventData eventData)
		{
			Set(true);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			Set(false);
		}

		protected override void UpdateValue(bool value) { }
	}
}