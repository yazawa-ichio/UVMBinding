using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UVMBinding.Semantics;
using UVMBinding.Semantics.Export;

namespace UVMBinding.Export
{
	public static class HtmlExportCommand
	{
		public const string OutputRoot = "Build/Html";

		[MenuItem("UVMBinding/Export HTML/Active Scene")]
		public static void ExportActiveScene()
		{
			var path = ArgumentValue("-exportScene");
			var scene = string.IsNullOrEmpty(path)
				? EditorSceneManager.GetActiveScene()
				: EditorSceneManager.OpenScene(path);
			var directory = Path.Combine(OutputRoot, scene.name);
			Directory.CreateDirectory(directory);
			var pages = Export(scene);
			var used = new HashSet<string>();
			foreach (var page in pages)
			{
				var name = page.FileName;
				for (var i = 2; !used.Add(name); i++)
				{
					name = Path.GetFileNameWithoutExtension(page.FileName) + "_" + i + ".html";
				}
				File.WriteAllText(Path.Combine(directory, name), page.Html);
			}
			Debug.Log($"[Export] {pages.Count} view(s) -> {directory}");
		}

		public struct Page
		{
			public string Id;
			public string Html;

			public string FileName => Id.Replace('/', '_') + ".html";
		}

		public static List<Page> Export(UnityEngine.SceneManagement.Scene scene)
		{
			var pages = new List<Page>();
			var options = StaticExportOptions();
			foreach (var root in scene.GetRootGameObjects())
			{
				foreach (var node in ViewTraversal.CaptureAll(root, options))
				{
					var parts = new List<string> { HtmlExporter.Export(node), ScriptExporter.Export(node) };
					foreach (var typeName in ViewModelTypes(node))
					{
						parts.Add(SourceExporter.Export(typeName, FindDeclaration(typeName)));
					}
					pages.Add(new Page
					{
						Id = node.Id,
						Html = HtmlDocument.Wrap(node.Id, parts),
					});
				}
			}
			return pages;
		}

		static List<string> ViewModelTypes(ViewNode node)
		{
			var names = new List<string>();
			Collect(node, names);
			return names;
		}

		static void Collect(ViewNode node, List<string> names)
		{
			if (!string.IsNullOrEmpty(node.ViewModelTypeName) && !names.Contains(node.ViewModelTypeName))
			{
				names.Add(node.ViewModelTypeName);
			}
			foreach (var child in node.Children)
			{
				Collect(child, names);
			}
		}

		static ViewTraversal.Options StaticExportOptions()
		{
			var options = ViewTraversal.Options.ForStaticExport;
			options.ResolveDeclaration = FindDeclaration;
			return options;
		}

		public static ViewModelDeclaration FindDeclaration(string typeName)
		{
			if (string.IsNullOrEmpty(typeName))
			{
				return null;
			}
			var type = Lint.BindSourceType.Find(typeName);
			if (type == null)
			{
				return null;
			}
			var data = Drawer.ViewModelData.Get(type);
			Dictionary<string, ConsequenceKind> consequences = null;
			foreach (var entry in data.Events)
			{
				if (entry.Consequence == ConsequenceKind.Unknown)
				{
					continue;
				}
				consequences ??= new Dictionary<string, ConsequenceKind>();
				consequences[entry.Path] = entry.Consequence;
			}
			var source = ViewModelSourceFinder.Find(type);
			return new ViewModelDeclaration
			{
				DependsOn = new Lint.ViewModelBindSource(type).DependsOn,
				Consequences = consequences,
				SourcePath = source.Path,
				SourceText = source.Text,
			};
		}

		static string ArgumentValue(string name)
		{
			var args = System.Environment.GetCommandLineArgs();
			for (int i = 0; i < args.Length - 1; i++)
			{
				if (args[i] == name)
				{
					return args[i + 1];
				}
			}
			return null;
		}
	}
}
