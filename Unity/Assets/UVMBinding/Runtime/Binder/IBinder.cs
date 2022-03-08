namespace UVMBinding.Core
{
	internal interface IBinder : IViewElement
	{
		string Path { get; }
		bool IsActive { get; }
		void TryUpdate();
		void Bind(IBindingProperty prop);
		void Unbind();
	}
}