using System;
using UnityEngine.UI;
using UVMBinding.Core;

namespace UVMBinding.Events
{
	public class DropdownSelectStringEvent : ViewEventBase
	{
		Dropdown m_Dropdown;

		void Awake()
		{
			if (TryGetComponent(out Dropdown m_Dropdown))
			{
				m_Dropdown.onValueChanged.AddListener(OnChange);
			}
		}

		void OnChange(int value)
		{
			if (value < 0 || m_Dropdown.options.Count <= value)
			{
				Dispatch(default(string));
			}
			else
			{
				var label = m_Dropdown.options[value].text;
				Dispatch(label);
			}
		}

		public override Type EventType()
		{
			return typeof(string);
		}
	}

}