namespace UVMBinding.Core
{
	[UnityEngine.Scripting.Preserve]
	public interface IViewModel
	{
		EventBroker Event { get; }
		PropertyContainer Property { get; }

		void Set<T>(string path, T val);
		T Get<T>(string path);

		void SetDirty(string path);
		void SetAllDirty();

		void OnBind();
		void OnUnbind();
	}
}