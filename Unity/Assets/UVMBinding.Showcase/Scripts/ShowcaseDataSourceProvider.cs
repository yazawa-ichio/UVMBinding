namespace UVMBinding.Showcase
{
	public class ShowcaseDataSourceProvider : DataSourceProvider<ShowcaseGlobalState>
	{
		protected override ShowcaseGlobalState GetSource()
		{
			return ShowcaseGlobalState.Instance;
		}
	}
}
