namespace UVMBinding.Tools
{
	public static class CastGenerateProcess
	{
		public static void Run()
		{
			CodeEmitter emitter = new();
			emitter.WriteLine("// Generate [UVMBinding/Generate/CastGenerate]");
			emitter.WriteLine("namespace UVMBinding.Converters");
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
						emitter.NewLine();
						var xName = GenerateUtil.Names[x];
						var yName = GenerateUtil.Names[y];
						emitter.WriteLine($"[DispName(\"Core/Cast/{xName}/To{yName}\")]");
						emitter.WriteLine($"public class {xName}To{yName} : CastBase<{xName.ToLower()}, {yName.ToLower()}>");
						using (emitter.Bracket())
						{
							emitter.WriteLine($"public override {yName.ToLower()} Convert({xName.ToLower()} input)");
							using (emitter.Bracket())
							{
								emitter.WriteLine($"return ({yName.ToLower()})input;");
							}
						}
					}
				}
				emitter.NewLine();
			}
			emitter.Dump("Assets/UVMBinding/Runtime/Converter/Converters/Generated/Cast.Generated.cs");
			UnityEngine.Debug.Log(emitter.ToString());
		}
	}

}