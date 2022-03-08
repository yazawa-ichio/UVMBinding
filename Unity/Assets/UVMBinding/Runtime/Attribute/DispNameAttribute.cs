using System;

namespace UVMBinding
{
	[System.Diagnostics.Conditional("UNITY_EDITOR")]
	public class DispNameAttribute : Attribute
	{
		public string Name { get; private set; }

		public DispNameAttribute(string name)
		{
			Name = name;
		}
	}
}