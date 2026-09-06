using System.Linq;
using UnityEditor;
using UnityEngine;
using UVMBinding.Core;

namespace UVMBinding.Drawer
{
	[CustomPropertyDrawer(typeof(SelfComponentTargetAttribute))]
	class SelfComponentTargetDrawer : PropertyDrawer
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
				if (property.serializedObject.targetObjects.Length != 1)
				{
					DrawerMultiSelect.ShowReason(right, DrawerMultiSelect.SingleOnly);
					return;
				}
				if (property.serializedObject.targetObject is Component component)
				{
					var targets = component.GetComponents(fieldInfo.FieldType)
						.Where(x => (x is not IBinder && x != component))
						.ToArray();
					if (targets.Length == 0)
					{
						Debug.LogWarning($"{fieldInfo.FieldType.Name} component found.");
						return;
					}
					if (targets.Length == 1)
					{
						property.objectReferenceValue = targets[0];
						return;
					}
					var names = targets.Select(x => x.GetType().Name).ToList();
					var serialized = property.serializedObject;
					var propertyPath = property.propertyPath;
					SearchableDropdown.Show(right, "Component", names, index =>
					{
						serialized.Update();
						serialized.FindProperty(propertyPath).objectReferenceValue = targets[index];
						serialized.ApplyModifiedProperties();
					});
				}
			}
		}
	}
}