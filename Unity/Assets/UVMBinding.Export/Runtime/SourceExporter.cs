#if UNITY_EDITOR || UVMBINDING_EXPORT
namespace UVMBinding.Semantics.Export
{
	public static class SourceExporter
	{
		public const string SourceType = "text/x-csharp";
		public const string SourceAttribute = "data-source";

		public static string Export(string typeName, ViewModelDeclaration declaration)
		{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
			if (string.IsNullOrEmpty(typeName))
			{
				return string.Empty;
			}
			if (declaration == null || string.IsNullOrEmpty(declaration.SourceText))
			{
				return $"<!-- unmapped: source not found for {typeName} -->";
			}
			return "<script type=\"" + SourceType + "\" " + SourceAttribute + "=\""
				+ Escape(declaration.SourcePath) + "\">\n"
				+ Guard(declaration.SourceText) + "\n</script>";
#else
			return string.Empty;
#endif
		}

		static string Guard(string source)
		{
			return source.Replace("</script", "<!--/script-->");
		}

		static string Escape(string value)
		{
			return (value ?? string.Empty).Replace("&", "&amp;").Replace("\"", "&quot;")
				.Replace("<", "&lt;").Replace(">", "&gt;");
		}
	}
}

#endif
