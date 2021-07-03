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
			EditorGUI.PropertyField(position, property, label, true);
		}

		private Rect GetPopupPosition(Rect currentPosition)
		{
			Rect popupPosition = new Rect(currentPosition);
			popupPosition.width -= EditorGUIUtility.labelWidth;
			popupPosition.x += EditorGUIUtility.labelWidth;
			popupPosition.height = EditorGUIUtility.singleLineHeight;
			return popupPosition;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property, true);
		}

	}
}