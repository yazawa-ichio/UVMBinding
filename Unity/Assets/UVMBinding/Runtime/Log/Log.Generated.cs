// Generate [UVMBinding/Generate/LoggerGenerate]
namespace UVMBinding.Logger
{
	public static partial class Log
	{

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Trace(string message)
		{
			if (Level < LogLevel.Trace) return;
			Handler?.Log(LogLevel.Trace, Tag, message);
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Trace(UnityEngine.Object context, string message)
		{
			if (Level < LogLevel.Trace) return;
			Handler?.Log(context, LogLevel.Trace, Tag, message);
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Trace<T1>(string message, T1 prm1)
		{
			if (Level < LogLevel.Trace) return;
			Handler?.Log(LogLevel.Trace, Tag, string.Format(message, prm1));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Trace<T1>(UnityEngine.Object context, string message, T1 prm1)
		{
			if (Level < LogLevel.Trace) return;
			Handler?.Log(context, LogLevel.Trace, Tag, string.Format(message, prm1));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Trace<T1, T2>(string message, T1 prm1, T2 prm2)
		{
			if (Level < LogLevel.Trace) return;
			Handler?.Log(LogLevel.Trace, Tag, string.Format(message, prm1, prm2));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Trace<T1, T2>(UnityEngine.Object context, string message, T1 prm1, T2 prm2)
		{
			if (Level < LogLevel.Trace) return;
			Handler?.Log(context, LogLevel.Trace, Tag, string.Format(message, prm1, prm2));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Trace<T1, T2, T3>(string message, T1 prm1, T2 prm2, T3 prm3)
		{
			if (Level < LogLevel.Trace) return;
			Handler?.Log(LogLevel.Trace, Tag, string.Format(message, prm1, prm2, prm3));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Trace<T1, T2, T3>(UnityEngine.Object context, string message, T1 prm1, T2 prm2, T3 prm3)
		{
			if (Level < LogLevel.Trace) return;
			Handler?.Log(context, LogLevel.Trace, Tag, string.Format(message, prm1, prm2, prm3));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Trace<T1, T2, T3, T4>(string message, T1 prm1, T2 prm2, T3 prm3, T4 prm4)
		{
			if (Level < LogLevel.Trace) return;
			Handler?.Log(LogLevel.Trace, Tag, string.Format(message, prm1, prm2, prm3, prm4));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Trace<T1, T2, T3, T4>(UnityEngine.Object context, string message, T1 prm1, T2 prm2, T3 prm3, T4 prm4)
		{
			if (Level < LogLevel.Trace) return;
			Handler?.Log(context, LogLevel.Trace, Tag, string.Format(message, prm1, prm2, prm3, prm4));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Trace<T1, T2, T3, T4, T5>(string message, T1 prm1, T2 prm2, T3 prm3, T4 prm4, T5 prm5)
		{
			if (Level < LogLevel.Trace) return;
			Handler?.Log(LogLevel.Trace, Tag, string.Format(message, prm1, prm2, prm3, prm4, prm5));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Trace<T1, T2, T3, T4, T5>(UnityEngine.Object context, string message, T1 prm1, T2 prm2, T3 prm3, T4 prm4, T5 prm5)
		{
			if (Level < LogLevel.Trace) return;
			Handler?.Log(context, LogLevel.Trace, Tag, string.Format(message, prm1, prm2, prm3, prm4, prm5));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Debug(string message)
		{
			if (Level < LogLevel.Debug) return;
			Handler?.Log(LogLevel.Debug, Tag, message);
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Debug(UnityEngine.Object context, string message)
		{
			if (Level < LogLevel.Debug) return;
			Handler?.Log(context, LogLevel.Debug, Tag, message);
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Debug<T1>(string message, T1 prm1)
		{
			if (Level < LogLevel.Debug) return;
			Handler?.Log(LogLevel.Debug, Tag, string.Format(message, prm1));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Debug<T1>(UnityEngine.Object context, string message, T1 prm1)
		{
			if (Level < LogLevel.Debug) return;
			Handler?.Log(context, LogLevel.Debug, Tag, string.Format(message, prm1));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Debug<T1, T2>(string message, T1 prm1, T2 prm2)
		{
			if (Level < LogLevel.Debug) return;
			Handler?.Log(LogLevel.Debug, Tag, string.Format(message, prm1, prm2));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Debug<T1, T2>(UnityEngine.Object context, string message, T1 prm1, T2 prm2)
		{
			if (Level < LogLevel.Debug) return;
			Handler?.Log(context, LogLevel.Debug, Tag, string.Format(message, prm1, prm2));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Debug<T1, T2, T3>(string message, T1 prm1, T2 prm2, T3 prm3)
		{
			if (Level < LogLevel.Debug) return;
			Handler?.Log(LogLevel.Debug, Tag, string.Format(message, prm1, prm2, prm3));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Debug<T1, T2, T3>(UnityEngine.Object context, string message, T1 prm1, T2 prm2, T3 prm3)
		{
			if (Level < LogLevel.Debug) return;
			Handler?.Log(context, LogLevel.Debug, Tag, string.Format(message, prm1, prm2, prm3));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Debug<T1, T2, T3, T4>(string message, T1 prm1, T2 prm2, T3 prm3, T4 prm4)
		{
			if (Level < LogLevel.Debug) return;
			Handler?.Log(LogLevel.Debug, Tag, string.Format(message, prm1, prm2, prm3, prm4));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Debug<T1, T2, T3, T4>(UnityEngine.Object context, string message, T1 prm1, T2 prm2, T3 prm3, T4 prm4)
		{
			if (Level < LogLevel.Debug) return;
			Handler?.Log(context, LogLevel.Debug, Tag, string.Format(message, prm1, prm2, prm3, prm4));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Debug<T1, T2, T3, T4, T5>(string message, T1 prm1, T2 prm2, T3 prm3, T4 prm4, T5 prm5)
		{
			if (Level < LogLevel.Debug) return;
			Handler?.Log(LogLevel.Debug, Tag, string.Format(message, prm1, prm2, prm3, prm4, prm5));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Debug<T1, T2, T3, T4, T5>(UnityEngine.Object context, string message, T1 prm1, T2 prm2, T3 prm3, T4 prm4, T5 prm5)
		{
			if (Level < LogLevel.Debug) return;
			Handler?.Log(context, LogLevel.Debug, Tag, string.Format(message, prm1, prm2, prm3, prm4, prm5));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_INFO_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Info(string message)
		{
			if (Level < LogLevel.Info) return;
			Handler?.Log(LogLevel.Info, Tag, message);
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_INFO_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Info(UnityEngine.Object context, string message)
		{
			if (Level < LogLevel.Info) return;
			Handler?.Log(context, LogLevel.Info, Tag, message);
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_INFO_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Info<T1>(string message, T1 prm1)
		{
			if (Level < LogLevel.Info) return;
			Handler?.Log(LogLevel.Info, Tag, string.Format(message, prm1));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_INFO_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Info<T1>(UnityEngine.Object context, string message, T1 prm1)
		{
			if (Level < LogLevel.Info) return;
			Handler?.Log(context, LogLevel.Info, Tag, string.Format(message, prm1));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_INFO_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Info<T1, T2>(string message, T1 prm1, T2 prm2)
		{
			if (Level < LogLevel.Info) return;
			Handler?.Log(LogLevel.Info, Tag, string.Format(message, prm1, prm2));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_INFO_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Info<T1, T2>(UnityEngine.Object context, string message, T1 prm1, T2 prm2)
		{
			if (Level < LogLevel.Info) return;
			Handler?.Log(context, LogLevel.Info, Tag, string.Format(message, prm1, prm2));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_INFO_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Info<T1, T2, T3>(string message, T1 prm1, T2 prm2, T3 prm3)
		{
			if (Level < LogLevel.Info) return;
			Handler?.Log(LogLevel.Info, Tag, string.Format(message, prm1, prm2, prm3));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_INFO_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Info<T1, T2, T3>(UnityEngine.Object context, string message, T1 prm1, T2 prm2, T3 prm3)
		{
			if (Level < LogLevel.Info) return;
			Handler?.Log(context, LogLevel.Info, Tag, string.Format(message, prm1, prm2, prm3));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_INFO_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Info<T1, T2, T3, T4>(string message, T1 prm1, T2 prm2, T3 prm3, T4 prm4)
		{
			if (Level < LogLevel.Info) return;
			Handler?.Log(LogLevel.Info, Tag, string.Format(message, prm1, prm2, prm3, prm4));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_INFO_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Info<T1, T2, T3, T4>(UnityEngine.Object context, string message, T1 prm1, T2 prm2, T3 prm3, T4 prm4)
		{
			if (Level < LogLevel.Info) return;
			Handler?.Log(context, LogLevel.Info, Tag, string.Format(message, prm1, prm2, prm3, prm4));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_INFO_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Info<T1, T2, T3, T4, T5>(string message, T1 prm1, T2 prm2, T3 prm3, T4 prm4, T5 prm5)
		{
			if (Level < LogLevel.Info) return;
			Handler?.Log(LogLevel.Info, Tag, string.Format(message, prm1, prm2, prm3, prm4, prm5));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_INFO_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Info<T1, T2, T3, T4, T5>(UnityEngine.Object context, string message, T1 prm1, T2 prm2, T3 prm3, T4 prm4, T5 prm5)
		{
			if (Level < LogLevel.Info) return;
			Handler?.Log(context, LogLevel.Info, Tag, string.Format(message, prm1, prm2, prm3, prm4, prm5));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_WARNING_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_INFO_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Warning(string message)
		{
			if (Level < LogLevel.Warning) return;
			Handler?.Log(LogLevel.Warning, Tag, message);
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_WARNING_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_INFO_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Warning(UnityEngine.Object context, string message)
		{
			if (Level < LogLevel.Warning) return;
			Handler?.Log(context, LogLevel.Warning, Tag, message);
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_WARNING_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_INFO_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Warning<T1>(string message, T1 prm1)
		{
			if (Level < LogLevel.Warning) return;
			Handler?.Log(LogLevel.Warning, Tag, string.Format(message, prm1));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_WARNING_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_INFO_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Warning<T1>(UnityEngine.Object context, string message, T1 prm1)
		{
			if (Level < LogLevel.Warning) return;
			Handler?.Log(context, LogLevel.Warning, Tag, string.Format(message, prm1));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_WARNING_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_INFO_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Warning<T1, T2>(string message, T1 prm1, T2 prm2)
		{
			if (Level < LogLevel.Warning) return;
			Handler?.Log(LogLevel.Warning, Tag, string.Format(message, prm1, prm2));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_WARNING_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_INFO_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Warning<T1, T2>(UnityEngine.Object context, string message, T1 prm1, T2 prm2)
		{
			if (Level < LogLevel.Warning) return;
			Handler?.Log(context, LogLevel.Warning, Tag, string.Format(message, prm1, prm2));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_WARNING_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_INFO_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Warning<T1, T2, T3>(string message, T1 prm1, T2 prm2, T3 prm3)
		{
			if (Level < LogLevel.Warning) return;
			Handler?.Log(LogLevel.Warning, Tag, string.Format(message, prm1, prm2, prm3));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_WARNING_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_INFO_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Warning<T1, T2, T3>(UnityEngine.Object context, string message, T1 prm1, T2 prm2, T3 prm3)
		{
			if (Level < LogLevel.Warning) return;
			Handler?.Log(context, LogLevel.Warning, Tag, string.Format(message, prm1, prm2, prm3));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_WARNING_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_INFO_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Warning<T1, T2, T3, T4>(string message, T1 prm1, T2 prm2, T3 prm3, T4 prm4)
		{
			if (Level < LogLevel.Warning) return;
			Handler?.Log(LogLevel.Warning, Tag, string.Format(message, prm1, prm2, prm3, prm4));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_WARNING_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_INFO_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Warning<T1, T2, T3, T4>(UnityEngine.Object context, string message, T1 prm1, T2 prm2, T3 prm3, T4 prm4)
		{
			if (Level < LogLevel.Warning) return;
			Handler?.Log(context, LogLevel.Warning, Tag, string.Format(message, prm1, prm2, prm3, prm4));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_WARNING_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_INFO_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Warning<T1, T2, T3, T4, T5>(string message, T1 prm1, T2 prm2, T3 prm3, T4 prm4, T5 prm5)
		{
			if (Level < LogLevel.Warning) return;
			Handler?.Log(LogLevel.Warning, Tag, string.Format(message, prm1, prm2, prm3, prm4, prm5));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_WARNING_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_INFO_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Warning<T1, T2, T3, T4, T5>(UnityEngine.Object context, string message, T1 prm1, T2 prm2, T3 prm3, T4 prm4, T5 prm5)
		{
			if (Level < LogLevel.Warning) return;
			Handler?.Log(context, LogLevel.Warning, Tag, string.Format(message, prm1, prm2, prm3, prm4, prm5));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_ERROR_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_WARNING_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_INFO_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Error(string message)
		{
			if (Level < LogLevel.Error) return;
			Handler?.Log(LogLevel.Error, Tag, message);
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_ERROR_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_WARNING_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_INFO_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Error(UnityEngine.Object context, string message)
		{
			if (Level < LogLevel.Error) return;
			Handler?.Log(context, LogLevel.Error, Tag, message);
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_ERROR_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_WARNING_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_INFO_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Error<T1>(string message, T1 prm1)
		{
			if (Level < LogLevel.Error) return;
			Handler?.Log(LogLevel.Error, Tag, string.Format(message, prm1));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_ERROR_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_WARNING_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_INFO_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Error<T1>(UnityEngine.Object context, string message, T1 prm1)
		{
			if (Level < LogLevel.Error) return;
			Handler?.Log(context, LogLevel.Error, Tag, string.Format(message, prm1));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_ERROR_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_WARNING_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_INFO_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Error<T1, T2>(string message, T1 prm1, T2 prm2)
		{
			if (Level < LogLevel.Error) return;
			Handler?.Log(LogLevel.Error, Tag, string.Format(message, prm1, prm2));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_ERROR_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_WARNING_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_INFO_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Error<T1, T2>(UnityEngine.Object context, string message, T1 prm1, T2 prm2)
		{
			if (Level < LogLevel.Error) return;
			Handler?.Log(context, LogLevel.Error, Tag, string.Format(message, prm1, prm2));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_ERROR_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_WARNING_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_INFO_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Error<T1, T2, T3>(string message, T1 prm1, T2 prm2, T3 prm3)
		{
			if (Level < LogLevel.Error) return;
			Handler?.Log(LogLevel.Error, Tag, string.Format(message, prm1, prm2, prm3));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_ERROR_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_WARNING_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_INFO_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Error<T1, T2, T3>(UnityEngine.Object context, string message, T1 prm1, T2 prm2, T3 prm3)
		{
			if (Level < LogLevel.Error) return;
			Handler?.Log(context, LogLevel.Error, Tag, string.Format(message, prm1, prm2, prm3));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_ERROR_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_WARNING_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_INFO_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Error<T1, T2, T3, T4>(string message, T1 prm1, T2 prm2, T3 prm3, T4 prm4)
		{
			if (Level < LogLevel.Error) return;
			Handler?.Log(LogLevel.Error, Tag, string.Format(message, prm1, prm2, prm3, prm4));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_ERROR_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_WARNING_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_INFO_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Error<T1, T2, T3, T4>(UnityEngine.Object context, string message, T1 prm1, T2 prm2, T3 prm3, T4 prm4)
		{
			if (Level < LogLevel.Error) return;
			Handler?.Log(context, LogLevel.Error, Tag, string.Format(message, prm1, prm2, prm3, prm4));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_ERROR_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_WARNING_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_INFO_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Error<T1, T2, T3, T4, T5>(string message, T1 prm1, T2 prm2, T3 prm3, T4 prm4, T5 prm5)
		{
			if (Level < LogLevel.Error) return;
			Handler?.Log(LogLevel.Error, Tag, string.Format(message, prm1, prm2, prm3, prm4, prm5));
		}

		[System.Diagnostics.Conditional("UVMB_LOG_ALL")]
		[System.Diagnostics.Conditional("UVMB_LOG_ERROR_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_WARNING_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_INFO_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_DEBUG_OR_HIGHER")]
		[System.Diagnostics.Conditional("UVMB_LOG_TRACE_OR_HIGHER")]
		public static void Error<T1, T2, T3, T4, T5>(UnityEngine.Object context, string message, T1 prm1, T2 prm2, T3 prm3, T4 prm4, T5 prm5)
		{
			if (Level < LogLevel.Error) return;
			Handler?.Log(context, LogLevel.Error, Tag, string.Format(message, prm1, prm2, prm3, prm4, prm5));
		}
	}
}
