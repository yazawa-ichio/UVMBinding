namespace UVMBinding.Lint
{
	[System.AttributeUsage(System.AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
	public sealed class LintIncompatibleTargetAttribute : System.Attribute
	{
		public readonly System.Type Type;
		public readonly string Reason;

		public LintIncompatibleTargetAttribute(System.Type type, string reason)
		{
			Type = type;
			Reason = reason;
		}
	}
}
