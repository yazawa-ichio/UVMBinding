using UnityEngine;
using UnityEngine.UI;

namespace UVMBinding.Converters
{
	[DispName("uGUI/DropdownSelectToString")]
	public class DropdownStringToSelect : ConverterBase<string, int>
	{
		[SerializeField, SelfComponentTarget]
		Dropdown m_Dropdown;

		public override int Convert(string input)
		{
			if (m_Dropdown == null)
			{
				return -1;
			}
			var options = m_Dropdown.options;
			for (int i = 0; i < options.Count; i++)
			{
				if (options[i].text == input)
				{
					return i;
				}
			}
			return -1;
		}

		public override bool TryInverseConvert(int value, ref string ret)
		{
			if (m_Dropdown == null)
			{
				return false;
			}
			var options = m_Dropdown.options;
			if (0 <= value && value < options.Count)
			{
				ret = options[value].text;
				return true;
			}
			ret = null;
			return true;
		}

	}
}