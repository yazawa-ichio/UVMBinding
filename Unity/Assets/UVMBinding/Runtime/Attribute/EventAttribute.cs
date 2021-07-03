using System;
namespace UVMBinding
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Event | AttributeTargets.Method, AllowMultiple = true)]
	public class EventAttribute : Attribute
	{
		public string Path { get; private set; }

		public EventAttribute() { }

		public EventAttribute(string path)
		{
			Path = path;
		}

	}
}