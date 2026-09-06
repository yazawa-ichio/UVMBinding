using UVMBinding.Binders;

namespace UVMBinding.Showcase
{
	public enum ShowcaseEnum
	{
		Alpha,
		Beta,
		Gamma,
	}

	public class ShowcaseEnumBinder : EnumPopupBinderBase<ShowcaseEnum>
	{
	}
}