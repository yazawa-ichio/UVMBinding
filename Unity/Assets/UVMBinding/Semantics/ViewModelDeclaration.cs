using System.Collections.Generic;

namespace UVMBinding.Semantics
{
	public class ViewModelDeclaration
	{
		public IReadOnlyDictionary<string, string[]> DependsOn;
		public IReadOnlyDictionary<string, ConsequenceKind> Consequences;
		public string SourcePath;
		public string SourceText;

		public ConsequenceKind GetConsequence(string eventName)
		{
			return Consequences != null && Consequences.TryGetValue(eventName, out var kind)
				? kind : ConsequenceKind.Unknown;
		}
	}
}
