using System;
using UnityEngine;

namespace UVMBinding
{
	[Serializable]
	class BindSourceTargetData
	{
		public enum TargetType
		{
			All,
			Selected
		}

		public TargetType Type = TargetType.All;

		public Component[] Targets = Array.Empty<Component>();

		public bool IsTarget(Component component)
		{
			if (Type == TargetType.All) return true;

			foreach (var item in Targets)
			{
				if (item == component) return true;
			}
			return false;
		}
	}
}