using System;
using UnityEditor;
using UnityEngine;
using UVMBinding.Arguments;

namespace UVMBinding.Drawer
{
	[CustomPropertyDrawer(typeof(EventArgument))]
	public class EventArgumentDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (property.hasMultipleDifferentValues)
			{
				EditorGUI.LabelField(position, label, new GUIContent("Multiple different values not support"));
				return;
			}
			var index = EventArgumentList.GetIndex(property.managedReferenceFullTypename);
			var names = EventArgumentList.GetDispNames();
			var popupPosition = GetPopupPosition(position);
			if (EditorGUI.DropdownButton(popupPosition, new GUIContent(names[index]), FocusType.Passive))
			{
				var serialized = property.serializedObject;
				var propertyPath = property.propertyPath;
				SearchableDropdown.Show(popupPosition, "Event Argument", names, select =>
				{
					serialized.Update();
					var target = serialized.FindProperty(propertyPath);
					var type = EventArgumentList.GetType(select);
					target.managedReferenceValue = type != null ? Activator.CreateInstance(type) : null;
					serialized.ApplyModifiedProperties();
				});
			}
			if (property.hasMultipleDifferentValues)
			{
				return;
			}
			EditorGUI.PropertyField(position, property, label, true);
		}

		private Rect GetPopupPosition(Rect currentPosition)
		{
			Rect popupPosition = new(currentPosition);
			popupPosition.width -= EditorGUIUtility.labelWidth;
			popupPosition.x += EditorGUIUtility.labelWidth;
			popupPosition.height = EditorGUIUtility.singleLineHeight;
			return popupPosition;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (property.hasMultipleDifferentValues)
			{
				return EditorGUIUtility.singleLineHeight;
			}
			return EditorGUI.GetPropertyHeight(property, true);
		}

	}
}