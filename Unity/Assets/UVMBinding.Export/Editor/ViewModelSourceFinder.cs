using System.Collections.Generic;
using System.IO;
using UnityEditor.Compilation;

namespace UVMBinding.Export
{
	public static class ViewModelSourceFinder
	{
		static readonly Dictionary<System.Type, Result> s_Cache = new Dictionary<System.Type, Result>();

		public readonly struct Result
		{
			public readonly string Path;
			public readonly string Text;

			public Result(string path, string text)
			{
				Path = path;
				Text = text;
			}
		}

		public static Result Find(System.Type type)
		{
			if (type == null)
			{
				return default;
			}
			if (s_Cache.TryGetValue(type, out var cached))
			{
				return cached;
			}
			return s_Cache[type] = Search(type);
		}

		static Result Search(System.Type type)
		{
			var assemblyName = type.Assembly.GetName().Name;
			foreach (var assembly in CompilationPipeline.GetAssemblies())
			{
				if (assembly.name != assemblyName)
				{
					continue;
				}
				foreach (var file in assembly.sourceFiles)
				{
					if (!File.Exists(file))
					{
						continue;
					}
					var text = File.ReadAllText(file);
					var body = Extract(text, type.Name);
					if (body != null)
					{
						return new Result(file.Replace(Path.DirectorySeparatorChar, '/'), body);
					}
				}
			}
			return default;
		}

		public static string Extract(string text, string typeName)
		{
			var index = FindDeclaration(text, typeName);
			if (index < 0)
			{
				return null;
			}
			var open = text.IndexOf('{', index);
			if (open < 0)
			{
				return null;
			}
			var close = MatchBrace(text, open);
			if (close < 0)
			{
				return null;
			}
			var start = LineStart(text, index);
			start = IncludeLeading(text, start);
			return text.Substring(start, close - start + 1).TrimEnd();
		}

		static int FindDeclaration(string text, string typeName)
		{
			foreach (var keyword in new[] { "class ", "struct ", "interface ", "record " })
			{
				var needle = keyword + typeName;
				var from = 0;
				while (true)
				{
					var index = text.IndexOf(needle, from, System.StringComparison.Ordinal);
					if (index < 0)
					{
						break;
					}
					var after = index + needle.Length;
					if (after >= text.Length || !IsNameChar(text[after]))
					{
						return index;
					}
					from = index + 1;
				}
			}
			return -1;
		}

		static bool IsNameChar(char c) => char.IsLetterOrDigit(c) || c == '_';

		static int MatchBrace(string text, int open)
		{
			var depth = 0;
			for (int i = open; i < text.Length; i++)
			{
				var c = text[i];
				if (c == '/' && i + 1 < text.Length && text[i + 1] == '/')
				{
					i = SkipTo(text, i, '\n');
					continue;
				}
				if (c == '"' || c == '\'')
				{
					i = SkipLiteral(text, i, c);
					continue;
				}
				if (c == '{')
				{
					depth++;
				}
				else if (c == '}')
				{
					depth--;
					if (depth == 0)
					{
						return i;
					}
				}
			}
			return -1;
		}

		static int SkipTo(string text, int from, char stop)
		{
			var index = text.IndexOf(stop, from);
			return index < 0 ? text.Length : index;
		}

		const char BackSlash = (char)92;

		static int SkipLiteral(string text, int from, char quote)
		{
			for (int i = from + 1; i < text.Length; i++)
			{
				if (text[i] == BackSlash)
				{
					i++;
					continue;
				}
				if (text[i] == quote)
				{
					return i;
				}
				if (text[i] == '\n')
				{
					return i;
				}
			}
			return text.Length;
		}

		static int LineStart(string text, int index)
		{
			var start = text.LastIndexOf('\n', index);
			return start < 0 ? 0 : start + 1;
		}

		static int IncludeLeading(string text, int start)
		{
			var current = start;
			while (current > 0)
			{
				var previousEnd = current - 1;
				var previousStart = LineStart(text, previousEnd - 1);
				if (previousStart >= previousEnd)
				{
					break;
				}
				var line = text.Substring(previousStart, previousEnd - previousStart).Trim();
				if (line.Length == 0 || !(line.StartsWith("//") || line.StartsWith("[")))
				{
					break;
				}
				current = previousStart;
			}
			return current;
		}
	}
}
