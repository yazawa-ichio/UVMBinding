using System.Collections.Generic;
using System.Text;
using UVMBinding.PropertyProvider;

#if UNITY_EDITOR || UVMBINDING_EXPORT
namespace UVMBinding.Semantics.Export
{
	public static class ScriptExporter
	{
		public const string GraphType = "application/json";
		public const string GraphId = "uvmbinding-graph";

		public static string Export(ViewNode node)
		{
			if (node == null)
			{
				return string.Empty;
			}
			var views = new List<ViewNode>();
			Flatten(node, views);
			var sb = new StringBuilder();
			sb.Append("<script type=").Append(Quote(GraphType))
				.Append(" id=").Append(Quote(GraphId)).Append(">").Append(NewLine);
			sb.Append("{").Append(NewLine).Append("  ").Append(Quote("views")).Append(": [").Append(NewLine);
			for (int i = 0; i < views.Count; i++)
			{
				WriteView(sb, views[i], i + 1 < views.Count);
			}
			sb.Append("  ]").Append(NewLine).Append("}").Append(NewLine).Append("</script>");
			return sb.ToString();
		}

		static void Flatten(ViewNode node, List<ViewNode> result)
		{
			result.Add(node);
			foreach (var child in node.Children)
			{
				Flatten(child, result);
			}
		}

		static void WriteView(StringBuilder sb, ViewNode node, bool more)
		{
			var paths = new Dictionary<string, List<ElementNode>>();
			foreach (var element in node.Elements)
			{
				Collect(element, paths);
			}
			sb.Append("    {").Append(NewLine);
			Field(sb, "id", node.Id, true);
			Field(sb, "viewModel", node.ViewModelTypeName, true);
			WritePaths(sb, paths);
			WriteMirrors(sb, paths);
			WriteDerived(sb, node);
			WriteStale(sb, node);
			sb.Append("    }").Append(more ? "," : string.Empty).Append(NewLine);
		}

		static void Collect(ElementNode element, Dictionary<string, List<ElementNode>> paths)
		{
			if (!string.IsNullOrEmpty(element.BindPath))
			{
				if (!paths.TryGetValue(element.BindPath, out var list))
				{
					paths[element.BindPath] = list = new List<ElementNode>();
				}
				list.Add(element);
			}
			foreach (var child in element.Children)
			{
				Collect(child, paths);
			}
		}

		static void WritePaths(StringBuilder sb, Dictionary<string, List<ElementNode>> paths)
		{
			sb.Append("      ").Append(Quote("paths")).Append(": {").Append(NewLine);
			var first = true;
			foreach (var pair in paths)
			{
				if (!first)
				{
					sb.Append(",").Append(NewLine);
				}
				sb.Append("        ").Append(Quote(pair.Key)).Append(": [");
				for (int i = 0; i < pair.Value.Count; i++)
				{
					if (i > 0)
					{
						sb.Append(", ");
					}
					sb.Append(Quote(pair.Value[i].Id));
				}
				sb.Append("]");
				first = false;
			}
			sb.Append(NewLine).Append("      },").Append(NewLine);
		}

		static void WriteMirrors(StringBuilder sb, Dictionary<string, List<ElementNode>> paths)
		{
			sb.Append("      ").Append(Quote("mirrors")).Append(": [");
			var first = true;
			foreach (var pair in paths)
			{
				if (pair.Value.Count < 2 || HasConverter(pair.Value))
				{
					continue;
				}
				if (!first)
				{
					sb.Append(", ");
				}
				sb.Append(Quote(pair.Key));
				first = false;
			}
			sb.Append("],").Append(NewLine);
		}

		static bool HasConverter(List<ElementNode> elements)
		{
			foreach (var element in elements)
			{
				if (element.HasConverter)
				{
					return true;
				}
			}
			return false;
		}

		static void WriteDerived(StringBuilder sb, ViewNode node)
		{
			var hasDerived = Has(node, PropertyRelation.None, false);
			sb.Append("      ").Append(Quote("derived")).Append(": [");
			if (hasDerived)
			{
				sb.Append(NewLine);
			}
			var first = true;
			foreach (var derived in node.Derived)
			{
				if (derived.Relation == PropertyRelation.None)
				{
					continue;
				}
				if (!first)
				{
					sb.Append(",").Append(NewLine);
				}
				sb.Append("        { ").Append(Quote("path")).Append(": ").Append(Quote(derived.Path))
					.Append(", ").Append(Quote("op")).Append(": ")
					.Append(Quote(derived.Relation.ToString().ToLowerInvariant()))
					.Append(", ").Append(Quote("sources")).Append(": ").Append(Array(derived.Sources));
				if (derived.Negated)
				{
					sb.Append(", ").Append(Quote("negate")).Append(": true");
				}
				if (derived.Relation == PropertyRelation.Format)
				{
					sb.Append(", ").Append(Quote("format")).Append(": ").Append(Quote(derived.Format));
				}
				sb.Append(" }");
				first = false;
			}
			sb.Append(hasDerived ? NewLine + "      " : string.Empty).Append("],").Append(NewLine);
		}

		static void WriteStale(StringBuilder sb, ViewNode node)
		{
			var hasStale = Has(node, PropertyRelation.None);
			sb.Append("      ").Append(Quote("stale")).Append(": [");
			if (hasStale)
			{
				sb.Append(NewLine);
			}
			var first = true;
			foreach (var derived in node.Derived)
			{
				if (derived.Relation != PropertyRelation.None)
				{
					continue;
				}
				if (!first)
				{
					sb.Append(",").Append(NewLine);
				}
				sb.Append("        { ").Append(Quote("path")).Append(": ").Append(Quote(derived.Path))
					.Append(", ").Append(Quote("sources")).Append(": ").Append(Array(derived.Sources))
					.Append(" }");
				first = false;
			}
			sb.Append(hasStale ? NewLine + "      " : string.Empty).Append("]").Append(NewLine);
		}

		static bool Has(ViewNode node, PropertyRelation kind, bool equal = true)
		{
			foreach (var derived in node.Derived)
			{
				if ((derived.Relation == kind) == equal)
				{
					return true;
				}
			}
			return false;
		}

		static void Field(StringBuilder sb, string name, string value, bool more)
		{
			sb.Append("      ").Append(Quote(name)).Append(": ").Append(Quote(value))
				.Append(more ? "," : string.Empty).Append(NewLine);
		}

		static string Array(string[] values)
		{
			var sb = new StringBuilder("[");
			for (int i = 0; i < values.Length; i++)
			{
				if (i > 0)
				{
					sb.Append(", ");
				}
				sb.Append(Quote(values[i]));
			}
			return sb.Append("]").ToString();
		}

		const string NewLine = "\n";
		const char Backslash = (char)92;

		static string Quote(string value)
		{
			var sb = new StringBuilder(QuoteMark);
			foreach (var c in value ?? string.Empty)
			{
				switch (c)
				{
					case '"': sb.Append(Backslash).Append('"'); break;
					case (char)92: sb.Append(Backslash).Append(Backslash); break;
					case '\n': sb.Append(Backslash).Append('n'); break;
					case '\r': sb.Append(Backslash).Append('r'); break;
					case '\t': sb.Append(Backslash).Append('t'); break;
					case '<': sb.Append(Backslash).Append("u003c"); break;
					default:
						if (c < ' ')
						{
							sb.Append(Backslash).Append("u").Append(((int)c).ToString("x4"));
						}
						else
						{
							sb.Append(c);
						}
						break;
				}
			}
			return sb.Append(QuoteMark).ToString();
		}

		static readonly string QuoteMark = ((char)34).ToString();
	}
}

#endif
