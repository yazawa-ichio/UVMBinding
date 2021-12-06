using Mono.Cecil;

namespace UVMBinding.CodeGen
{
	internal class ReflectionImporterProvider : IReflectionImporterProvider
	{
		public IReflectionImporter GetReflectionImporter(ModuleDefinition moduleDefinition)
		{
			return new ReflectionImporter(moduleDefinition);
		}
	}

}