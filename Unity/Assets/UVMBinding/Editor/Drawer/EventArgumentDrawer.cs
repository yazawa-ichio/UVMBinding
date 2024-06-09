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
			var select = EditorGUI.Popup(GetPopupPosition(position), index, EventArgumentList.GetDispNames());
			if (index != select)
			{
				var type = EventArgumentList.GetType(select);
				if (type != null)
				{
					property.managedReferenceValue = Activator.CreateInstance(type);
				}
				else
				{
					property.managedReferenceValue = null;
				}
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