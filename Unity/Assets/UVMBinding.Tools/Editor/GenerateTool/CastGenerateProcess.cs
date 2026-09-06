using System;
using System.Collections.Generic;

namespace UVMBinding.Tools
{
	public static class CastGenerateProcess
	{
		static readonly Dictionary<Type, (decimal Min, decimal Max, int Bits)> s_Integers = new()
		{
			{ typeof(byte), (byte.MinValue, byte.MaxValue, 8) },
			{ typeof(ushort), (ushort.MinValue, ushort.MaxValue, 16) },
			{ typeof(uint), (uint.MinValue, uint.MaxValue, 32) },
			{ typeof(ulong), (ulong.MinValue, ulong.MaxValue, 64) },
			{ typeof(sbyte), (sbyte.MinValue, sbyte.MaxValue, 8) },
			{ typeof(short), (short.MinValue, short.MaxValue, 16) },
			{ typeof(int), (int.MinValue, int.MaxValue, 32) },
			{ typeof(long), (long.MinValue, long.MaxValue, 64) },
		};

		static int Mantissa(Type type) => type == typeof(float) ? 24 : 53;

		static bool IsImplicit(Type from, Type to)
		{
			if (from == to)
			{
				return true;
			}
			bool fromInteger = s_Integers.ContainsKey(from);
			bool toInteger = s_Integers.ContainsKey(to);
			if (!fromInteger)
			{
				return !toInteger && from == typeof(float);
			}
			if (!toInteger)
			{
				return s_Integers[from].Bits <= Mantissa(to);
			}
			return s_Integers[to].Min <= s_Integers[from].Min && s_Integers[from].Max <= s_Integers[to].Max;
		}

		public static void Run()
		{
			CodeEmitter emitter = new();
			emitter.WriteLine("// Generate [UVMBinding/Generate/CastGenerate]");
			emitter.WriteLine("namespace UVMBinding.Converters");
			using (emitter.Bracket())
			{
				emitter.WriteLine("public static partial class CastRegistry");
				using (emitter.Bracket())
				{
					emitter.WriteLine("static void RegisterGenerated()");
					using (emitter.Bracket())
					{
						foreach (var x in GenerateUtil.Names.Keys)
						{
							foreach (var y in GenerateUtil.Names.Keys)
							{
								var xName = GenerateUtil.Names[x].ToLower();
								var yName = GenerateUtil.Names[y].ToLower();
								var register = IsImplicit(x, y) ? "RegisterImplicit" : "Register";
								if (x == y)
								{
									emitter.WriteLine($"{register}<{xName}, {xName}>(x => x);");
									continue;
								}
								emitter.WriteLine($"{register}<{xName}, {yName}>(x => ({yName})x);");
							}
						}
					}
				}
			}
			emitter.Dump("Assets/UVMBinding/Runtime/Converter/Converters/Generated/Cast.Generated.cs");
		}
	}

}
