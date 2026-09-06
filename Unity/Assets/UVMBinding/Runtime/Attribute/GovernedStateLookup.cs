using System;
using System.Collections.Generic;

namespace UVMBinding
{
	public static class GovernedStateLookup
	{
		static readonly Dictionary<Type, GovernedState> s_Cache = new Dictionary<Type, GovernedState>();

		public static GovernedState Get(Type type)
		{
			if (type == null)
			{
				return GovernedState.None;
			}
			if (s_Cache.TryGetValue(type, out var state))
			{
				return state;
			}
			return s_Cache[type] = Find(type);
		}

		public static GovernedState Get(object element)
		{
			return element != null ? Get(element.GetType()) : GovernedState.None;
		}

		static GovernedState Find(Type type)
		{
			var attributes = type.GetCustomAttributes(typeof(GovernsStateAttribute), true);
			if (attributes.Length == 0)
			{
				return GovernedState.None;
			}
			return ((GovernsStateAttribute)attributes[0]).State;
		}
	}
}
