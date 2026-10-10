namespace UVMBinding.Binders
{
	[SemanticRole(SemanticRole.None)]
	[GovernsState(GovernedState.Visible)]
	public class ActiveBinder : Binder<bool>
	{
		protected override void UpdateValue(bool value)
		{
			gameObject.SetActive(value);
		}
	}
}