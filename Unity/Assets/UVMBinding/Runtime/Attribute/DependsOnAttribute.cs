namespace UVMBinding
{
	[System.AttributeUsage(System.AttributeTargets.Property, Inherited = true)]
	public sealed class DependsOnAttribute : System.Attribute
	{
		public readonly string[] Paths;

		public DependsOnAttribute(params string[] paths)
		{
			Paths = paths ?? System.Array.Empty<string>();
		}
	}
}
