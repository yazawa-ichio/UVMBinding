namespace UVMBinding.Converters
{
	[DispName("Core/StringToLength")]
	public class StringToLength : ConverterBase<string, int>
	{
		public override int Convert(string input)
		{
			return input == null ? 0 : input.Length;
		}
	}

}