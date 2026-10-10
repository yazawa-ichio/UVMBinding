using System;
namespace UVMBinding.PropertyProvider
{
	public enum PropertyRelation
	{
		None = 0,
		All,
		Any,
		Format,
	}

	public readonly struct ExtensionPropertyInfo
	{
		static readonly string[] s_Empty = Array.Empty<string>();

		public readonly string Path;
		public readonly Type Type;
		public readonly string[] SourcePaths;
		public readonly PropertyRelation Relation;
		public readonly bool Negated;
		public readonly string Format;

		public ExtensionPropertyInfo(string path, Type type)
		{
			Path = path;
			Type = type;
			SourcePaths = s_Empty;
			Relation = PropertyRelation.None;
			Negated = false;
			Format = null;
		}

		public ExtensionPropertyInfo(string path, Type type, params string[] sourcePaths)
		{
			Path = path;
			Type = type;
			SourcePaths = sourcePaths ?? s_Empty;
			Relation = PropertyRelation.None;
			Negated = false;
			Format = null;
		}

		public ExtensionPropertyInfo(string path, Type type, string[] sourcePaths,
			PropertyRelation relation, bool negated = false, string format = null)
		{
			Path = path;
			Type = type;
			SourcePaths = sourcePaths ?? s_Empty;
			Relation = relation;
			Negated = negated;
			Format = format;
		}
	}
}