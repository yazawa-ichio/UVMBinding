namespace UVMBinding
{
	public abstract class ComponentSetter<T> : Binder<T>
	{
		protected override void OnBind()
		{
			if (TryGetComponent<T>(out var component))
			{
				Set(component);
			}
		}

		protected override void UpdateValue(T value)
		{
		}
	}
}