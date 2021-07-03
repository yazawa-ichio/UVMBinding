using System;
using System.IO;
using System.Text;

namespace UVMBinding.Tools
{
	public class CodeEmitter
	{
		class Scope : IDisposable
		{
			Action m_Action;
			public Scope(Action action) => m_Action = action;
			public void Dispose() => m_Action?.Invoke();
		}

		StringBuilder m_Writer = new StringBuilder();
		int m_Indent = 0;

		public void NewLine()
		{
			m_Writer.Append("\n");
		}

		public void AppendTab()
		{
			for (int i = 0; i < m_Indent; i++)
			{
				m_Writer.Append('\t');
			}
		}

		public void Append(string line)
		{
			m_Writer.Append(line);
		}

		public void AppendLine(string line)
		{
			m_Writer.Append(line + "\n");
		}

		public void WriteLine(string line)
		{
			AppendTab();
			m_Writer.Append(line + "\n");
		}

		public void WriteLine(string line, params object[] args)
		{
			AppendTab();
			m_Writer.AppendFormat(line, args);
		}

		public void Comment(string message)
		{
			Comment(message.Split('\n'));
		}

		public void Comment(string[] messages)
		{
			foreach (var message in messages)
			{
				WriteLine("// " + message);
			}
		}

		public void ShortSummary(string message)
		{
			ShortSummary(message.Split('\n'));
		}

		public void ShortSummary(string[] messages)
		{
			foreach (var message in messages)
			{
				WriteLine("/// " + message);
			}
		}

		public void DetailSummary(string message)
		{
			DetailSummary(message.Split('\n'));
		}

		public void DetailSummary(string[] messages)
		{
			WriteLine("/// <summary>");
			ShortSummary(messages);
			WriteLine("/// </summary>");
		}

		public void BeginIndent()
		{
			m_Indent++;
		}

		public void EndIndent()
		{
			m_Indent--;
		}

		public IDisposable Indent()
		{
			BeginIndent();
			return new Scope(EndIndent);
		}

		public void BeginBracket()
		{
			WriteLine("{");
			m_Indent++;
		}

		public void EndBracket(string append = "")
		{
			m_Indent--;
			WriteLine("}" + append);
		}

		public IDisposable Bracket(string append = "")
		{
			BeginBracket();
			return new Scope(() => EndBracket(append));
		}

		public void Dump(string path)
		{
			string dir = Path.GetDirectoryName(path);
			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}
			File.WriteAllBytes(path, Encoding.UTF8.GetBytes(m_Writer.ToString()));
		}

		public void Clear()
		{
			m_Writer.Clear();
		}

		public override string ToString()
		{
			return m_Writer.ToString();
		}

	}

}