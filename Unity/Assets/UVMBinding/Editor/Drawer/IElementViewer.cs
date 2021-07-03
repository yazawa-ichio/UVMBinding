namespace UVMBinding.Drawer
{
	public interface IElementViewer
	{
		string GetName();
		void DrawInspector();
		bool CanRemove();
		void Remove();
		bool IsBindable();
		bool DrawBinding(bool selected);
	}

}