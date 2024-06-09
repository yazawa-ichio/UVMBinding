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
				if (property.serializedObject.targetObject is GeneralBinder component)
				{
					var target = component.GetTarget();
					if (target == null) return;
					var menu = new GenericMenu();
					ReflectionCache.Get(target.GetType()).AddMenu(true, menu, (targetName) =>
					{
						property.serializedObject.Update();
						property.stringValue = targetName;
						property.serializedObject.ApplyModifiedProperties();
					});
					menu.ShowAsContext();
				}
			}
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

			public void AddMenu(bool root, GenericMenu menu, Action<string> select)
			{
				foreach (var name in TargetNames)
				{
					var target = name;
					var path = root ? name : $"{Type.Name}/{name}";
					menu.AddItem(new GUIContent(path), false, () =>
					{
						select(target);
					});
				}
				BaseData?.AddMenu(false, menu, select);
			}


		}
	}
}