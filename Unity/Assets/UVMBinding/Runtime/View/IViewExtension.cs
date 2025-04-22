using System;
using UVMBinding.Core;

namespace UVMBinding
{
	public static class IViewExtension
	{
		public static void Bind(this IView self, object obj)
		{
			if (obj is IViewModel model)
			{
				self.ViewModel = model;
			}
			else
			{
				self.ViewModel = new ReflectionViewModel(obj);
			}
		}

		public static T Attach<T>(this IView self, Action<T> action) where T : IViewModel, new()
		{
			T vm = new();
			action?.Invoke(vm);
			self.ViewModel = vm;
			return vm;
		}

	}
}