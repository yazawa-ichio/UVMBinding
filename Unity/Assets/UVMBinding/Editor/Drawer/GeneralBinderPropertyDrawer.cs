using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UVMBinding.Drawer
{
	[CustomPropertyDrawer(typeof(GeneralBinderPropertyAttribute))]
	class GeneralBinderPropertyDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var left = position;
			var right = position;
			left.width -= 20;
			right.x += left.width;
			right.width = 20;
			EditorGUI.PropertyField(left, property, label);
			if (EditorGUI.DropdownButton(right, GUIContent.none, FocusType.Passive))
			{
				if (!DrawerMultiSelect.TryResolveShared(property.serializedObject.targetObjects,
					FindTargetType, out var targetType, out var reason))
				{
					DrawerMultiSelect.ShowReason(right, reason);
					return;
				}
				{
					var names = new List<string>();
					var targets = new List<string>();
					ReflectionCache.Get(targetType).Collect(true, names, targets);
					var serialized = property.serializedObject;
					var propertyPath = property.propertyPath;
					SearchableDropdown.Show(right, "Target", names, index =>
					{
						serialized.Update();
						serialized.FindProperty(propertyPath).stringValue = targets[index];
						serialized.ApplyModifiedProperties();
					});
				}
			}
		}

		static Type FindTargetType(UnityEngine.Object obj)
		{
			if (!(obj is GeneralBinder binder))
			{
				return null;
			}
			var target = binder.GetTarget();
			return target != null ? target.GetType() : null;
		}

		class ReflectionCache
		{
			static Dictionary<Type, ReflectionCache> s_Cache = new();

			public static ReflectionCache Get(Type type)
			{
				if (!s_Cache.TryGetValue(type, out var cache))
				{
					var baseData = type.BaseType != null ? Get(type.BaseType) : null;
					s_Cache[type] = cache = new ReflectionCache(type, baseData);
				}
				return cache;
			}

			public readonly Type Type;
			public readonly ReflectionCache BaseData;
			public readonly List<FieldInfo> Fields = new();
			public readonly List<PropertyInfo> Properties = new();
			public readonly List<MethodInfo> Methods = new();

			public readonly List<string> TargetNames = new();

			private ReflectionCache(Type type, ReflectionCache baseData)
			{
				Type = type;
				BaseData = baseData;
				const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
				foreach (var field in type.GetFields(flags))
				{
					if (field.IsInitOnly) continue;

					Fields.Add(field);
				}
				foreach (var property in type.GetProperties(flags))
				{
					if (property.CanWrite)
					{
						Properties.Add(property);
					}
				}
				foreach (var method in type.GetMethods(flags))
				{
					if (method.GetParameters().Length == 1)
					{
						Methods.Add(method);
					}
				}

				TargetNames.AddRange(Fields.Select(x => x.Name));
				TargetNames.AddRange(Properties.Select(x => x.Name));
				TargetNames.AddRange(Methods.Select(x => x.Name));
			}

			public void Collect(bool root, List<string> names, List<string> targets)
			{
				foreach (var name in TargetNames)
				{
					names.Add(root ? name : $"{Type.Name}/{name}");
					targets.Add(name);
				}
				BaseData?.Collect(false, names, targets);
			}

		}
	}
}