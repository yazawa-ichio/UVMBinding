using System;
using System.Collections.Generic;

namespace UVMBinding
{
	public static class SemanticRoleLookup
	{
		static readonly Dictionary<Type, SemanticRole> s_Cache = new Dictionary<Type, SemanticRole>();

		public static SemanticRole Get(Type type)
		{
			if (type == null)
			{
				return SemanticRole.None;
			}
			if (s_Cache.TryGetValue(type, out var role))
			{
				return role;
			}
			return s_Cache[type] = Find(type);
		}

		public static SemanticRole Get(object element)
		{
			return element != null ? Get(element.GetType()) : SemanticRole.None;
		}

		static SemanticRole Find(Type type)
		{
			var attributes = type.GetCustomAttributes(typeof(SemanticRoleAttribute), true);
			if (attributes.Length == 0)
			{
				return SemanticRole.None;
			}
			return ((SemanticRoleAttribute)attributes[0]).Role;
		}
	}
}
