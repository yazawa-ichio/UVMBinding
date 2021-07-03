namespace UVMBinding
{
	internal interface IConverter
	{
		System.Type GetInputType();
		System.Type GetOutputType();
		void TryUpdate();
		bool TryConvert(IBindingProperty input, ref IBindingProperty output);
		void Unbind();
	}
}