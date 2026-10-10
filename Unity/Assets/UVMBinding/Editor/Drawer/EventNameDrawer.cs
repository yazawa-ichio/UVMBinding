using System.Linq;
using UnityEditor;
using UnityEngine;
using UVMBinding.Core;

namespace UVMBinding.Drawer
{
	[CustomPropertyDrawer(typeof(EventNameSelectAttribute))]
	public class EventNameDrawer : PropertyDrawer
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
				var targets = property.serializedObject.targetObjects;
				if (!DrawerMultiSelect.TryResolveShared(targets, FindSourceType, out var type, out var reason))
				{
					DrawerMultiSelect.ShowReason(right, reason);
					return;
				}
				var names = BindSourceData.GetEventPaths(type);
				var serialized = property.serializedObject;
				var propertyPath = property.propertyPath;
				SearchableDropdown.Show(right, "Event Name", names, index =>
				{
					serialized.Update();
					serialized.FindProperty(propertyPath).stringValue = names[index];
					serialized.ApplyModifiedProperties();
				});
			}
		}

		static System.Type FindSourceType(UnityEngine.Object target)
		{
			if (!(target is MonoBehaviour component))
			{
				return null;
			}
			if (!(component.GetComponentInParent<IView>() is View concrete))
			{
				return null;
			}
			return BindSourceData.FindType(concrete.ReservationViewModelName);
		}
	}
}