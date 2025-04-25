using UnityEngine;

namespace UVMBinding.Converters
{
	[DispName("Core/Lerp/Color")]
	public class ColorLerpConverter : ConverterBase<float, Color>
	{
		[SerializeField, ColorUsage(true)]
		Color m_From = new Color(1, 1, 1, 1);
		[SerializeField, ColorUsage(true)]
		Color m_To = new Color(1, 1, 1, 1);

		public override Color Convert(float input)
		{
			return Color.Lerp(m_From, m_To, input);
		}
	}

	[DispName("Core/Lerp/HDRColor")]
	public class HDRColorLerpConverter : ConverterBase<float, Color>
	{
		[SerializeField, ColorUsage(true, true)]
		Color m_From = new Color(1, 1, 1, 1);
		[SerializeField, ColorUsage(true, true)]
		Color m_To = new Color(1, 1, 1, 1);

		public override Color Convert(float input)
		{
			return Color.Lerp(m_From, m_To, input);
		}
	}

	[DispName("Core/Lerp/Vector2")]
	public class Vector2LerpConverter : LerpConverter<Vector2>
	{
		protected override Vector2 LerpUnclamped(Vector2 from, Vector2 to, float t)
		{
			return Vector2.LerpUnclamped(from, to, t);
		}
	}

	[DispName("Core/Lerp/Vector3")]
	public class Vector3LerpConverter : LerpConverter<Vector3>
	{
		protected override Vector3 LerpUnclamped(Vector3 from, Vector3 to, float t)
		{
			return Vector3.LerpUnclamped(from, to, t);
		}
	}


	[DispName("Core/Lerp/Quaternion")]
	public class QuaternionLerpConverter : LerpConverter<Quaternion>
	{
		[SerializeField]
		bool m_UseSlerp;

		protected override Quaternion LerpUnclamped(Quaternion from, Quaternion to, float t)
		{
			if (m_UseSlerp)
			{
				return Quaternion.SlerpUnclamped(from, to, t);
			}
			else
			{
				return Quaternion.LerpUnclamped(from, to, t);
			}
		}
	}

	public abstract class LerpConverter<T> : ConverterBase<float, T>
	{
		[SerializeField]
		T m_From;
		[SerializeField]
		T m_To;
		[SerializeField]
		bool m_Unclamped = false;

		protected T From => m_From;

		protected T To => m_To;

		protected abstract T LerpUnclamped(T from, T to, float t);

		public override T Convert(float input)
		{
			var t = m_Unclamped ? input : Mathf.Clamp01(input);
			return LerpUnclamped(m_From, m_To, t);
		}
	}
}