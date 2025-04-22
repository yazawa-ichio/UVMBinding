using UVMBinding.Core;

namespace UVMBinding
{
	public abstract class DataSourceProvider : ViewModelSourceProvider
	{
		protected abstract object GetDataSource();

		public override IViewModel GetViewModel()
		{
			var data = GetDataSource();
			if (data is IViewModel model)
			{
				return model;
			}
			else
			{
				return new ReflectionViewModel(data);
			}
		}
	}
}