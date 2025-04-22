using UnityEditor;
using UnityEngine;
using UVMBinding.Core;

namespace UVMBinding.Drawer
{
	[CustomPropertyDrawer(typeof(BindSourceTargetData))]
	class BindSourceProviderSelectDrawer : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (property.serializedObject.targetObjects.Length == 1)
			{
				var typeProperty = property.FindPropertyRelative(nameof(BindSourceTargetData.Type));
				if (typeProperty.enumValueIndex == (int)BindSourceTargetData.TargetType.All)
				{
					return base.GetPropertyHeight(property, label);
				}
				var provider = property.serializedObject.targetObject as BindSourceProvider;
				using (ListPool<IBinder>.Use(out var list))
				{
					provider.GetComponents<IBinder>(list);
					return (list.Count + 1) * EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
				}
			}
			return base.GetPropertyHeight(property, label);
		}

		GUIContent m_Temp = new GUIContent("Bind Source");

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (property.serializedObject.targetObjects.Length != 1)
			{
				m_Temp.text = "Multiple selection not allowed";
				EditorGUI.LabelField(position, label, m_Temp);
				return;
			}
			var provider = property.serializedObject.targetObject as BindSourceProvider;
			using var _ = ListPool<IBinder>.Use(out var list);
			provider.GetComponents<IBinder>(list);

			var targets = property.FindPropertyRelative(nameof(BindSourceTargetData.Targets));
			for (int i = 0; i < targets.arraySize; i++)
			{
				var item = targets.GetArrayElementAtIndex(i);
				if (item.objectReferenceValue == null)
				{
					targets.DeleteArrayElementAtIndex(i);
					i--;
				}
			}

			var rect = position;
			rect.height = EditorGUIUtility.singleLineHeight;
			rect.y += EditorGUIUtility.standardVerticalSpacing;
			var typeProperty = property.FindPropertyRelative(nameof(BindSourceTargetData.Type));
			EditorGUI.PropertyField(rect, typeProperty, label);
			if (typeProperty.enumValueIndex == (int)BindSourceTargetData.TargetType.All)
			{
				return;
			}
			using var indent = new EditorGUI.IndentLevelScope();
			for (int i = 0; i < list.Count; i++)
			{
				IBinder item = list[i];
				if (item == null) continue;
				var isTarget = provider.IsTarget(item as Component);
				rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
				if (isTarget != EditorGUI.ToggleLeft(rect, item.GetType().Name, isTarget))
				{
					if (isTarget)
					{
						for (int j = 0; j < targets.arraySize; j++)
						{
							var target = targets.GetArrayElementAtIndex(j);
							if (target.objectReferenceValue == (Component)item)
							{
								targets.DeleteArrayElementAtIndex(j);
								break;
							}
						}
					}
					else
					{
						targets.InsertArrayElementAtIndex(targets.arraySize);
						var newItem = targets.GetArrayElementAtIndex(i);
						newItem.objectReferenceValue = item as Component;
					}
				}
			}
		}
	}
}