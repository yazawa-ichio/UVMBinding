using System.Collections.Generic;
using UnityEngine;

namespace UVMBinding.Semantics
{
	[System.Flags]
	public enum ElementState
	{
		None = 0,
		Disabled = 1,
		Hidden = 2,
		Clipped = 4,
	}

	public class ElementNode
	{
		public string Id;
		public SemanticRole Role;
		public string Label;
		public string Value;
		public string BindPath;
		public string EventName;
		public string EventArgumentType;
		public string EventArgumentValue;
		public bool HasConverter;
		public ConsequenceKind Consequence;
		public string RowTemplateId;
		public string InteractablePath;
		public string VisiblePath;
		public string[] DependsOn = System.Array.Empty<string>();
		public ElementState State;
		public readonly List<ElementNode> Children = new List<ElementNode>();

		public Component Source;

		public override string ToString()
		{
			return $"{Id} [{Role}] path={BindPath} event={EventName}";
		}
	}
}
