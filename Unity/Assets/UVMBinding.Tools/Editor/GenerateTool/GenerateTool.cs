using UnityEditor;

namespace UVMBinding.Tools
{

	public static class GenerateTool
	{
		[MenuItem("UVMBinding/Generate/ComparisonGenerate")]
		static void ComparisonGenerate()
		{
			ComparisonGenerateProcess.Run();
		}

		[MenuItem("UVMBinding/Generate/CastGenerate")]
		static void CastGenerate()
		{
			CastGenerateProcess.Run();
		}

		[MenuItem("UVMBinding/Generate/LoggerGenerate")]
		static void LoggerGenerate()
		{
			LoggerGenerateProcess.Run();
		}
	}

}