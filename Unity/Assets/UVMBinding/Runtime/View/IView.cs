using System.Collections.Generic;

namespace UVMBinding.Core
{
	public interface IView
	{
		bool IsActive { get; }
		IViewModel ViewModel { get; set; }
		void Prepare(bool force = false);
		void TryUpdate();
		void GetElements(List<IViewElement> elements);
	}
}