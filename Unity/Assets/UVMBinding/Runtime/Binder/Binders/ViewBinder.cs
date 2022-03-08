using UVMBinding.Core;

namespace UVMBinding.Binders
{
	public class ViewBinder : Binder<IViewModel, IView>
	{

		protected override void UpdateValue(IViewModel value)
		{
			Target.ViewModel = value;
		}

		protected override bool CanUse(IView view)
		{
			var target = Target;
			return target == null || !target.Equals(view);
		}

	}
}