using UVMBinding.PropertyProvider;

namespace UVMBinding.Semantics
{
	public class DerivedValue
	{
		public string Path;
		public string[] Sources = System.Array.Empty<string>();
		public PropertyRelation Relation;
		public bool Negated;
		public string Format;

		public override string ToString()
		{
			return $"{Path} <- {Relation}({string.Join(", ", Sources)})";
		}
	}
}
