using UVMBinding.Core;

namespace UVMBinding
{
	public abstract class ViewModelSourceProvider<T> : ViewModelSourceProvider where T : IViewModel
	{
		protected abstract T GetSource();

		public sealed override IViewModel GetViewModel()
		{
			return GetSource();
		}
	}
}
