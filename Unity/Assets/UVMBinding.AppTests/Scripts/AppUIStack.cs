using ILib.UI;

namespace AppTests
{
	public class AppUIStack : UIStack<AppViewModel, AppUIControl>
	{
		public IStackEntry Push<T>(string path, System.Action<T> action) where T : AppViewModel, new()
		{
			var vm = new T();
			action?.Invoke(vm);
			return Push(path, vm);
		}

		public IStackEntry Switch<T>(string path, System.Action<T> action) where T : AppViewModel, new()
		{
			var vm = new T();
			action?.Invoke(vm);
			return Switch(path, vm);
		}
	}
}