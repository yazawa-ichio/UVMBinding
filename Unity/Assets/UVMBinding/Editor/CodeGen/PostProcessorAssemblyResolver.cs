using Mono.Cecil;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UVMBinding.CodeGen
{
	internal class PostProcessorAssemblyResolver : IAssemblyResolver
	{
		private readonly string[] _references;
		Dictionary<string, AssemblyDefinition> _cache = new Dictionary<string, AssemblyDefinition>();

		public PostProcessorAssemblyResolver(string[] references)
		{
			_references = references;
		}

		public void Dispose()
		{
		}

		public AssemblyDefinition Resolve(AssemblyNameReference name)
		{
			return Resolve(name, new ReaderParameters(ReadingMode.Deferred));
		}


		public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
		{
			lock (_cache)
			{
				var fileName = _references.FirstOrDefault(r => r.EndsWith(name.Name + ".dll"));
				if (fileName == null)
					return null;

				var lastWriteTime = File.GetLastWriteTime(fileName);

				var cacheKey = fileName + lastWriteTime.ToString();

				if (_cache.TryGetValue(cacheKey, out var result))
					return result;

				parameters.AssemblyResolver = this;

				var ms = new MemoryStream(File.ReadAllBytes(fileName));

				var pdb = fileName + ".pdb";
				if (File.Exists(pdb))
					parameters.SymbolStream = new MemoryStream(File.ReadAllBytes(pdb));

				var assemblyDefinition = AssemblyDefinition.ReadAssembly(ms, parameters);
				_cache.Add(cacheKey, assemblyDefinition);
				return assemblyDefinition;
			}
		}
	}

}