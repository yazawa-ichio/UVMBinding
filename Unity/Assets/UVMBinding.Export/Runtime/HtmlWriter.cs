using System.Collections.Generic;
using System.Text;

#if UNITY_EDITOR || UVMBINDING_EXPORT
namespace UVMBinding.Semantics.Export
{
	public class HtmlWriter
	{
		readonly StringBuilder m_Builder = new StringBuilder();
		readonly List<bool> m_HasBlockChild = new List<bool>();
		bool m_InTag;

		public HtmlWriter Open(string tag)
		{
			BeginBlock();
			m_Builder.Append('<').Append(tag);
			m_InTag = true;
			m_HasBlockChild.Add(false);
			return this;
		}

		public HtmlWriter OpenVoid(string tag)
		{
			BeginBlock();
			m_Builder.Append('<').Append(tag);
			m_InTag = true;
			return this;
		}

		public HtmlWriter Attribute(string name, string value)
		{
			if (!m_InTag || string.IsNullOrEmpty(name) || value == null)
			{
				return this;
			}
			m_Builder.Append(' ').Append(name).Append("=\"").Append(EscapeAttribute(value)).Append('"');
			return this;
		}

		public HtmlWriter Flag(string name, bool present)
		{
			if (m_InTag && present)
			{
				m_Builder.Append(' ').Append(name);
			}
			return this;
		}

		public HtmlWriter Text(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return this;
			}
			CloseTag();
			m_Builder.Append(EscapeText(text));
			return this;
		}

		public HtmlWriter Comment(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return this;
			}
			BeginBlock();
			m_Builder.Append("<!-- ").Append(text.Replace("--", "- -")).Append(" -->");
			m_InTag = false;
			return this;
		}

		public HtmlWriter Close(string tag)
		{
			CloseTag();
			var hadBlockChild = Pop();
			if (hadBlockChild)
			{
				Indent();
			}
			m_Builder.Append("</").Append(tag).Append('>');
			return this;
		}

		public HtmlWriter CloseVoid()
		{
			CloseTag();
			return this;
		}

		public override string ToString()
		{
			CloseTag();
			return m_Builder.ToString();
		}

		void BeginBlock()
		{
			CloseTag();
			MarkParentHasBlockChild();
			Indent();
		}

		void MarkParentHasBlockChild()
		{
			if (m_HasBlockChild.Count > 0)
			{
				m_HasBlockChild[m_HasBlockChild.Count - 1] = true;
			}
		}

		bool Pop()
		{
			if (m_HasBlockChild.Count == 0)
			{
				return false;
			}
			var last = m_HasBlockChild.Count - 1;
			var value = m_HasBlockChild[last];
			m_HasBlockChild.RemoveAt(last);
			return value;
		}

		void CloseTag()
		{
			if (m_InTag)
			{
				m_Builder.Append('>');
				m_InTag = false;
			}
		}

		void Indent()
		{
			if (m_Builder.Length > 0)
			{
				m_Builder.Append('\n');
			}
			m_Builder.Append(' ', m_HasBlockChild.Count * 2);
		}

		static string EscapeAttribute(string value)
		{
			return EscapeText(value).Replace("\"", "&quot;");
		}

		static string EscapeText(string value)
		{
			return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
		}
	}
}

#endif
