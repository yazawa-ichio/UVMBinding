using System.Collections.Generic;
using UnityEngine;
using UVMBinding.Converters;
using UVMBinding.Core;
using UVMBinding.PropertyProvider;
using UVMBinding.Drawer;
using UVMBinding.Semantics;

namespace UVMBinding.Lint
{
	public static class ViewLinter
	{
		static LintSettings s_Settings;

		public static LintSettings Settings
		{
			get => s_Settings != null ? s_Settings : s_Settings = LintSettings.Find();
			set => s_Settings = value;
		}

		public static void Lint(View view, string assetPath, LintReport report)
		{
			if (view == null)
			{
				return;
			}
			var objectPath = GetPath(view.transform);

			var ignore = FindIgnore(view.transform);
			if (ignore != null)
			{
				report.Ignored.Add(new LintSkipped(assetPath, objectPath, ignore));
				return;
			}

			var typeName = view.ReservationViewModelName;
			if (string.IsNullOrEmpty(typeName))
			{
				report.Skipped.Add(new LintSkipped(assetPath, objectPath, "Bind source type is not declared"));
				return;
			}
			var type = BindSourceType.Find(typeName);
			if (type == null)
			{
				report.Skipped.Add(new LintSkipped(assetPath, objectPath, $"Declared type not found: {typeName}"));
				return;
			}
			LintView(view, type, assetPath, objectPath, ViewTraversal.Options.ForLint, report);
		}

		public static void LintRuntime(View view, LintReport report)
		{
			if (view == null || view.ViewModel == null)
			{
				return;
			}
			var objectPath = GetPath(view.transform);
			var ignore = FindIgnore(view.transform);
			if (ignore != null)
			{
				report.Ignored.Add(new LintSkipped(string.Empty, objectPath, ignore));
				return;
			}
			var type = GetRuntimeSourceType(view.ViewModel);
			LintDeclaredType(view, type, objectPath, report);
			LintView(view, type, string.Empty, objectPath, ViewTraversal.Options.ForRuntimeLint, report);
		}

		static void LintDeclaredType(View view, System.Type runtimeType, string objectPath, LintReport report)
		{
			var declaredName = view.ReservationViewModelName;
			if (string.IsNullOrEmpty(declaredName))
			{
				return;
			}
			var declared = BindSourceType.Find(declaredName);
			if (declared == null || declared.IsAssignableFrom(runtimeType))
			{
				return;
			}
			report.Add(new LintViolation(string.Empty, objectPath, LintRule.DeclaredTypeMismatch,
				$"Declared type does not match the attached type: declared {declared.Name} -> attached {runtimeType.Name}",
				view));
		}

		static System.Type GetRuntimeSourceType(IViewModel viewModel)
		{
			if (viewModel is ReflectionViewModel reflection && reflection.DataSource != null)
			{
				return reflection.DataSource.GetType();
			}
			return viewModel.GetType();
		}

		static void LintView(View view, System.Type type, string assetPath, string objectPath,
			ViewTraversal.Options options, LintReport report)
		{
			var source = new ExtendedBindSource(BindSourceType.Create(type));
			var extensions = view.GetComponentsInChildren<ViewModelPropertyExtension>(true);
			foreach (var extension in extensions)
			{
				foreach (var info in extension.GetPropertyInfos())
				{
					source.Add(info);
				}
			}

			var node = ViewTraversal.CaptureSelf(view, options);
			var elements = new List<IViewElement>();
			CollectSelf(node, elements);

			if (source.IsDynamic)
			{
				report.Partial.Add(new LintSkipped(assetPath, objectPath,
					$"Path-dependent rules skipped because paths are dynamic: {type.FullName}"));
			}
			else
			{
				report.CheckedViewCount++;
			}
			CollectDispatchedEvents(elements, type, report);
			LintElements(elements, view.transform, assetPath, source, report);
			LintExtensions(extensions, assetPath, source, report);
			LintDependsOn(type, assetPath, objectPath, source, report);
		}

		static void CollectDispatchedEvents(List<IViewElement> elements, System.Type type, LintReport report)
		{
			foreach (var element in elements)
			{
				if (element is IViewEvent viewEvent)
				{
					report.AddDispatchedEvent(type, viewEvent.Name);
				}
			}
		}

		public static void LintUnusedEvents(LintReport report)
		{
			foreach (var pair in report.DispatchedEvents)
			{
				var type = pair.Key;
				if (type == null || FindTypeIgnore(type) != null
					|| type.GetCustomAttributes(typeof(DynamicPathAttribute), true).Length > 0)
				{
					continue;
				}
				foreach (var entry in ViewModelData.Get(type).Events)
				{
					if (pair.Value.Contains(entry.Path))
					{
						continue;
					}
					var ignore = FindMemberIgnore(entry);
					if (ignore != null)
					{
						report.Ignored.Add(new LintSkipped(string.Empty, $"{type.FullName}.{entry.Path}", ignore));
						continue;
					}
					report.Add(new LintViolation(string.Empty, type.FullName, LintRule.UnusedEventName,
						$"Event is not dispatched by any view: {entry.Path}"));
				}
			}
		}

		static readonly string[] s_ConsequenceWords =
		{
			"Purchase", "Buy", "Pay", "Delete", "Remove", "Clear", "Reset", "Destroy",
		};

		public static void LintConsequences(LintReport report)
		{
			foreach (var pair in report.DispatchedEvents)
			{
				var type = pair.Key;
				if (type == null || FindTypeIgnore(type) != null)
				{
					continue;
				}
				foreach (var entry in ViewModelData.Get(type).Events)
				{
					if (entry.Consequence != ConsequenceKind.Unknown || !SuggestsConsequence(entry.Path))
					{
						continue;
					}
					var ignore = FindMemberIgnore(entry);
					if (ignore != null)
					{
						report.Ignored.Add(new LintSkipped(string.Empty, $"{type.FullName}.{entry.Path}", ignore));
						continue;
					}
					report.Add(new LintViolation(string.Empty, type.FullName, LintRule.MissingConsequence,
						$"Name suggests a consequence but has no [Consequence]: {entry.Path}"));
				}
			}
		}

		static bool SuggestsConsequence(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return false;
			}
			foreach (var word in s_ConsequenceWords)
			{
				if (name.IndexOf(word, System.StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return true;
				}
			}
			return false;
		}

		static string FindMemberIgnore(ViewModelData.EventEntry entry)
		{
			var member = (System.Reflection.MemberInfo)entry.Info ?? entry.Property;
			if (member == null)
			{
				return null;
			}
			var attributes = member.GetCustomAttributes(typeof(LintIgnoreAttribute), true);
			return attributes.Length > 0 ? ((LintIgnoreAttribute)attributes[0]).Reason : null;
		}

		public static void Lint(ViewModelSourceProvider provider, string assetPath, LintReport report)
		{
			if (provider == null)
			{
				return;
			}
			var objectPath = GetPath(provider.transform);

			var ignore = FindIgnore(provider.transform);
			if (ignore == null)
			{
				ignore = FindTypeIgnore(provider.GetType());
			}
			if (ignore != null)
			{
				report.Ignored.Add(new LintSkipped(assetPath, objectPath, ignore));
				return;
			}

			var declared = GetDeclaredSourceType(provider.GetType());
			if (declared == null)
			{
				report.Skipped.Add(new LintSkipped(assetPath, objectPath,
					"Bind source type is not declared (generic base class is not used)"));
				return;
			}

			var source = BindSourceType.Create(declared);

			var elements = new List<IViewElement>();
			foreach (var element in provider.GetComponents<IViewElement>())
			{
				if (element is Component component && provider.IsTarget(component))
				{
					elements.Add(element);
				}
			}

			if (source.IsDynamic)
			{
				report.Partial.Add(new LintSkipped(assetPath, objectPath,
					$"Path-dependent rules skipped because paths are dynamic: {declared.FullName}"));
			}
			else
			{
				report.CheckedProviderCount++;
			}
			CollectDispatchedEvents(elements, declared, report);
			LintElements(elements, provider.transform, assetPath, source, report);
		}

		static void CollectSelf(ViewNode node, List<IViewElement> elements)
		{
			foreach (var element in node.Elements)
			{
				Collect(element, elements);
			}
		}

		static void Collect(ElementNode node, List<IViewElement> elements)
		{
			if (node.Source is IViewElement element)
			{
				elements.Add(element);
			}
			foreach (var child in node.Children)
			{
				Collect(child, elements);
			}
		}

		public static System.Type GetDeclaredSourceType(System.Type providerType)
		{
			for (var type = providerType; type != null; type = type.BaseType)
			{
				if (!type.IsGenericType)
				{
					continue;
				}
				var definition = type.GetGenericTypeDefinition();
				if (definition == typeof(DataSourceProvider<>) || definition == typeof(ViewModelSourceProvider<>))
				{
					return type.GetGenericArguments()[0];
				}
			}
			return null;
		}

		static void LintElements(List<IViewElement> elements, Transform owner, string assetPath, IBindSource source, LintReport report)
		{
			foreach (var element in elements)
			{
				var elementTransform = (element as Component)?.transform;
				var objectPath = elementTransform != null ? GetPath(elementTransform) : GetPath(owner);

				var ignore = elementTransform != null ? FindIgnore(elementTransform) : null;
				if (ignore == null)
				{
					ignore = FindTypeIgnore(element.GetType());
				}
				if (ignore != null)
				{
					report.Ignored.Add(new LintSkipped(assetPath, objectPath, ignore));
					continue;
				}

				LintTargetCompatibility(element, assetPath, objectPath, report);
				LintNaming(element, assetPath, objectPath, report);

				if (element is IBinderInfo binder && !string.IsNullOrEmpty(binder.Path) && !source.IsDynamic)
				{
					if (!source.HasBind(binder.Path))
					{
						report.Add(new LintViolation(assetPath, objectPath, LintRule.UnknownBindPath,
							$"Bind path does not exist: {binder.Path}", element as Component));
					}
					else
					{
						LintType(binder, source.GetDeclaredType(binder.Path), assetPath, objectPath,
							element as Component, report);
						LintConverterStages(binder, assetPath, objectPath, element as Component, report);
					}
				}

				if (element is IViewEvent viewEvent && !string.IsNullOrEmpty(viewEvent.Name))
				{
					if (!source.IsDynamic && !source.HasEvent(viewEvent.Name, viewEvent.EventType()))
					{
						report.Add(new LintViolation(assetPath, objectPath, LintRule.UnknownEventName,
							$"Event name is not subscribed: {viewEvent.Name}", element as Component));
					}
				}
			}
		}

		static void LintNaming(IViewElement element, string assetPath, string objectPath, LintReport report)
		{
			var settings = Settings;
			if (element is IBinderInfo binder && !string.IsNullOrEmpty(binder.Path))
			{
				NamingLinter.LintName(binder.Path, assetPath, objectPath, settings, report);
			}
			if (element is IViewEvent viewEvent)
			{
				NamingLinter.LintEventName(viewEvent, assetPath, objectPath, settings, report);
			}
			if (!settings.CheckMissingLabel)
			{
				return;
			}
			var role = SemanticRoleLookup.Get(element);
			if (!NamingLinter.IsInteractive(role))
			{
				return;
			}
			if (!NamingLinter.HasLabel(element as Component))
			{
				report.Add(new LintViolation(assetPath, objectPath, LintRule.MissingLabel,
					$"Interactive element has no label: {role}"));
			}
		}

		static void LintTargetCompatibility(IViewElement element, string assetPath, string objectPath, LintReport report)
		{
			if (!(element is Component component))
			{
				return;
			}
			var attributes = element.GetType().GetCustomAttributes(typeof(LintIncompatibleTargetAttribute), true);
			foreach (LintIncompatibleTargetAttribute attribute in attributes)
			{
				if (attribute.Type == null || component.GetComponent(attribute.Type) == null)
				{
					continue;
				}
				report.Add(new LintViolation(assetPath, objectPath, LintRule.IncompatibleTarget,
					$"{element.GetType().Name} does not work correctly on {attribute.Type.Name}: {attribute.Reason}", component));
			}
		}

		static void LintType(IBinderInfo binder, System.Type declared, string assetPath, string objectPath,
			Component source, LintReport report)
		{
			if (declared == null)
			{
				return;
			}
			var required = binder.BindType;
			if (binder.HasConverter)
			{
				required = binder.ConverterInputType;
				if (required == null)
				{
					return;
				}
			}
			if (required == null || Connects(declared, required, binder.AllowCast))
			{
				return;
			}
			report.Add(new LintViolation(assetPath, objectPath, LintRule.UnconvertibleType,
				$"Type mismatch with no converter: {binder.Path} declared {declared.Name} -> required {required.Name}", source));
		}

		static void LintConverterStages(IBinderInfo binder, string assetPath, string objectPath,
			Component source, LintReport report)
		{
			var stages = new List<ConverterStage>();
			binder.GetConverterStages(stages);
			if (stages.Count == 0)
			{
				return;
			}
			for (int i = 0; i < stages.Count; i++)
			{
				if (stages[i].IsEmpty)
				{
					report.Add(new LintViolation(assetPath, objectPath, LintRule.ConverterStageMismatch,
						$"Converter stage {i} is empty: {binder.Path}", source));
					continue;
				}
				var from = stages[i].OutputType;
				var to = i == stages.Count - 1 ? binder.BindType : stages[i + 1].InputType;
				if (to == null || Connects(from, to, binder.AllowCast))
				{
					continue;
				}
				var place = i == stages.Count - 1 ? "output" : $"stage {i} -> {i + 1}";
				report.Add(new LintViolation(assetPath, objectPath, LintRule.ConverterStageMismatch,
					$"Converter {place} type does not connect: {from.Name} -> {to.Name} ({binder.Path})", source));
			}
		}

		static bool Connects(System.Type from, System.Type to, bool allowCast)
		{
			if (to.IsAssignableFrom(from) || CastRegistry.IsImplicit(from, to))
			{
				return true;
			}
			return allowCast && CastRegistry.IsRegistered(from, to);
		}

		static void LintDependsOn(System.Type type, string assetPath, string objectPath, IBindSource source, LintReport report)
		{
			if (source.IsDynamic || !typeof(IViewModel).IsAssignableFrom(type))
			{
				return;
			}
			foreach (var entry in ViewModelData.Get(type).Properties)
			{
				foreach (var sourcePath in entry.SourcePaths)
				{
					if (string.IsNullOrEmpty(sourcePath) || source.HasBind(sourcePath))
					{
						continue;
					}
					report.Add(new LintViolation(assetPath, objectPath, LintRule.UnknownSourcePath,
						$"Source path does not exist: {entry.Path} <- {sourcePath}"));
				}
			}
		}

		static void LintExtensions(ViewModelPropertyExtension[] extensions, string assetPath, IBindSource source, LintReport report)
		{
			foreach (var extension in extensions)
			{
				var objectPath = GetPath(extension.transform);
				foreach (var info in extension.GetPropertyInfos())
				{
					foreach (var sourcePath in info.SourcePaths)
					{
						if (string.IsNullOrEmpty(sourcePath))
						{
							continue;
						}
						if (!source.IsDynamic && !source.HasBind(sourcePath))
						{
							report.Add(new LintViolation(assetPath, objectPath, LintRule.UnknownSourcePath,
								$"Source path does not exist: {info.Path} <- {sourcePath}"));
						}
					}
				}
			}
		}

		static string FindIgnore(Transform transform)
		{
			for (var current = transform; current != null; current = current.parent)
			{
				var ignore = current.GetComponent<LintIgnore>();
				if (ignore != null && ignore.IsValid)
				{
					return ignore.Reason;
				}
			}
			return null;
		}

		static string FindTypeIgnore(System.Type type)
		{
			var attributes = type.GetCustomAttributes(typeof(LintIgnoreAttribute), true);
			if (attributes.Length > 0)
			{
				return ((LintIgnoreAttribute)attributes[0]).Reason;
			}
			return null;
		}

		static string GetPath(Transform transform)
		{
			var sb = new System.Text.StringBuilder(transform.name);
			for (var parent = transform.parent; parent != null; parent = parent.parent)
			{
				sb.Insert(0, '/').Insert(0, parent.name);
			}
			return sb.ToString();
		}
	}
}
