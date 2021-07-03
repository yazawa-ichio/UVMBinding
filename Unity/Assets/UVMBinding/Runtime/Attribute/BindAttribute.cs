using System;
namespace UVMBinding
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
	public class BindAttribute : Attribute
	{
		public string Path { get; private set; }

		public BindAttribute() { }

		public BindAttribute(string path)
		{
			Path = path;
		}

	}
}