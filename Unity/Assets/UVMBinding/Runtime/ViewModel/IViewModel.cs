namespace UVMBinding.Core
{
	[UnityEngine.Scripting.Preserve]
	public interface IViewModel
	{
		EventBroker Event { get; }
		PropertyContainer Property { get; }

		void Set<T>(string path, T val);
		T Get<T>(string path);

		bool TrySet<T>(string path, T val);
		bool TryGetAs<T>(string path, out T value);

		void SetDirty(string path);
		void SetAllDirty();

		void OnBind();
		void OnUnbind();
	}
}