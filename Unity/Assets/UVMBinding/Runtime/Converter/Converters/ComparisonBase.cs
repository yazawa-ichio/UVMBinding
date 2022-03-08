using UnityEngine;

namespace UVMBinding.Converters
{
	public abstract class ComparisonBase<T> : ConverterBase<T, bool> where T : System.IComparable<T>
	{
		[SerializeField]
		ComparisonOperatorType m_Type = ComparisonOperatorType.Equal;
		[SerializeField]
		T m_Value = default;

		public override bool Convert(T input)
		{
			switch (m_Type)
			{
				case ComparisonOperatorType.Equal:
					return m_Value.CompareTo(input) == 0;
				case ComparisonOperatorType.NotEqual:
					return m_Value.CompareTo(input) != 0;
				case ComparisonOperatorType.GreaterThan:
					return m_Value.CompareTo(input) < 0;
				case ComparisonOperatorType.GreaterThanOrEqual:
					return m_Value.CompareTo(input) <= 0;
				case ComparisonOperatorType.LessThan:
					return m_Value.CompareTo(input) > 0;
				case ComparisonOperatorType.LessThanOrEqual:
					return m_Value.CompareTo(input) >= 0;
			}
			return false;
		}

	}

}