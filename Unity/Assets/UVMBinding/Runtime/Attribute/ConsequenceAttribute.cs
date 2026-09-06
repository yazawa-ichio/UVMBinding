namespace UVMBinding
{
	public enum ConsequenceKind
	{
		Unknown = 0,
		Safe,
		Irreversible,
		Purchase,
		Destructive,
	}

	[System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Method
		| System.AttributeTargets.Event, Inherited = true)]
	public sealed class ConsequenceAttribute : System.Attribute
	{
		public readonly ConsequenceKind Kind;

		public ConsequenceAttribute(ConsequenceKind kind)
		{
			Kind = kind;
		}
	}

	public static class ConsequenceGate
	{
		public static bool AllowsAutoPress(ConsequenceKind kind) => kind == ConsequenceKind.Safe;

		public static string ToAttributeValue(ConsequenceKind kind) => kind.ToString().ToLowerInvariant();
	}
}
