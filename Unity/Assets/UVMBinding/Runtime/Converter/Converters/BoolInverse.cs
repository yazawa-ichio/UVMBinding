namespace UVMBinding.Converters
{
	[DispName("Core/BoolInverse")]
	public class BoolInverse : ConverterBase<bool, bool>
	{
		public override bool Convert(bool input)
		{
			return !input;
		}

		public override bool TryInverseConvert(bool value, ref bool ret)
		{
			ret = !value;
			return true;
		}

	}
}