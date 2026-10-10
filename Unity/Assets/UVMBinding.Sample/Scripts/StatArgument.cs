using UnityEngine;
using UVMBinding.Arguments;

namespace UVMBinding.Sample
{
	public class StatArgument : EventArgument<int>
	{
		[SerializeField]
		GrowthStat m_Stat = default;

		public override int GetValue() => (int)m_Stat;
	}

	public enum GrowthStat
	{
		Strength,
		Agility,
		Vitality,
	}
}
