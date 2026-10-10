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
					var types = new System.Collections.Generic.List<Type>();
					foreach (var type in TypeCache.GetTypesDerivedFrom<IPropertyProvider>())
					{
						if (!type.IsAbstract)
						{
							types.Add(type);
						}
					}
					var names = types.ConvertAll(x => x.Name);
					var serialized = property.serializedObject;
					var propertyPath = property.propertyPath;
					SearchableDropdown.Show(position, "Property Provider", names, index =>
					{
						serialized.Update();
						serialized.FindProperty(propertyPath).managedReferenceValue = Activator.CreateInstance(types[index]);
						serialized.ApplyModifiedProperties();
					});
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