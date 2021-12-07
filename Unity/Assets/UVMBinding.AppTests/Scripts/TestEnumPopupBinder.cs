using UVMBinding.Binders;

namespace AppTests
{
	public enum TestEnum
	{
		Value1,
		Value2,
		Value3,
		Value4,
		Value5,
	}

	public class TestEnumPopupBinder : EnumPopupBinderBase<TestEnum>
	{
	}

}