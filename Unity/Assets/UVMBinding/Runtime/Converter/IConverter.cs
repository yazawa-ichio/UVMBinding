namespace UVMBinding.Core
{
	internal interface IConverter
	{
		System.Type GetInputType();
		System.Type GetOutputType();
		void TryUpdate();
		//outputType は Binder<T> の T。入出力の型がバインド時に決まる Converter が使う
		bool TryConvert(IBindingProperty input, System.Type outputType, ref IBindingProperty output);
		void Unbind();
	}
}