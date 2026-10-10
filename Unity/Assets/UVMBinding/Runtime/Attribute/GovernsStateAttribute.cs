namespace UVMBinding
{
	public enum GovernedState
	{
		None = 0,
		Interactable,
		Visible,
	}

	[System.AttributeUsage(System.AttributeTargets.Class, Inherited = true)]
	public sealed class GovernsStateAttribute : System.Attribute
	{
		public readonly GovernedState State;

		public GovernsStateAttribute(GovernedState state)
		{
			State = state;
		}
	}
}
