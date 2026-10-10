namespace UVMBinding.Semantics
{
	public class DroppedElement
	{
		public string Id;
		public string BindPath;
		public string TypeName;

		public override string ToString() => $"{Id} {BindPath} ({TypeName})";
	}
}
