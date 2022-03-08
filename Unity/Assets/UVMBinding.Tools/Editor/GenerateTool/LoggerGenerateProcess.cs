namespace UVMBinding.Tools
{
	public class LoggerGenerateProcess
	{
		public static void Run()
		{
			CodeEmitter emitter = new CodeEmitter();
			emitter.WriteLine("// Generate [UVMBinding/Generate/LoggerGenerate]");
			emitter.WriteLine("namespace UVMBinding.Logger");
			using (emitter.Bracket())
			{
				emitter.WriteLine("public static partial class Log");
				using (emitter.Bracket())
				{
					var levelList = new string[] { "Trace", "Debug", "Info", "Warning", "Error" };
					for (int i = 0; i < levelList.Length; i++)
					{
						Method(emitter, levelList, i);
					}
				}
			}
			emitter.Dump("Assets/UVMBinding/Runtime/Log/Log.Generated.cs");
			UnityEngine.Debug.Log(emitter.ToString());
		}


		static void Conditional(CodeEmitter emitter, string[] levelList, int index)
		{
			emitter.WriteLine("[System.Diagnostics.Conditional(\"UVMB_LOG_ALL\")]");
			for (int i = index; i >= 0; i--)
			{
				var level = levelList[i];
				emitter.WriteLine($"[System.Diagnostics.Conditional(\"UVMB_LOG_{level.ToUpper()}_OR_HIGHER\")]");
			}
		}

		static void Method(CodeEmitter emitter, string[] levelList, int index)
		{
			var level = levelList[index];
			emitter.NewLine();
			Conditional(emitter, levelList, index);
			emitter.WriteLine($"public static void {level}(string message)");
			using (emitter.Bracket())
			{
				emitter.WriteLine($"if (Level < LogLevel.{level}) return;");
				emitter.WriteLine($"Handler?.Log(LogLevel.{level}, Tag, message);");
			}

			emitter.NewLine();
			Conditional(emitter, levelList, index);
			emitter.WriteLine($"public static void {level}(UnityEngine.Object context, string message)");
			using (emitter.Bracket())
			{
				emitter.WriteLine($"if (Level < LogLevel.{level}) return;");
				emitter.WriteLine($"Handler?.Log(context, LogLevel.{level}, Tag, message);");
			}

			for (int i = 1; i < 6; i++)
			{
				string[] types = new string[i];
				string[] prms = new string[i];
				string[] args = new string[i];
				for (int j = 0; j < i; j++)
				{
					types[j] = "T" + (j + 1);
					prms[j] = "prm" + (j + 1);
					args[j] = types[j] + " " + prms[j];
				}
				string type = string.Join(", ", types);
				string arg = string.Join(", ", args);
				string prm = string.Join(", ", prms);

				emitter.NewLine();
				Conditional(emitter, levelList, index);
				emitter.WriteLine($"public static void {level}<{type}>(string message, {arg})");
				using (emitter.Bracket())
				{
					emitter.WriteLine($"if (Level < LogLevel.{level}) return;");
					emitter.WriteLine($"Handler?.Log(LogLevel.{level}, Tag, string.Format(message, {prm}));");
				}

				emitter.NewLine();
				Conditional(emitter, levelList, index);
				emitter.WriteLine($"public static void {level}<{type}>(UnityEngine.Object context, string message, {arg})");
				using (emitter.Bracket())
				{
					emitter.WriteLine($"if (Level < LogLevel.{level}) return;");
					emitter.WriteLine($"Handler?.Log(context, LogLevel.{level}, Tag, string.Format(message, {prm}));");
				}
			}

		}
	}

}