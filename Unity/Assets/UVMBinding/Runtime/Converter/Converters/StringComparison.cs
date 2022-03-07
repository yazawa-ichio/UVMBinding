using UnityEngine;

namespace UVMBinding.Converters
{
	[DispName("Core/Comparison/StringToBool")]
	public class StringComparison : ConverterBase<string, bool>
	{
		[SerializeField]
		string m_Value;
		[SerializeField]
		System.StringComparison m_Comparison = System.StringComparison.Ordinal;

		public override bool Convert(string input)
		{
			return string.Equals(m_Value, input, m_Comparison);
		}
	}

}