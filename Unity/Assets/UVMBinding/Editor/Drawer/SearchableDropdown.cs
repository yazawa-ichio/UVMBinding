using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UVMBinding.Drawer
{
	public class SearchableDropdown : AdvancedDropdown
	{
		class Item : AdvancedDropdownItem
		{
			public readonly int Index;

			public Item(string name, int index) : base(name)
			{
				Index = index;
				id = index;
			}
		}

		static readonly Dictionary<string, AdvancedDropdownState> s_States = new Dictionary<string, AdvancedDropdownState>();

		readonly string m_Title;
		readonly IReadOnlyList<string> m_Items;
		readonly System.Action<int> m_Select;

		const float MinWidth = 400f;
		const float MinHeight = 250f;

		SearchableDropdown(AdvancedDropdownState state, string title, IReadOnlyList<string> items, System.Action<int> select)
			: base(state)
		{
			m_Title = title;
			m_Items = items;
			m_Select = select;
			minimumSize = new Vector2(MinWidth, MinHeight);
		}

		public static void Show(Rect rect, string title, IReadOnlyList<string> items, System.Action<int> select,
			string key = null)
		{
			if (items == null || items.Count == 0)
			{
				return;
			}
			var stateKey = key ?? title;
			if (!s_States.TryGetValue(stateKey, out var state))
			{
				s_States[stateKey] = state = new AdvancedDropdownState();
			}
			new SearchableDropdown(state, title, items, select).Show(GetPosition(rect));
		}

		static Rect GetPosition(Rect rect)
		{
			var current = UnityEngine.Event.current;
			if (current == null)
			{
				return rect;
			}
			var position = rect;
			position.position = current.mousePosition;
			position.height = 0f;
			return position;
		}

		protected override AdvancedDropdownItem BuildRoot()
		{
			var root = new AdvancedDropdownItem(m_Title);
			var groups = new Dictionary<string, AdvancedDropdownItem>();
			for (int i = 0; i < m_Items.Count; i++)
			{
				var path = m_Items[i];
				var parent = root;
				var separator = path.LastIndexOf('/');
				if (separator >= 0)
				{
					parent = GetGroup(root, groups, path.Substring(0, separator));
					path = path.Substring(separator + 1);
				}
				parent.AddChild(new Item(path, i));
			}
			return root;
		}

		static AdvancedDropdownItem GetGroup(AdvancedDropdownItem root, Dictionary<string, AdvancedDropdownItem> groups, string path)
		{
			if (groups.TryGetValue(path, out var group))
			{
				return group;
			}
			var parent = root;
			var name = path;
			var separator = path.LastIndexOf('/');
			if (separator >= 0)
			{
				parent = GetGroup(root, groups, path.Substring(0, separator));
				name = path.Substring(separator + 1);
			}
			group = new AdvancedDropdownItem(name);
			parent.AddChild(group);
			groups[path] = group;
			return group;
		}

		protected override void ItemSelected(AdvancedDropdownItem item)
		{
			if (item is Item leaf)
			{
				m_Select(leaf.Index);
			}
		}
	}
}
