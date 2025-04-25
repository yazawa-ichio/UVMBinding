using UnityEngine;
using UnityEngine.EventSystems;

namespace UVMBinding.Events
{
	public class EventSystemSelectBinder : Binder<bool>, ISelectHandler, IDeselectHandler
	{
		[SerializeField]
		bool m_ApplySelected;

		public void OnDeselect(BaseEventData eventData)
		{
			Set(false);
		}

		public void OnSelect(BaseEventData eventData)
		{
			Set(true);
		}

		protected override void UpdateValue(bool value)
		{
			if (m_ApplySelected)
			{
				if (value)
				{
					EventSystem.current.SetSelectedGameObject(gameObject);
				}
				else if (gameObject == EventSystem.current.currentSelectedGameObject)
				{
					EventSystem.current.SetSelectedGameObject(null);
				}
			}
		}
	}
}