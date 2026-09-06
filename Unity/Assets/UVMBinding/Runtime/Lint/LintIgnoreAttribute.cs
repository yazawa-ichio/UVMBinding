namespace UVMBinding.Lint
{
	[System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Property
		| System.AttributeTargets.Method | System.AttributeTargets.Event, Inherited = true)]
	public sealed class LintIgnoreAttribute : System.Attribute
	{
		public readonly string Reason;

		public LintIgnoreAttribute(string reason)
		{
			Reason = reason;
		}
	}
}