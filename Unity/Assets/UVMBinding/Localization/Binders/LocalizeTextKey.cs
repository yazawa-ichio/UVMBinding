using System;

namespace UVMBinding.Binders
{
	public readonly struct LocalizeTextKey : IEquatable<LocalizeTextKey>
	{
		public static LocalizeTextKey GetEntry(string entry) => new(null, entry);

		public static LocalizeTextKey GetText(string text) => new(text);

		public readonly bool RawText;
		public readonly string Table;
		public readonly string EntryName;

		LocalizeTextKey(string text)
		{
			RawText = true;
			Table = null;
			EntryName = text;
		}

		public LocalizeTextKey(string table, string entryName)
		{
			RawText = false;
			Table = table;
			EntryName = entryName;
		}

		public override bool Equals(object obj)
		{
			return obj is LocalizeTextKey key && Equals(key);
		}

		public bool Equals(LocalizeTextKey other)
		{
			return Table == other.Table &&
				   EntryName == other.EntryName;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Table, EntryName);
		}

		public static bool operator ==(LocalizeTextKey left, LocalizeTextKey right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(LocalizeTextKey left, LocalizeTextKey right)
		{
			return !(left == right);
		}

		public static implicit operator LocalizeTextKey(string v)
		{
			return GetText(v);
		}
	}

}