using UnityEngine;
using UnityEngine.EventSystems;

namespace UVMBinding.Events
{
	public class PointerMovePositionBinder : Binder<Vector2>, IPointerMoveHandler
	{
		protected override void UpdateValue(Vector2 value)
		{
		}

		void IPointerMoveHandler.OnPointerMove(PointerEventData eventData)
		{
			Set(eventData.position);
		}
	}
}