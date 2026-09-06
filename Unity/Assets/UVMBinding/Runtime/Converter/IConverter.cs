namespace UVMBinding.Core
{
	internal interface IConverter
	{
		System.Type InputType { get; }
		System.Type OutputType { get; }

		bool NeedsUpdate { get; }
		void TryUpdate();

		bool TryConvert(IBindingProperty input, bool allowCast, ref IBindingProperty output);
		void Unbind();
	}
}
