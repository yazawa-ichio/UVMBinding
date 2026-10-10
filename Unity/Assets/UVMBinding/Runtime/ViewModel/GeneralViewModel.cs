namespace UVMBinding
{

	[Lint.DynamicPath]
	public class GeneralViewModel : ViewModel
	{

		public T Get<T>(string path)
		{
			return GetImpl<T>(path);
		}

		public void Set<T>(string path, T val)
		{
			SetImpl(path, val);
		}

		public bool TryGetAs<T>(string path, out T value)
		{
			return TryGetAsImpl(path, out value);
		}

		public bool TrySet<T>(string path, T val)
		{
			return TrySetImpl(path, val);
		}

		public bool UnSet(string path)
		{
			return Property.UnSet(path);
		}

	}

}