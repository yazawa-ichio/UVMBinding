namespace UVMBinding.Core
{
	public interface IViewEvent : IViewElement
	{
		string Name { get; }
		System.Type EventType();
		void Bind(IViewEventDispatcher handler);
	}
}