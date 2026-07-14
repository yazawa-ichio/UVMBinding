using System.Linq;
using UnityEditor;
using UnityEngine;
using UVMBinding.Core;
using UVMBinding.PropertyProvider;
namespace UVMBinding.Drawer
{
	[CustomPropertyDrawer(typeof(PropertyPathAttribute))]
	public class PropertyPathDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var dropdownWidth = 20;
			var buffer = 2;
			var left = position;
			left.width -= dropdownWidth + buffer;
			EditorGUI.PropertyField(left, property, label);
			var right = position;
			right.xMin = left.xMax + buffer;
			right.width = dropdownWidth;
			if (EditorGUI.DropdownButton(right, GUIContent.none, FocusType.Passive))
			{
				IView view = null;
				bool ignoreExtension = false;
				var components = property.serializedObject.targetObjects.OfType<Component>();
				foreach (var component in components)
				{
					foreach (var v in component.GetComponentsInParent<IView>(true))
					{
						if (component is IBinder binder)
						{
							if (binder.CanUse(v))
							{
								view = v;
							}
							break;
						}
						else if (component is ViewModelPropertyExtension)
						{
							ignoreExtension = true;
							view = v;
							break;
						}
					}
				}
				if (view == null) return;
				using var so = new SerializedObject((MonoBehaviour)view);
				var typeName = so.FindProperty("m_ReservationViewModel").stringValue;
				var type = ViewModelList.GetType(typeName);
				if (type == null) return;
				var data = ViewModelData.Get(type);
				var menu = new GenericMenu();
				var _property = property;
				foreach (var name in data.Properties.Select(x => x.Path))
				{
					Add(name);
				}
				if (!ignoreExtension && view is Component viewComp && viewComp.TryGetComponent<ViewModelPropertyExtension>(out var extension))
				{
					foreach (var info in extension.GetPropertyInfos())
					{
						Add(info.Path);
					}
				}
				menu.ShowAsContext();

				void Add(string name)
				{
					menu.AddItem(new GUIContent(name), false, () =>
					{
						var so = _property.serializedObject;
						so.Update();
						_property.stringValue = name;
						so.ApplyModifiedProperties();
					});
				}
			}
		}

	}
}