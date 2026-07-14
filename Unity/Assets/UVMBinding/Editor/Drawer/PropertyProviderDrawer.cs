using System;
using UnityEditor;
using UnityEngine;
using UVMBinding.PropertyProvider;
namespace UVMBinding.Drawer
{
	[CustomPropertyDrawer(typeof(IPropertyProvider))]
	public class PropertyProviderDrawer : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property, true);
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			bool isEmpty = property.managedReferenceValue == null;
			if (isEmpty)
			{
				if (GUI.Button(position, "New Property"))
				{
					GenericMenu menu = new GenericMenu();
					foreach (var type in TypeCache.GetTypesDerivedFrom<IPropertyProvider>())
					{
						var _type = type;
						if (_type.IsAbstract)
							continue;
						menu.AddItem(new GUIContent(_type.Name), false, () =>
						{
							property.serializedObject.Update();
							property.managedReferenceValue = Activator.CreateInstance(_type);
							property.serializedObject.ApplyModifiedProperties();
						});
					}
					menu.ShowAsContext();
				}
			}
			else
			{
				label.text = property.managedReferenceValue.GetType().Name;
				EditorGUI.PropertyField(position, property, label, true);
			}
		}
	}
}