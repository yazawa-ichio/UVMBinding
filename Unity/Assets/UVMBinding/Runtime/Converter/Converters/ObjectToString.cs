using UnityEngine;

namespace UVMBinding.Converters
{

	public class ObjectToString : ConverterBase<object, string>
	{
		[SerializeField]
		string m_Format = default;

		public override string Convert(object input)
		{
			if (string.IsNullOrEmpty(m_Format))
			{
				return input.ToString();
			}
			return string.Format(m_Format, input);
		}
	}

}