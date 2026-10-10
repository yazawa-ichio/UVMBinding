using System.Collections.Generic;

#if UNITY_EDITOR || UVMBINDING_EXPORT
namespace UVMBinding.Semantics.Export
{
	public static class HtmlExporter
	{
		public const string ViewNameAttribute = "data-view-name";
		public const string ConsequenceAttribute = "data-consequence";

		public static string Export(ViewNode node)
		{
			var writer = new HtmlWriter();
			if (node != null)
			{
				var ids = new Dictionary<string, List<string>>();
				CollectIds(node, ids);
				WriteView(writer, node, "section", 0, 0, ids);
			}
			return writer.ToString();
		}

		readonly struct Row
		{
			public readonly ViewNode Node;
			public readonly bool IsTemplate;

			public Row(ViewNode node, bool isTemplate)
			{
				Node = node;
				IsTemplate = isTemplate;
			}
		}

		static void WriteView(HtmlWriter writer, ViewNode node, string tag, int position, int count,
			Dictionary<string, List<string>> ids, bool isTemplate = false)
		{
			writer.Open(tag)
				.Attribute("id", node.Id)
				.Attribute(ViewNameAttribute, node.ViewModelTypeName);
			if (count > 0)
			{
				writer.Attribute("aria-posinset", position.ToString())
					.Attribute("aria-setsize", count.ToString());
			}
			if (node.IsModal)
			{
				writer.Attribute("role", "dialog").Attribute("aria-modal", "true");
			}
			writer.Flag("hidden", (node.State & ElementState.Hidden) != 0);
			var rows = CollectRows(node);
			foreach (var element in node.Elements)
			{
				WriteElement(writer, element, ids, rows, isTemplate);
			}
			foreach (var dropped in node.Dropped)
			{
				writer.Comment($"unmapped: no role: {dropped.Id}"
					+ (string.IsNullOrEmpty(dropped.BindPath) ? string.Empty : $" bind={dropped.BindPath}")
					+ $" ({dropped.TypeName})");
			}
			foreach (var child in node.Children)
			{
				if (!IsRow(child, rows))
				{
					WriteView(writer, child, "section", 0, 0, ids);
				}
			}
			writer.Close(tag);
		}

		static Dictionary<string, List<Row>> CollectRows(ViewNode node)
		{
			Dictionary<string, List<Row>> rows = null;
			foreach (var element in EnumerateElements(node.Elements))
			{
				if (element.Role != SemanticRole.List)
				{
					continue;
				}
				foreach (var child in node.Children)
				{
					if (Deepest(child, node) != element)
					{
						continue;
					}
					var contained = child.Id.StartsWith(element.Id + "/", System.StringComparison.Ordinal);
					var template = !contained && !string.IsNullOrEmpty(element.RowTemplateId)
						&& child.Id == element.RowTemplateId;
					if (!contained && !template)
					{
						continue;
					}
					rows ??= new Dictionary<string, List<Row>>();
					if (!rows.TryGetValue(element.Id, out var list))
					{
						rows[element.Id] = list = new List<Row>();
					}
					list.Add(new Row(child, template));
				}
			}
			return rows;
		}

		static ElementNode Deepest(ViewNode child, ViewNode node)
		{
			ElementNode best = null;
			foreach (var element in EnumerateElements(node.Elements))
			{
				if (element.Role != SemanticRole.List)
				{
					continue;
				}
				var contained = child.Id.StartsWith(element.Id + "/", System.StringComparison.Ordinal);
				var template = !string.IsNullOrEmpty(element.RowTemplateId) && child.Id == element.RowTemplateId;
				if (!contained && !template)
				{
					continue;
				}
				if (best == null || element.Id.Length > best.Id.Length)
				{
					best = element;
				}
			}
			return best;
		}

		static bool IsHidden(ViewNode node) => (node.State & ElementState.Hidden) != 0;

		static bool IsRow(ViewNode child, Dictionary<string, List<Row>> rows)
		{
			if (rows == null)
			{
				return false;
			}
			foreach (var pair in rows)
			{
				foreach (var row in pair.Value)
				{
					if (row.Node == child)
					{
						return true;
					}
				}
			}
			return false;
		}

		static IEnumerable<ElementNode> EnumerateElements(List<ElementNode> elements)
		{
			foreach (var element in elements)
			{
				yield return element;
				foreach (var child in EnumerateElements(element.Children))
				{
					yield return child;
				}
			}
		}

		static void CollectIds(ViewNode node, Dictionary<string, List<string>> ids)
		{
			foreach (var element in node.Elements)
			{
				CollectIds(element, ids);
			}
			foreach (var child in node.Children)
			{
				CollectIds(child, ids);
			}
		}

		static void CollectIds(ElementNode element, Dictionary<string, List<string>> ids)
		{
			if (!string.IsNullOrEmpty(element.BindPath))
			{
				if (!ids.TryGetValue(element.BindPath, out var list))
				{
					ids[element.BindPath] = list = new List<string>();
				}
				list.Add(element.Id);
			}
			foreach (var child in element.Children)
			{
				CollectIds(child, ids);
			}
		}

		static void WriteElement(HtmlWriter writer, ElementNode element,
			Dictionary<string, List<string>> ids, Dictionary<string, List<Row>> rows, bool isTemplate)
		{
			var tag = HtmlElementMap.Get(element.Role);
			WriteUnmapped(writer, element, ids, rows);
			if (tag.IsVoid)
			{
				writer.OpenVoid(tag.Name);
			}
			else
			{
				writer.Open(tag.Name);
			}
			WriteAttributes(writer, element, tag, ids, isTemplate);

			if (tag.IsVoid)
			{
				writer.CloseVoid();
				if (element.Children.Count > 0)
				{
					writer.Comment($"unmapped: children of <{tag.Name}> are emitted as siblings");
					foreach (var child in element.Children)
					{
						WriteElement(writer, child, ids, rows, isTemplate);
					}
				}
				return;
			}

			if (tag.ValueAsOption && !string.IsNullOrEmpty(element.Value))
			{
				writer.Open("option").Attribute("value", element.Value).Flag("selected", true)
					.Text(element.Label).Close("option");
			}
			if (!tag.ValueInAttribute && element.Role != SemanticRole.Image)
			{
				writer.Text(element.Label);
			}
			WriteRows(writer, element, rows, ids);
			foreach (var child in element.Children)
			{
				WriteElement(writer, child, ids, rows, isTemplate);
			}
			writer.Close(tag.Name);
		}

		static void WriteAttributes(HtmlWriter writer, ElementNode element, HtmlTag tag,
			Dictionary<string, List<string>> ids, bool isTemplate)
		{
			writer.Attribute("id", element.Id)
				.Attribute("name", NameOf(element))
				.Attribute("type", Empty(tag.Type))
				.Attribute("role", Empty(tag.Role));

			if (element.Role == SemanticRole.Image)
			{
				writer.Attribute("alt", element.Label ?? string.Empty);
			}
			if (tag.ValueAsChecked)
			{
				writer.Flag("checked", IsTrue(element.Value));
			}
			if (!string.IsNullOrEmpty(element.EventName))
			{
				writer.Attribute("value", isTemplate ? null : element.EventArgumentValue)
					.Attribute(ConsequenceAttribute, ConsequenceGate.ToAttributeValue(element.Consequence));
			}
			else if (tag.ValueInAttribute)
			{
				writer.Attribute("value", element.Value);
			}
			writer.Attribute("for", Empty(ResolveDependsOn(element, ids)))
				.Flag("disabled", (element.State & ElementState.Disabled) != 0)
				.Flag("hidden", (element.State & ElementState.Hidden) != 0);
		}

		static string NameOf(ElementNode element)
		{
			return !string.IsNullOrEmpty(element.EventName) ? element.EventName : Empty(element.BindPath);
		}

		static string ResolveDependsOn(ElementNode element, Dictionary<string, List<string>> ids)
		{
			if (element.DependsOn.Length == 0)
			{
				return null;
			}
			string result = null;
			foreach (var path in element.DependsOn)
			{
				if (!ids.TryGetValue(path, out var list))
				{
					continue;
				}
				foreach (var id in list)
				{
					result = result == null ? id : result + " " + id;
				}
			}
			return result;
		}

		static void WriteRows(HtmlWriter writer, ElementNode element, Dictionary<string, List<Row>> rows,
			Dictionary<string, List<string>> ids)
		{
			if (rows == null || !rows.TryGetValue(element.Id, out var list))
			{
				return;
			}
			var count = 0;
			foreach (var row in list)
			{
				if (!row.IsTemplate && !IsHidden(row.Node))
				{
					count++;
				}
			}
			var position = 0;
			foreach (var row in list)
			{
				if (row.IsTemplate)
				{
					writer.Comment("row template: shows the shape of a row, not a row");
					WriteView(writer, row.Node, "li", 0, 0, ids, true);
					continue;
				}
				if (IsHidden(row.Node))
				{
					writer.Comment("pooled row: kept for reuse, not a row");
					WriteView(writer, row.Node, "li", 0, 0, ids);
					continue;
				}
				WriteView(writer, row.Node, "li", ++position, count, ids);
			}
		}

		static void WriteUnmapped(HtmlWriter writer, ElementNode element,
			Dictionary<string, List<string>> ids, Dictionary<string, List<Row>> rows)
		{
			if (!string.IsNullOrEmpty(element.EventName) && !string.IsNullOrEmpty(element.BindPath))
			{
				writer.Comment($"unmapped: bind path {element.BindPath}");
			}
			foreach (var path in element.DependsOn)
			{
				if (!ids.ContainsKey(path))
				{
					writer.Comment($"unmapped: depends on {path} which has no element");
				}
			}
			if (element.Role == SemanticRole.List
				&& (rows == null || !rows.ContainsKey(element.Id)))
			{
				writer.Comment("unmapped: row template is outside this capture");
			}
			if (!string.IsNullOrEmpty(element.EventName)
				&& string.IsNullOrEmpty(element.Value) && string.IsNullOrEmpty(element.Label))
			{
				writer.Comment("unmapped: no bound label");
			}
		}

		static bool IsTrue(string value)
		{
			return string.Equals(value, "True", System.StringComparison.OrdinalIgnoreCase);
		}

		static string Empty(string value) => string.IsNullOrEmpty(value) ? null : value;
	}
}

#endif
