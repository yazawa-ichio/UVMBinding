namespace UVMBinding.Tools
{
	public static class ComparisonGenerateProcess
	{
		public static void Run()
		{

			CodeEmitter emitter = new CodeEmitter();
			emitter.WriteLine("// Generate [UVMBinding/Generate/ComparisonGenerate]");
			emitter.WriteLine("namespace UVMBinding.Converters");
			using (emitter.Bracket())
			{
				foreach (var type in GenerateUtil.Names.Keys)
				{
					emitter.NewLine();
					var name = GenerateUtil.Names[type];
					emitter.WriteLine($"[DispName(\"Core/Comparison/{name}\")]");
					emitter.WriteLine($"public class {name}Comparison : ComparisonBase<{name.ToLower()}> {{ }}");
				}
				emitter.NewLine();
			}
			emitter.Dump("Assets/UVMBinding/Runtime/Converter/Converters/Generated/Comparison.Generated.cs");
			UnityEngine.Debug.Log(emitter.ToString());
		}
	}

}