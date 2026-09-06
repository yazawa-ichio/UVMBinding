using System.Collections.Generic;

namespace UVMBinding.Semantics
{
	public class ViewNode
	{
		public string Id;
		public string Label;
		public bool IsModal;
		public string ViewModelTypeName;
		public readonly List<DerivedValue> Derived = new List<DerivedValue>();
		public ElementState State;
		public readonly List<ElementNode> Elements = new List<ElementNode>();
		public readonly List<DroppedElement> Dropped = new List<DroppedElement>();
		public readonly List<ViewNode> Children = new List<ViewNode>();

		public View Source;

		public override string ToString()
		{
			return $"{Id} vm={ViewModelTypeName} elements={Elements.Count} children={Children.Count}";
		}
	}
}
