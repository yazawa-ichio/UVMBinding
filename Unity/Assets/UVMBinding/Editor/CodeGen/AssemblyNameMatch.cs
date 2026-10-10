using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UVMBinding.CodeGen
{
	public static class AssemblyNameMatch
	{
		public static string Find(IEnumerable<string> references, string name)
		{
			return references.FirstOrDefault(r => Path.GetFileNameWithoutExtension(r) == name);
		}
	}

}
