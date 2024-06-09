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
					var menu = new GenericMenu();
					foreach (var target in targets)
					{
						menu.AddItem(new GUIContent(target.GetType().Name), false, () =>
						{
							property.serializedObject.Update();
							property.objectReferenceValue = target;
							property.serializedObject.ApplyModifiedProperties();
						});
					}
					menu.ShowAsContext();
				}
			}
		}
	}
}