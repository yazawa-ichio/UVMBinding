using Mono.Cecil;
using System.Linq;
using System.Reflection;

namespace UVMBinding.CodeGen
{
	internal class ReflectionImporter : DefaultReflectionImporter
	{
		const string SystemPrivateCoreLib = "System.Private.CoreLib";
		AssemblyNameReference m_CorrectCorlib;

		public ReflectionImporter(ModuleDefinition module) : base(module)
		{
			m_CorrectCorlib = module.AssemblyReferences.FirstOrDefault(a => a.Name == "mscorlib" || a.Name == "netstandard" || a.Name == SystemPrivateCoreLib);
		}

		public override AssemblyNameReference ImportReference(AssemblyName reference)
		{
			return m_CorrectCorlib != null && reference.Name == SystemPrivateCoreLib ? m_CorrectCorlib : base.ImportReference(reference);
		}
	}

}