namespace UVMBinding
{

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

	}

}