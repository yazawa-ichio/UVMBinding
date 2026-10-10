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
				var targets = property.serializedObject.targetObjects;
				bool ignoreExtension = false;
				foreach (var target in targets)
				{
					if (target is ViewModelPropertyExtension)
					{
						ignoreExtension = true;
					}
				}
				if (!DrawerMultiSelect.TryResolveShared(targets, FindSourceType, out var type, out var reason))
				{
					DrawerMultiSelect.ShowReason(right, reason);
					return;
				}
				var view = FindView(targets[0] as Component);
				var names = BindSourceData.GetBindPaths(type);
				if (!ignoreExtension && view is Component viewComp && viewComp.TryGetComponent<ViewModelPropertyExtension>(out var extension))
				{
					foreach (var info in extension.GetPropertyInfos())
					{
						names.Add(info.Path);
					}
				}
				var serialized = property.serializedObject;
				var propertyPath = property.propertyPath;
				SearchableDropdown.Show(right, "Property Path", names, index =>
				{
					serialized.Update();
					serialized.FindProperty(propertyPath).stringValue = names[index];
					serialized.ApplyModifiedProperties();
				});
			}
		}

		static IView FindView(Component component)
		{
			if (component == null)
			{
				return null;
			}
			foreach (var v in component.GetComponentsInParent<IView>(true))
			{
				if (component is IBinder binder)
				{
					return binder.CanUse(v) ? v : null;
				}
				if (component is ViewModelPropertyExtension)
				{
					return v;
				}
			}
			return null;
		}

		static System.Type FindSourceType(UnityEngine.Object target)
		{
			if (!(FindView(target as Component) is View concrete))
			{
				return null;
			}
			return BindSourceData.FindType(concrete.ReservationViewModelName);
		}
	}
}