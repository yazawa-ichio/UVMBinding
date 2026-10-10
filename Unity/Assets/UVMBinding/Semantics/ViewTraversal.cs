using System.Collections.Generic;
using UnityEngine;
using UVMBinding.Arguments;
using UVMBinding.Core;
using UVMBinding.PropertyProvider;

namespace UVMBinding.Semantics
{
	public static class ViewTraversal
	{
		public enum Mode
		{
			Static,
			Dynamic,
		}

		public struct Options
		{
			public Mode Mode;
			public bool ExcludeNoneRole;

			public System.Func<string, ViewModelDeclaration> ResolveDeclaration;

			public static Options ForExport => new Options { Mode = Mode.Dynamic, ExcludeNoneRole = true };

			public static Options ForStaticExport => new Options { Mode = Mode.Static, ExcludeNoneRole = true };

			public static Options ForLint => new Options { Mode = Mode.Static, ExcludeNoneRole = false };

			public static Options ForRuntimeLint => new Options { Mode = Mode.Dynamic, ExcludeNoneRole = false };
		}

		public static ViewNode Capture(View root, Options options)
		{
			if (root == null)
			{
				return null;
			}
			s_Paths.Clear();
			var views = root.GetComponentsInChildren<View>(true);
			var nodes = new Dictionary<View, ViewNode>();
			foreach (var view in views)
			{
				nodes[view] = CreateViewNode(root.transform, view, options);
			}
			foreach (var view in views)
			{
				var parent = FindParentView(root.transform, view, nodes);
				if (parent != null)
				{
					parent.Children.Add(nodes[view]);
				}
			}
			return nodes[root];
		}

		public static List<ViewNode> CaptureAll(GameObject root, Options options)
		{
			var result = new List<ViewNode>();
			if (root == null)
			{
				return result;
			}
			s_Paths.Clear();
			var views = root.GetComponentsInChildren<View>(true);
			var nodes = new Dictionary<View, ViewNode>();
			foreach (var view in views)
			{
				nodes[view] = CreateViewNode(root.transform, view, options);
			}
			foreach (var view in views)
			{
				var parent = FindParentView(root.transform, view, nodes);
				if (parent != null)
				{
					parent.Children.Add(nodes[view]);
				}
				else
				{
					result.Add(nodes[view]);
				}
			}
			return result;
		}

		static ViewNode FindParentView(Transform root, View view, Dictionary<View, ViewNode> nodes)
		{
			if (nodes == null)
			{
				return null;
			}
			for (var current = view.transform.parent; current != null; current = current.parent)
			{
				if (current.TryGetComponent<View>(out var parent) && nodes.TryGetValue(parent, out var node))
				{
					return node;
				}
				if (root != null && current == root)
				{
					break;
				}
			}
			return null;
		}

		public static ViewNode CaptureSelf(View view, Options options)
		{
			s_Paths.Clear();
			return view != null ? CreateViewNode(view.transform, view, options) : null;
		}

		static ViewNode CreateViewNode(Transform root, View view, Options options)
		{
			var node = new ViewNode
			{
				Id = GetPath(root, view.transform),
				Label = view.name,
				Source = view,
				ViewModelTypeName = GetViewModelTypeName(view, options),
				State = GetState(view),
			};
			var elements = new List<IViewElement>();
			view.GetElements(elements);
			var declaration = options.ResolveDeclaration?.Invoke(node.ViewModelTypeName);
			CollectDerived(view, declaration, node);
			BuildElements(root, ToDependsOnMap(node), declaration, elements, node, options);
			return node;
		}

		static string GetViewModelTypeName(View view, Options options)
		{
			if (options.Mode == Mode.Dynamic && view.ViewModel != null)
			{
				return view.ViewModel.GetType().FullName;
			}
			return view.ReservationViewModelName;
		}

		static void BuildElements(Transform root, Dictionary<string, string[]> dependsOn,
			ViewModelDeclaration declaration, List<IViewElement> elements, ViewNode node, Options options)
		{
			var byTransform = new Dictionary<Transform, List<ElementNode>>();
			var order = new List<Transform>();
			var governed = CollectGovernedState(elements, options);
			var dropped = new List<DroppedCandidate>();
			foreach (var element in elements)
			{
				if (!(element is Component component))
				{
					continue;
				}
				var role = SemanticRoleLookup.Get(element);
				if (options.ExcludeNoneRole && role == SemanticRole.None)
				{
					dropped.Add(new DroppedCandidate
					{
						Transform = component.transform,
						Governs = GovernedStateLookup.Get(element),
						Element = new DroppedElement
						{
							Id = GetPath(root, component.transform),
							BindPath = (element as IBinderInfo)?.Path,
							TypeName = element.GetType().Name,
						},
					});
					continue;
				}
				if (!byTransform.TryGetValue(component.transform, out var list))
				{
					byTransform[component.transform] = list = new List<ElementNode>();
					order.Add(component.transform);
				}
				var created = CreateElementNode(root, component, element, role, dependsOn, declaration, options);
				ApplyGovernedState(created, governed, component.transform);
				if (list.Count > 0)
				{
					created.Id += "#" + list.Count;
				}
				if (role != SemanticRole.None && list.Count > 0 && list[0].Role == SemanticRole.None)
				{
					list.Insert(0, created);
				}
				else
				{
					list.Add(created);
				}
			}
			foreach (var candidate in dropped)
			{
				if (candidate.Governs != GovernedState.None && byTransform.ContainsKey(candidate.Transform))
				{
					continue;
				}
				node.Dropped.Add(candidate.Element);
			}
			foreach (var transform in order)
			{
				var parent = FindParentElements(transform, byTransform);
				var list = byTransform[transform];
				if (parent != null)
				{
					parent[0].Children.AddRange(list);
				}
				else
				{
					node.Elements.AddRange(list);
				}
			}
		}

		static List<ElementNode> FindParentElements(Transform transform, Dictionary<Transform, List<ElementNode>> byTransform)
		{
			for (var current = transform.parent; current != null; current = current.parent)
			{
				if (byTransform.TryGetValue(current, out var list) && list.Count > 0)
				{
					return list;
				}
			}
			return null;
		}

		static ElementNode CreateElementNode(Transform root, Component component, IViewElement element,
			SemanticRole role, Dictionary<string, string[]> dependsOn, ViewModelDeclaration declaration,
			Options options)
		{
			var node = new ElementNode
			{
				Id = GetPath(root, component.transform),
				Role = role,
				Source = component,
				State = GetState(component),
			};
			if (element is IBinderInfo binder)
			{
				node.BindPath = binder.Path;
				node.Label = binder.GetDisplayText();
				node.HasConverter = binder.HasConverter;
				if (!string.IsNullOrEmpty(binder.Path) && dependsOn.TryGetValue(binder.Path, out var sources))
				{
					node.DependsOn = sources;
				}
				if (options.Mode == Mode.Dynamic)
				{
					node.Value = binder.GetBoundValue()?.ToString();
				}
			}
			if (element is IViewEvent viewEvent)
			{
				node.EventName = viewEvent.Name;
				ReadEventArgument(node, element);
				node.Consequence = declaration != null
					? declaration.GetConsequence(viewEvent.Name) : ConsequenceKind.Unknown;
			}
			if (element is IRowTemplateInfo rows && rows.RowTemplate != null
				&& root != null && rows.RowTemplate.transform.IsChildOf(root))
			{
				node.RowTemplateId = GetPath(root, rows.RowTemplate.transform);
			}
			return node;
		}

		static void ReadEventArgument(ElementNode node, IViewElement element)
		{
			if (!(element is IHasEventArgument holder))
			{
				return;
			}
			var argument = holder.GetArgument();
			if (argument == null)
			{
				return;
			}
			node.EventArgumentType = argument.GetType().Name;
			try
			{
				node.EventArgumentValue = argument.GetBoxedValue()?.ToString();
			}
			catch (System.Exception)
			{
				node.EventArgumentValue = null;
			}
		}

		static Dictionary<Transform, GovernedPaths> CollectGovernedState(List<IViewElement> elements,
			Options options)
		{
			Dictionary<Transform, GovernedPaths> map = null;
			foreach (var element in elements)
			{
				var governs = GovernedStateLookup.Get(element);
				if (governs == GovernedState.None || !(element is Component component)
					|| !(element is IBinderInfo binder))
				{
					continue;
				}
				map ??= new Dictionary<Transform, GovernedPaths>();
				map.TryGetValue(component.transform, out var paths);
				var value = options.Mode == Mode.Dynamic ? binder.GetBoundValue() as bool? : null;
				if (governs == GovernedState.Interactable)
				{
					paths.Interactable ??= binder.Path;
					paths.NotInteractable |= value == false;
				}
				else
				{
					paths.Visible ??= binder.Path;
					paths.NotVisible |= value == false;
				}
				map[component.transform] = paths;
			}
			return map;
		}

		static void ApplyGovernedState(ElementNode node, Dictionary<Transform, GovernedPaths> governed,
			Transform transform)
		{
			if (governed == null || !governed.TryGetValue(transform, out var paths))
			{
				return;
			}
			node.InteractablePath = paths.Interactable;
			node.VisiblePath = paths.Visible;
			if (paths.NotInteractable)
			{
				node.State |= ElementState.Disabled;
			}
			if (paths.NotVisible)
			{
				node.State |= ElementState.Hidden;
			}
		}

		struct DroppedCandidate
		{
			public Transform Transform;
			public GovernedState Governs;
			public DroppedElement Element;
		}

		struct GovernedPaths
		{
			public string Interactable;
			public string Visible;
			public bool NotInteractable;
			public bool NotVisible;
		}

		static readonly Dictionary<string, string[]> s_Empty = new Dictionary<string, string[]>();

		static void CollectDerived(View view, ViewModelDeclaration declaration, ViewNode node)
		{
			if (declaration?.DependsOn != null)
			{
				foreach (var pair in declaration.DependsOn)
				{
					node.Derived.Add(new DerivedValue { Path = pair.Key, Sources = pair.Value });
				}
			}
			foreach (var extension in view.GetComponents<ViewModelPropertyExtension>())
			{
				foreach (var info in extension.GetPropertyInfos())
				{
					if (info.SourcePaths.Length == 0)
					{
						continue;
					}
					node.Derived.Add(new DerivedValue
					{
						Path = info.Path,
						Sources = info.SourcePaths,
						Relation = info.Relation,
						Negated = info.Negated,
						Format = info.Format,
					});
				}
			}
		}

		static Dictionary<string, string[]> ToDependsOnMap(ViewNode node)
		{
			if (node.Derived.Count == 0)
			{
				return s_Empty;
			}
			var map = new Dictionary<string, string[]>();
			foreach (var derived in node.Derived)
			{
				map[derived.Path] = derived.Sources;
			}
			return map;
		}

		static ElementState GetState(Component component)
		{
			var state = ElementState.None;
			if (!component.gameObject.activeInHierarchy)
			{
				state |= ElementState.Hidden;
			}
			if (component is Behaviour behaviour && !behaviour.enabled)
			{
				state |= ElementState.Disabled;
			}
			return state;
		}

		static readonly Dictionary<Transform, string> s_Paths = new Dictionary<Transform, string>();

		static string GetPath(Transform root, Transform transform)
		{
			if (s_Paths.TryGetValue(transform, out var cached))
			{
				return cached;
			}
			return s_Paths[transform] = BuildPath(root, transform);
		}

		static string BuildPath(Transform root, Transform transform)
		{
			var parts = new List<string>();
			parts.Add(NameOf(transform));
			if (transform != root)
			{
				for (var parent = transform.parent; parent != null; parent = parent.parent)
				{
					parts.Add(NameOf(parent));
					if (parent == root)
					{
						break;
					}
				}
			}
			var sb = new System.Text.StringBuilder();
			for (int i = parts.Count - 1; i >= 0; i--)
			{
				if (sb.Length > 0)
				{
					sb.Append('/');
				}
				sb.Append(parts[i]);
			}
			return sb.ToString();
		}

		static string NameOf(Transform transform)
		{
			var parent = transform.parent;
			if (parent == null)
			{
				return transform.name.Replace('/', '_');
			}
			int index = 0;
			int same = 0;
			for (int i = 0; i < parent.childCount; i++)
			{
				var child = parent.GetChild(i);
				if (child.name != transform.name)
				{
					continue;
				}
				if (child == transform)
				{
					index = same;
				}
				same++;
			}
			var name = transform.name.Replace('/', '_');
			return same > 1 ? $"{name}[{index}]" : name;
		}
	}
}
