namespace UVMBinding.Tools
{
	//数値変換の実装を静的レジストリとして生成する。
	//Register<TIn, TOut> の呼び出しがジェネリック実体化への静的参照になるため AOT 安全。
	//型ごとの Converter クラスは生成しない（TASK-022）。
	public static class CastGenerateProcess
	{
		public static void Run()
		{
			CodeEmitter emitter = new();
			emitter.WriteLine("// Generate [UVMBinding/Generate/CastGenerate]");
			emitter.WriteLine("namespace UVMBinding.Converters");
			using (emitter.Bracket())
			{
				emitter.WriteLine("internal static partial class CastRegistry");
				using (emitter.Bracket())
				{
					emitter.WriteLine("static void RegisterGenerated()");
					using (emitter.Bracket())
					{
						foreach (var x in GenerateUtil.Names.Keys)
						{
							foreach (var y in GenerateUtil.Names.Keys)
							{
								if (x == y)
								{
									continue;
								}
								var xName = GenerateUtil.Names[x].ToLower();
								var yName = GenerateUtil.Names[y].ToLower();
								emitter.WriteLine($"Register<{xName}, {yName}>(x => ({yName})x);");
							}
						}
					}
				}
			}
			emitter.Dump("Assets/UVMBinding/Runtime/Converter/Converters/Generated/Cast.Generated.cs");
		}
	}

}
