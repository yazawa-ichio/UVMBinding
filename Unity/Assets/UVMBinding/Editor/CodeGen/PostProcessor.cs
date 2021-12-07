using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace UVMBinding.CodeGen
{


	public class PostProcessor : ILPostProcessor
	{
		private readonly List<DiagnosticMessage> m_Diagnostics = new List<DiagnosticMessage>();

		public override ILPostProcessor GetInstance()
		{
			return this;
		}

		public override bool WillProcess(ICompiledAssembly compiledAssembly)
		{
			var referenceDlls = compiledAssembly.References
					.Select(Path.GetFileNameWithoutExtension);

			return referenceDlls.Any(x => x == "UVMBinding");
		}

		public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
		{
			if (!WillProcess(compiledAssembly))
			{
				return null;
			}
			m_Diagnostics.Clear();

			var assemblyDefinition = AssemblyDefinitionFor(compiledAssembly);
			if (assemblyDefinition == null)
			{
				return null;
			}

			var mainModule = assemblyDefinition.MainModule;
			if (mainModule == null)
			{
				return null;
			}
			try
			{
				foreach (var typeDefinition in mainModule.Types)
				{
					if (TryGetViewModelReference(typeDefinition, out var vmReference))
					{
						new ViewModelPostProcessor(typeDefinition, vmReference, m_Diagnostics).Process();
					}
				}
			}
			catch (Exception error)
			{
				m_Diagnostics.Add(new DiagnosticMessage
				{
					DiagnosticType = DiagnosticType.Error,
					MessageData = error.ToString().Replace("\r\n", "").Replace("\n", ""),
					File = error.Source,
				});
			}

			var pe = new MemoryStream();
			var pdb = new MemoryStream();

			var writerParameters = new WriterParameters
			{
				SymbolWriterProvider = new PortablePdbWriterProvider(),
				SymbolStream = pdb,
				WriteSymbols = true
			};

			assemblyDefinition.Write(pe, writerParameters);

			return new ILPostProcessResult(new InMemoryAssembly(pe.ToArray(), pdb.ToArray()), m_Diagnostics);
		}


		bool TryGetViewModelReference(TypeDefinition typeDefinition, out TypeReference vmReference)
		{
			var type = typeDefinition;
			while (type != null)
			{
				if (type.FullName == "UVMBinding.ViewModel")
				{
					vmReference = type;
					return true;
				}
				type = type.BaseType?.Resolve();
			}
			vmReference = null;
			return false;
		}

		AssemblyDefinition AssemblyDefinitionFor(ICompiledAssembly compiledAssembly)
		{
			var readerParameters = new ReaderParameters
			{
				SymbolStream = new MemoryStream(compiledAssembly.InMemoryAssembly.PdbData.ToArray()),
				SymbolReaderProvider = new PortablePdbReaderProvider(),
				AssemblyResolver = new PostProcessorAssemblyResolver(compiledAssembly.References),
				ReflectionImporterProvider = new ReflectionImporterProvider(),
				ReadingMode = ReadingMode.Immediate
			};

			var peStream = new MemoryStream(compiledAssembly.InMemoryAssembly.PeData.ToArray());
			return AssemblyDefinition.ReadAssembly(peStream, readerParameters);
		}
	}

}