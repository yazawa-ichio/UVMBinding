namespace UVMBinding.Tools
{
	public static class BoolSwitchProcess
	{
		public static void Run()
		{
			CodeEmitter emitter = new();
			emitter.WriteLine("// Generate [UVMBinding/Generate/BoolSwitchGenerate]");
			emitter.WriteLine("namespace UVMBinding.Converters");
			using (emitter.Bracket())
			{
				foreach (var x in GenerateUtil.Names.Keys)
				{
					emitter.NewLine();
					var typeName = GenerateUtil.Names[x];
					emitter.WriteLine($"[DispName(\"Core/BoolSwitch/{typeName}\")]");
					emitter.WriteLine($"public class BoolSwitch{typeName} : BoolSwitchBase<{typeName.ToLower()}> {{}}");
				}
				emitter.NewLine();
				emitter.WriteLine($"[DispName(\"Core/BoolSwitch/String\")]");
				emitter.WriteLine($"public class BoolSwitchString : BoolSwitchBase<string> {{}}");
				emitter.NewLine();
			}
			emitter.Dump("Assets/UVMBinding/Runtime/Converter/Converters/Generated/BoolSwitch.Generated.cs");
			UnityEngine.Debug.Log(emitter.ToString());
		}
	}

}