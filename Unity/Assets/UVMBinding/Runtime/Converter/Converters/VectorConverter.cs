using UnityEngine;

namespace UVMBinding.Converters
{
	[DispName("Core/Vector/FloatToVector3")]
	public class FloatToVector3 : ConverterBase<float, Vector3>
	{
		[System.Flags]
		public enum TargetType
		{
			X = 0x001,
			Y = 0x002,
			Z = 0x004,
		}

		[SerializeField]
		TargetType m_Target;
		[SerializeField]
		bool m_Inverse;

		public override Vector3 Convert(float input)
		{
			Vector3 ret = default;
			if ((m_Target & TargetType.X) != 0)
			{
				ret.x = m_Inverse ? input : -input;
			}
			if ((m_Target & TargetType.Y) != 0)
			{
				ret.y = m_Inverse ? input : -input;
			}
			if ((m_Target & TargetType.Z) != 0)
			{
				ret.z = m_Inverse ? input : -input;
			}
			return ret;
		}

		public override bool TryInverseConvert(Vector3 value, ref float ret)
		{
			if ((m_Target & TargetType.X) != 0)
			{
				ret = m_Inverse ? value.x : -value.x;
				return true;
			}
			if ((m_Target & TargetType.Y) != 0)
			{
				ret = m_Inverse ? value.y : -value.y;
				return true;
			}
			if ((m_Target & TargetType.Z) != 0)
			{
				ret = m_Inverse ? value.z : -value.z;
				return true;
			}
			return false;
		}

	}

	[DispName("Core/Vector/FloatToVector2")]
	public class FloatToVector2 : ConverterBase<float, Vector2>
	{
		[System.Flags]
		public enum TargetType
		{
			X = 0x001,
			Y = 0x002,
		}

		[SerializeField]
		TargetType m_Target;
		[SerializeField]
		bool m_Inverse;

		public override Vector2 Convert(float input)
		{
			Vector2 ret = default;
			if ((m_Target & TargetType.X) != 0)
			{
				ret.x = m_Inverse ? input : -input;
			}
			if ((m_Target & TargetType.Y) != 0)
			{
				ret.y = m_Inverse ? input : -input;
			}
			return ret;
		}

		public override bool TryInverseConvert(Vector2 value, ref float ret)
		{
			if ((m_Target & TargetType.X) != 0)
			{
				ret = m_Inverse ? value.x : -value.x;
				return true;
			}
			if ((m_Target & TargetType.Y) != 0)
			{
				ret = m_Inverse ? value.y : -value.y;
				return true;
			}
			return false;
		}

	}

	[DispName("Core/Vector/Vector3ToFloat")]
	public class Vector3ToFloat : ConverterBase<Vector3, float>
	{

		public enum TargetType
		{
			X = 0,
			Y = 1,
			Z = 2,
		}

		[SerializeField]
		TargetType m_Target;
		[SerializeField]
		bool m_Inverse;

		public override float Convert(Vector3 input)
		{
			switch (m_Target)
			{
				case TargetType.Y:
					return m_Inverse ? input.y : -input.y;
				case TargetType.Z:
					return m_Inverse ? input.z : -input.z;
				case TargetType.X:
				default:
					return m_Inverse ? input.x : -input.x;
			}
		}

	}

	[DispName("Core/Vector/Vector2ToFloat")]
	public class Vector2ToFloat : ConverterBase<Vector2, float>
	{

		public enum TargetType
		{
			X = 0,
			Y = 1,
		}

		[SerializeField]
		TargetType m_Target;
		[SerializeField]
		bool m_Inverse;

		public override float Convert(Vector2 input)
		{
			switch (m_Target)
			{
				case TargetType.Y:
					return m_Inverse ? input.y : -input.y;
				case TargetType.X:
				default:
					return m_Inverse ? input.x : -input.x;
			}
		}

	}

}