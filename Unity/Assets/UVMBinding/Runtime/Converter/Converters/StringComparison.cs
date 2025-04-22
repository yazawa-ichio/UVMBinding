using UnityEngine;

namespace UVMBinding.Converters
{
	[DispName("Core/Comparison/StringToBool")]
	public class StringComparison : ConverterBase<object, bool>
	{
		[SerializeField]
		string m_Value;
		[SerializeField]
		System.StringComparison m_Comparison = System.StringComparison.Ordinal;

		public override bool Convert(object input)
		{
			return string.Equals(m_Value, input?.ToString(), m_Comparison);
		}
	}

}