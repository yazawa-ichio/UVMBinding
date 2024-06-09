using System.Collections.Generic;

namespace UVMBinding.Core
{
	public interface IAutoViewUpdate
	{
		bool IsActive { get; }
		void TryUpdate();
	}

	public interface IView
	{
		IViewModel ViewModel { get; set; }
		void Prepare(bool force = false);
		void GetElements(List<IViewElement> elements);
		void Add(IViewElement elm);
		void Remove(IViewElement elm);
		void TryUpdate();
	}
}