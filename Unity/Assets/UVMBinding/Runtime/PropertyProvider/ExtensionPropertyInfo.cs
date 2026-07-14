using System;
namespace UVMBinding.PropertyProvider
{
	public readonly struct ExtensionPropertyInfo
	{
		public readonly string Path;
		public readonly Type Type;

		public ExtensionPropertyInfo(string path, Type type)
		{
			Path = path;
			Type = type;
		}
	}
}