namespace UVMBinding.Binders
{
	public class ActiveBinder : Binder<bool>
	{
		protected override void UpdateValue(bool value)
		{
			gameObject.SetActive(value);
		}
	}
}