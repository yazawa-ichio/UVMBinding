using System;
using UVMBinding.Converters;

namespace UVMBinding.Core
{
	internal static class NumericCast<TSrc, TDst>
	{
		public static readonly Func<TSrc, TDst> Convert = CastRegistry.FindImplicit<TSrc, TDst>();

		public static readonly bool Is = Convert != null;
	}

	internal static class ExplicitCast<TSrc, TDst>
	{
		public static readonly Func<TSrc, TDst> Convert = CastRegistry.FindExplicit<TSrc, TDst>();

		public static readonly bool Is = Convert != null;
	}
}
