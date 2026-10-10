#if UNITY_EDITOR || UVMBINDING_EXPORT
namespace UVMBinding.Semantics.Export
{
	public struct HtmlTag
	{
		public string Name;
		public string Role;
		public string Type;
		public bool IsVoid;
		public bool ValueInAttribute;
		public bool ValueAsChecked;
		public bool ValueAsOption;
	}

	public static class HtmlElementMap
	{
		public static HtmlTag Get(SemanticRole role)
		{
			switch (role)
			{
				case SemanticRole.Text: return new HtmlTag { Name = "output" };
				case SemanticRole.Heading: return new HtmlTag { Name = "h2" };
				case SemanticRole.Image: return new HtmlTag { Name = "img", IsVoid = true };
				case SemanticRole.Button: return new HtmlTag { Name = "button", Type = "button" };
				case SemanticRole.Link: return new HtmlTag { Name = "a" };
				case SemanticRole.TextBox:
					return new HtmlTag { Name = "input", Type = "text", IsVoid = true, ValueInAttribute = true };
				case SemanticRole.SearchBox:
					return new HtmlTag { Name = "input", Type = "search", IsVoid = true, ValueInAttribute = true };
				case SemanticRole.Checkbox:
					return new HtmlTag { Name = "input", Type = "checkbox", IsVoid = true, ValueAsChecked = true };
				case SemanticRole.Switch:
					return new HtmlTag { Name = "input", Role = "switch", Type = "checkbox", IsVoid = true, ValueAsChecked = true };
				case SemanticRole.Slider:
					return new HtmlTag { Name = "input", Type = "range", IsVoid = true, ValueInAttribute = true };
				case SemanticRole.Scrollbar:
					return new HtmlTag { Name = "input", Role = "scrollbar", Type = "range", IsVoid = true, ValueInAttribute = true };
				case SemanticRole.ComboBox: return new HtmlTag { Name = "select", ValueAsOption = true };
				case SemanticRole.List: return new HtmlTag { Name = "ul" };
				case SemanticRole.ListItem: return new HtmlTag { Name = "li" };
				case SemanticRole.Group: return new HtmlTag { Name = "div", Role = "group" };
				case SemanticRole.Dialog: return new HtmlTag { Name = "div", Role = "dialog" };
				case SemanticRole.Tab: return new HtmlTag { Name = "button", Role = "tab", Type = "button" };
				case SemanticRole.TabList: return new HtmlTag { Name = "div", Role = "tablist" };
				case SemanticRole.ProgressBar: return new HtmlTag { Name = "div", Role = "progressbar" };
				default: return new HtmlTag { Name = "div", Role = "none" };
			}
		}
	}
}

#endif
