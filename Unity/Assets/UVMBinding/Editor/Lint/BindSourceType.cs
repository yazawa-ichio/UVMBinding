using UVMBinding.Core;
using UVMBinding.Drawer;

namespace UVMBinding.Lint
{
	public static class BindSourceType
	{
		public static System.Type Find(string fullName) => BindSourceData.FindType(fullName);

		public static IBindSource Create(System.Type type)
		{
			if (typeof(IViewModel).IsAssignableFrom(type))
			{
				return new ViewModelBindSource(type);
			}
			return new ReflectionBindSource(type);
		}
	}
}
