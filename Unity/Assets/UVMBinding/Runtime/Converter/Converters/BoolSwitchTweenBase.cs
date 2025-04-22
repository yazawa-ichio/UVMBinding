using System.Collections.Generic;
using UnityEngine;

namespace UVMBinding.Converters
{
	[DispName("Core/BoolSwitchTween/Float")]
	public class BoolSwitchTweenFloat : BoolSwitchTweenBase<float>
	{
		protected override float Leap(float from, float to, float rate)
		{
			return Mathf.Lerp(from, to, rate);
		}
	}

	[DispName("Core/BoolSwitchTween/Vector2")]
	public class BoolSwitchTweenVector2 : BoolSwitchTweenBase<Vector2>
	{
		protected override Vector2 Leap(Vector2 from, Vector2 to, float rate)
		{
			return Vector2.Lerp(from, to, rate);
		}
	}

	[DispName("Core/BoolSwitchTween/Vector3")]
	public class BoolSwitchTweenVector3 : BoolSwitchTweenBase<Vector3>
	{
		protected override Vector3 Leap(Vector3 from, Vector3 to, float rate)
		{
			return Vector3.Lerp(from, to, rate);
		}
	}

	[DispName("Core/BoolSwitchTween/Color")]
	public class BoolSwitchTweenColor : BoolSwitchTweenBase<Color>
	{
		protected override Color Leap(Color from, Color to, float rate)
		{
			return Color.Lerp(from, to, rate);
		}
	}


	public abstract class BoolSwitchTweenBase<T> : ConverterBase<bool, T>
	{
		static EqualityComparer<T> s_EqualityComparer = EqualityComparer<T>.Default;

		[SerializeField]
		T m_On;
		[SerializeField]
		T m_Off;
		[SerializeField]
		float m_TweenTime = 0.2f;

		[SerializeField]
		AnimationCurve m_OnCurve = AnimationCurve.Linear(0, 0, 1, 1);
		[SerializeField]
		AnimationCurve m_OffCurve = AnimationCurve.Linear(0, 0, 1, 1);
		[SerializeField]
		bool m_Realtime = true;

		bool m_Current;
		float m_Time;
		float m_TargetTime;

		protected abstract T Leap(T from, T to, float rate);

		public override T Convert(bool input)
		{
			if (input != m_Current)
			{
				m_Current = input;
				m_Time = m_Realtime ? Time.unscaledTime : Time.time;
				m_TargetTime = m_Time + m_TweenTime;
			}
			var curve = m_Current ? m_OnCurve : m_OffCurve;
			var time = m_Realtime ? Time.unscaledTime : Time.time;
			var rate = Mathf.Clamp01((time - m_Time) / m_TweenTime);
			if (rate >= 1)
			{
				return input ? m_On : m_Off;
			}
			SetDirty();
			rate = curve.Evaluate(rate);
			if (input)
			{
				return Leap(m_Off, m_On, rate);
			}
			else
			{
				return Leap(m_On, m_Off, rate);
			}
		}
	}

}