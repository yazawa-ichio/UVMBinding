using System;
using System.Collections.Generic;
using UVMBinding.Logger;

namespace UVMBinding.Converters
{
	public static partial class CastRegistry
	{
		static Dictionary<(Type, Type), object> s_Delegates;
		static HashSet<(Type, Type)> s_Implicit;

		static Dictionary<(Type, Type), object> Delegates
		{
			get
			{
				if (s_Delegates == null)
				{
					s_Delegates = new Dictionary<(Type, Type), object>();
					s_Implicit = new HashSet<(Type, Type)>();
					RegisterGenerated();
				}
				return s_Delegates;
			}
		}

		public static void Register<TInput, TOutput>(Func<TInput, TOutput> convert)
		{
			Add(convert, false);
		}

		public static void RegisterImplicit<TInput, TOutput>(Func<TInput, TOutput> convert)
		{
			Add(convert, true);
		}

		static void Add<TInput, TOutput>(Func<TInput, TOutput> convert, bool auto)
		{
			if (convert == null)
			{
				throw new ArgumentNullException(nameof(convert));
			}
			var key = (typeof(TInput), typeof(TOutput));
			var delegates = Delegates;
			if (delegates.ContainsKey(key))
			{
				Log.Warning("CastRegistry Overwrite {0} => {1}", typeof(TInput), typeof(TOutput));
			}
			delegates[key] = convert;
			if (auto)
			{
				s_Implicit.Add(key);
			}
			else
			{
				s_Implicit.Remove(key);
			}
		}

		public static bool IsRegistered(Type inputType, Type outputType)
		{
			return Delegates.ContainsKey((inputType, outputType));
		}

		public static bool IsImplicit(Type inputType, Type outputType)
		{
			_ = Delegates;
			return s_Implicit.Contains((inputType, outputType));
		}

		internal static Func<TSrc, TDst> FindImplicit<TSrc, TDst>()
		{
			var key = (typeof(TSrc), typeof(TDst));
			if (!Delegates.TryGetValue(key, out var convert) || !s_Implicit.Contains(key))
			{
				return null;
			}
			return convert as Func<TSrc, TDst>;
		}

		internal static Func<TSrc, TDst> FindExplicit<TSrc, TDst>()
		{
			var key = (typeof(TSrc), typeof(TDst));
			if (!Delegates.TryGetValue(key, out var convert) || s_Implicit.Contains(key))
			{
				return null;
			}
			return convert as Func<TSrc, TDst>;
		}
	}
}
