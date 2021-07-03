using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UVMBinding.Core;

namespace UVMBinding.Drawer
{

	[System.Serializable]
	public class BindingViewer
	{
		static string s_SelectElementPrefKey = "UVMBinding.Drawer-SelectElement";
		static string s_ViewModelPrefKey = "UVMBinding.Drawer-ViewModel";

		[SerializeField]
		int m_Index;

		View m_View;
		SerializedObject m_Serialized;
		HashSet<IViewElement> m_Elements = new HashSet<IViewElement>();

		string[] m_ElementNames;
		List<IElementViewer> m_ElementViewers = new List<IElementViewer>();

		public void Setup(View view)
		{
			m_View = view;
			UpdateList();
		}

		bool IsChange()
		{
			if (UnityEngine.Event.current.type != EventType.Layout)
			{
				return false;
			}

			using (ListPool<IViewElement>.Use(out var temp))
			{
				m_View.GetComponentsInChildren(true, temp);
				foreach (var elm in temp)
				{
					if (!m_Elements.Contains(elm))
					{
						return true;
					}
				}
				return false;
			}
		}

		public void UpdateList()
		{
			m_Serialized = new SerializedObject(m_View);
			m_Elements.Clear();
			using (ListPool<IViewElement>.Use(out var temp))
			{
				m_View.GetComponentsInChildren(true, temp);
				m_Elements.UnionWith(temp);
			}
			m_View.Prepare(true);
			m_ElementViewers.Clear();
			using (ListPool<IViewElement>.Use(out var temp))
			{
				m_View.GetElements(temp);
				foreach (var elm in temp)
				{
					if (elm is MonoBehaviour)
					{
						m_ElementViewers.Add(new ElementViewer(elm as MonoBehaviour));
					}
				}
			}
			m_ElementNames = new string[m_ElementViewers.Count];
		}

		public void Draw()
		{
			if (m_View == null)
			{
				GUILayout.Label("None Binding.");
				return;
			}

			m_Serialized.Update();

			if (IsChange())
			{
				UpdateList();
			}

			for (int i = 0; i < m_ElementViewers.Count; i++)
			{
				m_ElementNames[i] = (i + 1) + ":" + m_ElementViewers[i].GetName();
			}

			DrawViewModel();
			DrawElementInspector();

			m_Serialized.ApplyModifiedProperties();
		}

		void DrawElementInspector()
		{
			var foldout = EditorPrefs.GetBool(s_SelectElementPrefKey, true);
			var ret = EditorGUILayout.Foldout(foldout, "Element Inspector", true);
			if (ret != foldout)
			{
				EditorPrefs.SetBool(s_SelectElementPrefKey, ret);
			}
			if (!ret)
			{
				return;
			}
			using (new GUILayout.VerticalScope("box"))
			{
				using (new GUILayout.HorizontalScope())
				{
					GUILayout.Label("Select Element", GUILayout.ExpandWidth(false));
					m_Index = EditorGUILayout.Popup(m_Index, m_ElementNames);
				}
				GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
				if (m_Index >= 0 && m_Index < m_ElementViewers.Count)
				{
					m_ElementViewers[m_Index].DrawInspector();
				}
				else
				{
					GUILayout.Label("None Select Elements");
				}
			}
		}

		void DrawViewModel()
		{
			var foldout = EditorPrefs.GetBool(s_ViewModelPrefKey, true);
			var ret = EditorGUILayout.Foldout(foldout, "ViewModel", true);
			if (ret != foldout)
			{
				EditorPrefs.SetBool(s_ViewModelPrefKey, ret);
			}
			if (!ret)
			{
				return;
			}
			DrawReservationViewModel();
			DrawRuntimeViewModel(m_View.ViewModel);
		}

		void DrawReservationViewModel()
		{
			var property = m_Serialized.FindProperty("m_ReservationViewModel");
			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUILayout.PropertyField(property, new GUIContent("Name"), true);
				if (EditorGUILayout.DropdownButton(GUIContent.none, FocusType.Passive, GUILayout.ExpandWidth(false)))
				{
					var menu = new GenericMenu();
					foreach (var name in ViewModelList.GetNames())
					{
						if (name == "None" || name == "UVMBinding.GeneralViewModel")
						{
							continue;
						}
						menu.AddItem(new GUIContent(ViewModelList.GetDispName(name)), false, () =>
						{
							var so = new SerializedObject(m_View);
							so.Update();
							var property = so.FindProperty("m_ReservationViewModel");
							property.stringValue = name;
							so.ApplyModifiedProperties();
						});
					}
					menu.ShowAsContext();
				}
			}
			if (GUILayout.Button("Attach"))
			{
				var type = ViewModelList.GetType(property.stringValue);
				if (type != null)
				{
					m_View.ViewModel = (IViewModel)System.Activator.CreateInstance(type);
				}
			}
		}

		void DrawRuntimeViewModel(IViewModel vm)
		{
			if (vm == null)
			{
				GUILayout.Label("not attach ViewModel");
				return;
			}
			using (new GUILayout.VerticalScope("box"))
			{
				EditorGUILayout.LabelField("Property Path(Type)", "Value");
				GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
				using (ListPool<IBindingProperty>.Use(out var list))
				{
					foreach (var prop in vm.Property.GetAllImpl(list))
					{
						using (var scope = new EditorGUI.ChangeCheckScope())
						{
							DrawProperty(prop);
							if (scope.changed)
							{
								EditorApplication.QueuePlayerLoopUpdate();
							}
						}
					}
				}
			}
			using (new GUILayout.VerticalScope("box"))
			{
				EditorGUILayout.LabelField("Event Name", "Target");
				GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
				foreach (var e in vm.Event.GetAll())
				{
					EditorGUILayout.LabelField(e.Name, e.ToString());
				}
			}

		}

		void DrawProperty(IBindingProperty prop)
		{
			var name = $"{prop.Path}({prop.GetBindType().Name})";
			switch (prop)
			{
				case IBindingProperty<string> property:
					property.Value = EditorGUILayout.TextField(name, property.Value);
					break;
				case IBindingProperty<byte> property:
					property.Value = (byte)EditorGUILayout.IntField(name, property.Value);
					break;
				case IBindingProperty<ushort> property:
					property.Value = (ushort)EditorGUILayout.IntField(name, property.Value);
					break;
				case IBindingProperty<uint> property:
					property.Value = (uint)EditorGUILayout.LongField(name, property.Value);
					break;
				case IBindingProperty<sbyte> property:
					property.Value = (sbyte)EditorGUILayout.IntField(name, property.Value);
					break;
				case IBindingProperty<short> property:
					property.Value = (short)EditorGUILayout.IntField(name, property.Value);
					break;
				case IBindingProperty<int> property:
					property.Value = EditorGUILayout.IntField(name, property.Value);
					break;
				case IBindingProperty<long> property:
					property.Value = EditorGUILayout.LongField(name, property.Value);
					break;
				case IBindingProperty<float> property:
					property.Value = EditorGUILayout.FloatField(name, property.Value);
					break;
				case IBindingProperty<double> property:
					property.Value = EditorGUILayout.DoubleField(name, property.Value);
					break;
				case IBindingProperty<bool> property:
					property.Value = EditorGUILayout.Toggle(name, property.Value);
					break;
				default:
					EditorGUILayout.LabelField(name, prop.ToString());
					break;
			}
		}


	}

}