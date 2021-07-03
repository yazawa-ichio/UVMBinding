using UVMBinding.Binders;

namespace UVMBinding.Tests
{

	public class UpdateImmediateTextBinder : TextBinder
	{
		protected override bool UpdateImmediate => true;
	}

}