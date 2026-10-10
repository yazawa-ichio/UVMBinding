using System;
using System.Collections.Generic;
using UnityEngine;

namespace UVMBinding.Drawer
{
	public static class DrawerMultiSelect
	{
		public const string Mixed = "Bind source does not match across selection";
		public const string NotFound = "Bind source not found";
		public const string SingleOnly = "Multiple selection not allowed";

		public static bool TryResolveShared<T>(IEnumerable<UnityEngine.Object> targets,
			Func<UnityEngine.Object, T> resolve, out T shared, out string reason) where T : class
		{
			shared = null;
			reason = null;
			bool found = false;
			foreach (var target in targets)
			{
				var value = target != null ? resolve(target) : null;
				if (value == null)
				{
					reason = NotFound;
					shared = null;
					return false;
				}
				if (!found)
				{
					shared = value;
					found = true;
				}
				else if (!shared.Equals(value))
				{
					reason = Mixed;
					shared = null;
					return false;
				}
			}
			if (!found)
			{
				reason = NotFound;
				return false;
			}
			return true;
		}

		public static void ShowReason(Rect rect, string reason)
		{
			SearchableDropdown.Show(rect, "Not available", new List<string> { reason }, _ => { });
		}
	}
}
