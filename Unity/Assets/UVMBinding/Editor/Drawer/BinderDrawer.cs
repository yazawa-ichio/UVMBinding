using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UVMBinding.Core;

namespace UVMBinding.Drawer
{
	[CustomEditor(typeof(Binder<>), true)]
	public class BinderDrawer : Editor
	{
		static string s_ConverterFoldoutPrefKey = "UVMBinding.Drawer-ConverterFoldout";

		MonoBehaviour m_Binder;

		void OnEnable()
		{
			m_Binder = target as MonoBehaviour;
		}

		public override void OnInspectorGUI()
		{
			serializedObject.UpdateIfRequiredOrScript();
			SerializedProperty property = serializedObject.GetIterator();
			bool expanded = true;
			SerializedProperty converter = null;
			while (property.NextVisible(expanded))
			{
				if (property.propertyPath == "m_Converter")
				{
					converter = property.Copy();
					continue;
				}
				if (property.propertyPath == "m_Path")
				{
					DrawPath(property);
					continue;
				}
				using (new EditorGUI.DisabledScope(property.propertyPath == "m_Script"))
				{
					EditorGUILayout.PropertyField(property, true);
				}
				expanded = false;
			}
			DrawConverter(converter);
			serializedObject.ApplyModifiedProperties();
		}

		protected void DrawPath(SerializedProperty property)
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUILayout.PropertyField(property, true);
				if (EditorGUILayout.DropdownButton(GUIContent.none, FocusType.Passive, GUILayout.ExpandWidth(false)))
				{
					IView view = null;
					foreach (var v in m_Binder.GetComponentsInParent<IView>(this))
					{
						if ((m_Binder as IBinder).CanUse(v))
						{
							view = v;
							break;
						}
					}
					if (view == null) return;
					var so = new SerializedObject((MonoBehaviour)view);
					var typeName = so.FindProperty("m_ReservationViewModel").stringValue;
					var type = ViewModelList.GetType(typeName);
					if (type == null) return;
					var data = ViewModelData.Get(type);
					var menu = new GenericMenu();
					foreach (var name in data.Properties.Select(x => x.Path))
					{
						menu.AddItem(new GUIContent(name), false, () =>
						{
							var so = serializedObject;
							so.Update();
							var property = so.FindProperty("m_Path");
							property.stringValue = name;
							so.ApplyModifiedProperties();
						});
					}
					menu.ShowAsContext();
				}
			}
		}

		protected void DrawConverter(SerializedProperty property)
		{
			if (property == null) return;

			GUILayout.BeginHorizontal();

			var foldout = EditorPrefs.GetBool(s_ConverterFoldoutPrefKey, true);
			var ret = EditorGUILayout.Foldout(foldout, "Converter", true);
			if (ret != foldout)
			{
				EditorPrefs.SetBool(s_ConverterFoldoutPrefKey, ret);
			}
			var index = ConverterList.GetIndex(property.managedReferenceFullTypename);
			var select = EditorGUILayout.Popup(index, ConverterList.GetDispNames());
			GUILayout.EndHorizontal();
			if (index != select)
			{
				var type = ConverterList.GetType(select);
				if (type != null)
				{
					property.managedReferenceValue = Activator.CreateInstance(type);
				}
				else
				{
					property.managedReferenceValue = null;
				}
			}
			if (select == 0 || !foldout)
			{
				return;
			}
			if (property.hasChildren)
			{
				var type = ConverterList.GetType(select);
				if (typeof(ConcatenateConverter) == type)
				{
					GUILayout.Label("Input");
					property = property.FindPropertyRelative("m_Converters");
					for (int i = 0; i < property.arraySize; i++)
					{
						DrawConcatenateConverter(property, i, property.GetArrayElementAtIndex(i));
					}
					if (GUILayout.Button("+"))
					{
						property.InsertArrayElementAtIndex(property.arraySize);
					}
					GUILayout.Label("Output");
				}
				else
				{
					using (new EditorGUI.IndentLevelScope())
					{
						var end = property.GetEndProperty();
						bool expanded = true;
						while (property.NextVisible(expanded) && !SerializedProperty.EqualContents(property, end))
						{
							EditorGUILayout.PropertyField(property, true);
							expanded = false;
						}
					}
				}
			}

		}

		protected void DrawConcatenateConverter(SerializedProperty root, int index, SerializedProperty property)
		{
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("▲", GUILayout.ExpandWidth(false)))
			{
				if (index > 0)
				{
					root.MoveArrayElement(index, index - 1);
				}
			}
			if (GUILayout.Button("▼", GUILayout.ExpandWidth(false)))
			{
				if (root.arraySize > index)
				{
					root.MoveArrayElement(index, index + 1);
				}
			}
			var converterIndex = ConverterList.GetIndex(property.managedReferenceFullTypename);
			var select = EditorGUILayout.Popup(converterIndex, ConverterList.GetDispNames());
			if (ConverterList.GetType(select) == typeof(ConcatenateConverter))
			{
				Debug.LogWarning("not allow ConcatenateConverter in ConcatenateConverter");
				select = 0;
			}
			if (GUILayout.Button("x", GUILayout.ExpandWidth(false)))
			{
				root.DeleteArrayElementAtIndex(index);
				return;
			}
			GUILayout.EndHorizontal();
			if (converterIndex != select)
			{
				var type = ConverterList.GetType(select);
				if (type != null)
				{
					property.managedReferenceValue = Activator.CreateInstance(type);
				}
				else
				{
					property.managedReferenceValue = null;
				}
			}
			if (select == 0)
			{
				return;
			}
			if (property.hasChildren)
			{
				using (new EditorGUI.IndentLevelScope())
				{
					var end = property.GetEndProperty();
					bool expanded = true;
					while (property.NextVisible(expanded) && !SerializedProperty.EqualContents(property, end))
					{
						EditorGUILayout.PropertyField(property, true);
						expanded = false;
					}
				}
			}
		}
	}


}