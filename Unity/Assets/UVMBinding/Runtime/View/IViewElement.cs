namespace UVMBinding.Core
{
	//Viewにバインディングされているコンテンツを取得するため定義
	public interface IViewElement
	{
		bool CanUse(IView view);
	}
}