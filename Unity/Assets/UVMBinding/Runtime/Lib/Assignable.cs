using System;
using System.Collections.Generic;
using UVMBinding.Logger;

namespace UVMBinding.Core
{
	internal static class Assignable<TSrc, TDst>
	{
		public static readonly bool Is = typeof(TDst).IsAssignableFrom(typeof(TSrc));

		static Assignable()
		{
			Log.Trace("Assignable.Is:{0} Src:{1} > Dst:{2}", Is, typeof(TSrc), typeof(TDst));
		}
	}

	internal static class Assignable
	{
		static Dictionary<Type, Dictionary<Type, bool>> s_Cache = new Dictionary<Type, Dictionary<Type, bool>>();

		static Dictionary<Type, bool> GetSrcDic(Type type)
		{
			if (!s_Cache.TryGetValue(type, out var dict))
			{
				dict = new Dictionary<Type, bool>();
				s_Cache[type] = dict;
			}
			return dict;
		}

		public static bool Is(Type src, Type dst)
		{
			var dict = GetSrcDic(src);
			if (dict.TryGetValue(dst, out var isAssignable))
			{
				return isAssignable;
			}
			return dict[dst] = dst.IsAssignableFrom(src);
		}
	}

	internal static class AssignableNull<TSrc>
	{
		public static readonly bool IsNullable = typeof(TSrc).IsClass || typeof(TSrc).IsValueType && Nullable.GetUnderlyingType(typeof(TSrc)) != null;

		static AssignableNull()
		{
			Log.Trace("Assignable.IsNullable:{0} {1}", IsNullable, typeof(TSrc));
		}
	}
}