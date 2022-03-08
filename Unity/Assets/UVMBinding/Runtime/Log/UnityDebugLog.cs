using UnityEngine;

namespace UVMBinding.Logger
{
	public class UnityDebugLog : ILogHandler
	{
		ILogger m_Logger;

		public UnityDebugLog()
		{
			m_Logger = Debug.unityLogger;
		}

		public UnityDebugLog(ILogger logger)
		{
			m_Logger = logger;
		}

		public void Log(LogLevel level, string tag, string message)
		{
			if (m_Logger == null || !m_Logger.logEnabled) return;

			switch (level)
			{
				case LogLevel.Error:
					m_Logger.Log(LogType.Error, $"[{tag}:{level}]", message);
					break;
				case LogLevel.Warning:
					m_Logger.Log(LogType.Warning, $"[{tag}:{level}]", message);
					break;
				default:
					m_Logger.Log(LogType.Log, $"[{tag}:{level}]", message);
					break;
			}
		}

		public void Log(Object context, LogLevel level, string tag, string message)
		{
			if (m_Logger == null || !m_Logger.logEnabled) return;

			switch (level)
			{
				case LogLevel.Error:
					m_Logger.Log(LogType.Error, $"[{tag}:{level}]", message, context);
					break;
				case LogLevel.Warning:
					m_Logger.Log(LogType.Warning, $"[{tag}:{level}]", message, context);
					break;
				default:
					m_Logger.Log(LogType.Log, $"[{tag}:{level}]", message, context);
					break;
			}
		}
	}
}