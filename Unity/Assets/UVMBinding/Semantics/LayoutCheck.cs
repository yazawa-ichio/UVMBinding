using System.Collections.Generic;
using UnityEngine;

namespace UVMBinding.Semantics
{
	public enum LayoutIssueKind
	{
		ZeroSize,
		Overlap,
	}

	public readonly struct LayoutIssue
	{
		public readonly LayoutIssueKind Kind;
		public readonly string Id;
		public readonly string Detail;

		public LayoutIssue(LayoutIssueKind kind, string id, string detail)
		{
			Kind = kind;
			Id = id;
			Detail = detail;
		}

		public override string ToString() => $"{Kind} {Id} {Detail}";
	}

	public static class LayoutCheck
	{
		const float Tolerance = 1f;

		public static int Run(ViewNode node, List<LayoutIssue> issues)
		{
			if (node == null || issues == null)
			{
				return 0;
			}
			var elements = new List<ElementNode>();
			Flatten(node, elements);
			var checkedCount = 0;
			var texts = new List<Placed>();
			foreach (var element in elements)
			{
				var rect = Rect(element);
				if (rect == null || (element.State & ElementState.Hidden) != 0)
				{
					continue;
				}
				checkedCount++;
				CheckZeroSize(element, rect, issues);
				if (element.Role == SemanticRole.Text)
				{
					texts.Add(new Placed(element, rect, WorldRect(rect)));
				}
			}
			CheckOverlap(texts, issues);
			return checkedCount;
		}

		static void Flatten(ViewNode node, List<ElementNode> result)
		{
			foreach (var element in node.Elements)
			{
				Flatten(element, result);
			}
			foreach (var child in node.Children)
			{
				Flatten(child, result);
			}
		}

		static void Flatten(ElementNode element, List<ElementNode> result)
		{
			result.Add(element);
			foreach (var child in element.Children)
			{
				Flatten(child, result);
			}
		}

		readonly struct Placed
		{
			public readonly ElementNode Node;
			public readonly Transform Parent;
			public readonly Rect World;

			public Placed(ElementNode node, RectTransform rect, Rect world)
			{
				Node = node;
				Parent = rect.parent;
				World = world;
			}
		}

		static RectTransform Rect(ElementNode element)
		{
			return element.Source != null ? element.Source.transform as RectTransform : null;
		}

		static void CheckZeroSize(ElementNode element, RectTransform target, List<LayoutIssue> issues)
		{
			var rect = target.rect;
			if (rect.width > Tolerance && rect.height > Tolerance)
			{
				return;
			}
			issues.Add(new LayoutIssue(LayoutIssueKind.ZeroSize, element.Id,
				$"{rect.width:0.0}x{rect.height:0.0}"));
		}

		static void CheckOverlap(List<Placed> texts, List<LayoutIssue> issues)
		{
			if (texts.Count < 2)
			{
				return;
			}
			var byParent = new Dictionary<Transform, List<Placed>>();
			foreach (var text in texts)
			{
				if (!byParent.TryGetValue(text.Parent, out var list))
				{
					byParent[text.Parent] = list = new List<Placed>();
				}
				list.Add(text);
			}
			foreach (var pair in byParent)
			{
				var group = pair.Value;
				for (int i = 0; i < group.Count; i++)
				{
					for (int j = i + 1; j < group.Count; j++)
					{
						if (!Overlaps(group[i].World, group[j].World))
						{
							continue;
						}
						issues.Add(new LayoutIssue(LayoutIssueKind.Overlap,
							group[i].Node.Id, group[j].Node.Id));
					}
				}
			}
		}

		static bool Overlaps(Rect a, Rect b)
		{
			return a.xMax - Tolerance > b.xMin && b.xMax - Tolerance > a.xMin
				&& a.yMax - Tolerance > b.yMin && b.yMax - Tolerance > a.yMin;
		}

		static readonly Vector3[] s_Corners = new Vector3[4];

		static Rect WorldRect(RectTransform rect)
		{
			rect.GetWorldCorners(s_Corners);
			return new Rect(s_Corners[0].x, s_Corners[0].y,
				s_Corners[2].x - s_Corners[0].x, s_Corners[2].y - s_Corners[0].y);
		}
	}
}
