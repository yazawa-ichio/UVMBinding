namespace UVMBinding.Core
{
	public interface IViewEventDispatcher
	{
		void Dispatch(string name);
		void Dispatch<T>(string name, T args);
	}
}