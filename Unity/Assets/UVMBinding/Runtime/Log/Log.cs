namespace UVMBinding.Logger
{
	public static partial class Log
	{
		public static string Tag { get; set; } = "UVMBinding";

		public static ILogHandler Handler { get; set; } = new UnityDebugLog();

#if DEBUG
		public static LogLevel Level { get; set; } = LogLevel.Debug;
#else
		public static LogLevel Level { get; set; } = LogLevel.Warning;
#endif

	}
}