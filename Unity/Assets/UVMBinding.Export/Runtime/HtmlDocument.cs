using System.Collections.Generic;
using System.Text;

#if UNITY_EDITOR || UVMBINDING_EXPORT
namespace UVMBinding.Semantics.Export
{
	public static class HtmlDocument
	{
		public static string Wrap(string title, IReadOnlyList<string> parts)
		{
			var sb = new StringBuilder();
			sb.Append("<!doctype html>\n<html lang=\"ja\">\n<head>\n");
			sb.Append("<meta charset=\"utf-8\">\n");
			sb.Append("<title>").Append(Escape(title)).Append("</title>\n");
			sb.Append("</head>\n<body>\n");
			if (parts != null)
			{
				foreach (var part in parts)
				{
					if (string.IsNullOrEmpty(part))
					{
						continue;
					}
					sb.Append(part).Append('\n');
				}
			}
			sb.Append("</body>\n</html>\n");
			return sb.ToString();
		}

		static string Escape(string value)
		{
			return (value ?? string.Empty).Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
		}
	}
}

#endif
