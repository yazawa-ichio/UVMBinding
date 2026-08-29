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


	//出力の各成分をどの入力軸から取るかを選ぶ。3D キャラの位置を 2D マップへ
	//落とす場合は XZ 平面を使うなど、対応付けは用途によって変わる。
	//符号付きの軸選択のみで、係数による拡大・せん断は扱わない。
	public enum Vector3Axis
	{
		X = 0,
		Y = 1,
		Z = 2,
		MinusX = 3,
		MinusY = 4,
		MinusZ = 5,
		Zero = 6,
	}

	public enum Vector2Axis
	{
		X = 0,
		Y = 1,
		MinusX = 2,
		MinusY = 3,
		Zero = 4,
	}

	[DispName("Core/Vector/Vector3ToVector2")]
	public class Vector3ToVector2 : ConverterBase<Vector3, Vector2>
	{

		[SerializeField]
		Vector3Axis m_X = Vector3Axis.X;
		[SerializeField]
		Vector3Axis m_Y = Vector3Axis.Y;

		public override Vector2 Convert(Vector3 input)
		{
			return new Vector2(Select(input, m_X), Select(input, m_Y));
		}

		//取り出した軸だけ戻す。参照していない軸は 0 になる。
		//変換前の値は渡ってこないため復元できない
		public override bool TryInverseConvert(Vector2 value, ref Vector3 ret)
		{
			Vector3 result = Vector3.zero;
			Assign(ref result, m_X, value.x);
			Assign(ref result, m_Y, value.y);
			ret = result;
			return true;
		}

		internal static float Select(Vector3 value, Vector3Axis axis)
		{
			switch (axis)
			{
				case Vector3Axis.X: return value.x;
				case Vector3Axis.Y: return value.y;
				case Vector3Axis.Z: return value.z;
				case Vector3Axis.MinusX: return -value.x;
				case Vector3Axis.MinusY: return -value.y;
				case Vector3Axis.MinusZ: return -value.z;
				default: return 0f;
			}
		}

		internal static void Assign(ref Vector3 target, Vector3Axis axis, float value)
		{
			switch (axis)
			{
				case Vector3Axis.X: target.x = value; break;
				case Vector3Axis.Y: target.y = value; break;
				case Vector3Axis.Z: target.z = value; break;
				case Vector3Axis.MinusX: target.x = -value; break;
				case Vector3Axis.MinusY: target.y = -value; break;
				case Vector3Axis.MinusZ: target.z = -value; break;
			}
		}

	}

	[DispName("Core/Vector/Vector2ToVector3")]
	public class Vector2ToVector3 : ConverterBase<Vector2, Vector3>
	{

		[SerializeField]
		Vector2Axis m_X = Vector2Axis.X;
		[SerializeField]
		Vector2Axis m_Y = Vector2Axis.Y;
		[SerializeField]
		Vector2Axis m_Z = Vector2Axis.Zero;

		public override Vector3 Convert(Vector2 input)
		{
			return new Vector3(Select(input, m_X), Select(input, m_Y), Select(input, m_Z));
		}

		public override bool TryInverseConvert(Vector3 value, ref Vector2 ret)
		{
			Vector2 result = Vector2.zero;
			Assign(ref result, m_X, value.x);
			Assign(ref result, m_Y, value.y);
			Assign(ref result, m_Z, value.z);
			ret = result;
			return true;
		}

		static float Select(Vector2 value, Vector2Axis axis)
		{
			switch (axis)
			{
				case Vector2Axis.X: return value.x;
				case Vector2Axis.Y: return value.y;
				case Vector2Axis.MinusX: return -value.x;
				case Vector2Axis.MinusY: return -value.y;
				default: return 0f;
			}
		}

		static void Assign(ref Vector2 target, Vector2Axis axis, float value)
		{
			switch (axis)
			{
				case Vector2Axis.X: target.x = value; break;
				case Vector2Axis.Y: target.y = value; break;
				case Vector2Axis.MinusX: target.x = -value; break;
				case Vector2Axis.MinusY: target.y = -value; break;
			}
		}

	}

}