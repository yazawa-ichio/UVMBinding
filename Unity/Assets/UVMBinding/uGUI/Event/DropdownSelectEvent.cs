using System;
using UnityEngine.UI;
using UVMBinding.Core;

namespace UVMBinding.Events
{
	public class DropdownSelectEvent : ViewEventBase
	{
		void Awake()
		{
			if (TryGetComponent(out Dropdown dropdown))
			{
				dropdown.onValueChanged.AddListener(OnChange);
			}
		}

		void OnChange(int value)
		{
			Dispatch(value);
		}

		public override Type EventType()
		{
			return typeof(int);
		}
	}

}