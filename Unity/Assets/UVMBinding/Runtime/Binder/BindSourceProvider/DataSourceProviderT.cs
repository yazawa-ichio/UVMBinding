namespace UVMBinding
{
	public abstract class DataSourceProvider<T> : DataSourceProvider
	{
		protected abstract T GetSource();

		protected sealed override object GetDataSource()
		{
			return GetSource();
		}
	}
}
