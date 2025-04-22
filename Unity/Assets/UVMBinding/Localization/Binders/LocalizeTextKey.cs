using System;
using System.Collections.Generic;
using UnityEngine.Localization;

namespace UVMBinding.Binders
{
	public readonly struct LocalizeTextKey : IEquatable<LocalizeTextKey>
	{
		public static LocalizeTextKey GetEntry(string table, string entry) => new LocalizeTextKey(table, entry);

		public static LocalizeTextKey GetEntry(string entry) => new LocalizeTextKey(null, entry);

		public static LocalizeTextKey GetText(string text) => new LocalizeTextKey(text);

		public readonly bool RawText;
		public readonly string Table;
		public readonly string Entry;
		public readonly Dictionary<string, object> Dic;
		public readonly object Fallback;

		LocalizeTextKey(string text, Dictionary<string, object> dic = null, object fallback = null)
		{
			RawText = true;
			Table = null;
			Entry = text;
			Dic = dic;
			Fallback = fallback;
		}

		public LocalizeTextKey(string table, string entry)
		{
			RawText = false;
			Table = table;
			Entry = entry;
			Dic = null;
			Fallback = null;
		}

		public LocalizeTextKey(string table, string entry, Dictionary<string, object> dic, object fallback)
		{
			RawText = false;
			Table = table;
			Entry = entry;
			Dic = dic;
			Fallback = fallback;
		}

		public LocalizeTextKey WithParam(string key, object value)
		{
			var dic = Dic;
			if (dic == null)
			{
				dic = new Dictionary<string, object>();
			}
			dic[key] = value;
			if (RawText)
			{
				return new LocalizeTextKey(Entry, dic, fallback: Fallback);
			}
			else
			{
				return new LocalizeTextKey(Table, Entry, dic, Fallback);
			}
		}

		public LocalizeTextKey WithFallback(object fallback)
		{
			return new LocalizeTextKey(Table, Entry, Dic, fallback);
		}


		public override string ToString()
		{
			using var ret = new LocalizedString();
			ret.TableReference = Table;
			ret.TableEntryReference = Entry;
			if (Dic != null)
			{
				ret.Arguments = new object[] { Dic };
			}
			var text = ret.GetLocalizedString();
			if (string.IsNullOrEmpty(text) && Fallback != null)
			{
				text = Fallback?.ToString() ?? string.Empty;
			}
			return text;
		}

		public override bool Equals(object obj)
		{
			return obj is LocalizeTextKey key && Equals(key);
		}

		public bool Equals(LocalizeTextKey other)
		{
			return RawText == other.RawText &&
				   Table == other.Table &&
				   Entry == other.Entry &&
				   Dic == other.Dic &&
				   Fallback == other.Fallback;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(RawText, Table, Entry, Dic, Fallback);
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