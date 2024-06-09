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
				if (property.serializedObject.targetObject is MonoBehaviour component)
				{
					var view = component.GetComponentInParent<IView>();
					if (view == null) return;
					var so = new SerializedObject((MonoBehaviour)view);
					var typeName = so.FindProperty("m_ReservationViewModel").stringValue;
					var type = ViewModelList.GetType(typeName);
					if (type == null) return;
					var data = ViewModelData.Get(type);
					var menu = new GenericMenu();
					var propertyName = property.name;
					foreach (var name in data.Events.Select(x => x.Path))
					{
						menu.AddItem(new GUIContent(name), false, () =>
						{
							var so = new SerializedObject(component);
							var property = so.FindProperty(propertyName);
							property.stringValue = name;
							so.ApplyModifiedProperties();
						});
					}
					menu.ShowAsContext();
				}
			}
		}
	}
}