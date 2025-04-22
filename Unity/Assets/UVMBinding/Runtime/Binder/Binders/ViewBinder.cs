using UVMBinding.Core;

namespace UVMBinding.Binders
{
	public class ViewBinder : Binder<object, IView>
	{
		protected override void UpdateValue(object value)
		{
			Target.Bind(value);
		}

		protected override bool CanUse(IView view)
		{
			var target = Target;
			return target == null || !target.Equals(view);
		}

	}

}