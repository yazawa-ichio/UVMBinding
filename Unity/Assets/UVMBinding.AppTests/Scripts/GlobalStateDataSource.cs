using UVMBinding;

namespace AppTests
{
	public class GlobalStateDataSource : DataSourceProvider
	{
		protected override object GetDataSource()
		{
			return GlobalState.I;
		}
	}
}