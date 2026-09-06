using System.Collections.Generic;
using UnityEngine;

namespace UVMBinding.Lint
{
	[CreateAssetMenu(fileName = "LintBaseline", menuName = "UVMBinding/Lint Baseline")]
	public class LintBaseline : ScriptableObject
	{
		[SerializeField]
		string m_Expires = default;

		[SerializeField, TextArea]
		string m_Reason = default;

		[SerializeField]
		List<string> m_Entries = new List<string>();

		public string Reason => m_Reason;

		public int Count => m_Entries.Count;

		public static string Key(LintViolation violation)
		{
			return $"{violation.AssetPath}:{violation.ObjectPath} [{violation.Rule}]";
		}

		public bool IsValid(System.DateTime now, out string reason)
		{
			if (string.IsNullOrWhiteSpace(m_Reason))
			{
				reason = "Baseline has no reason.";
				return false;
			}
			if (!System.DateTime.TryParse(m_Expires, out var expires))
			{
				reason = $"Baseline has no valid expiry date: '{m_Expires}'.";
				return false;
			}
			if (now.Date > expires.Date)
			{
				reason = $"Baseline expired on {expires:yyyy-MM-dd}.";
				return false;
			}
			reason = null;
			return true;
		}

		public bool Contains(LintViolation violation)
		{
			return m_Entries.Contains(Key(violation));
		}

		public void Replace(IReadOnlyList<LintViolation> violations)
		{
			m_Entries.Clear();
			foreach (var violation in violations)
			{
				var key = Key(violation);
				if (!m_Entries.Contains(key))
				{
					m_Entries.Add(key);
				}
			}
			m_Entries.Sort(System.StringComparer.Ordinal);
		}
	}
}
