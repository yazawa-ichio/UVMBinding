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
		static string s_FailurePrefKey = "UVMBinding.Drawer-BindingFailure";

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
			DrawFailures();
			DrawElementInspector();

			m_Serialized.ApplyModifiedProperties();
		}

		void DrawFailures()
		{
			if (!Application.isPlaying)
			{
				return;
			}
			var foldout = EditorPrefs.GetBool(s_FailurePrefKey, true);
			var ret = EditorGUILayout.Foldout(foldout, "Binding Failures", true);
			if (ret != foldout)
			{
				EditorPrefs.SetBool(s_FailurePrefKey, ret);
			}
			if (!ret)
			{
				return;
			}
			var report = new Lint.LintReport();
			Lint.ViewLinter.LintRuntime(m_View, report);
			using (new GUILayout.VerticalScope("box"))
			{
				if (m_View.ViewModel == null)
				{
					GUILayout.Label("not attach ViewModel");
					return;
				}
				if (report.Violations.Count == 0)
				{
					GUILayout.Label(report.Partial.Count > 0
						? "No failures (paths are dynamic)"
						: "No failures");
					return;
				}
				foreach (var violation in report.Violations)
				{
					using (new GUILayout.HorizontalScope())
					{
						EditorGUILayout.LabelField($"[{violation.Rule}] {violation.Detail}");
						using (new EditorGUI.DisabledScope(violation.Source == null))
						{
							if (GUILayout.Button("Select", GUILayout.ExpandWidth(false)))
							{
								Selection.activeObject = violation.Source;
								EditorGUIUtility.PingObject(violation.Source);
							}
						}
					}
				}
			}
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
					var current = m_Index >= 0 && m_Index < m_ElementNames.Length ? m_ElementNames[m_Index] : "None";
					if (EditorGUILayout.DropdownButton(new GUIContent(current), FocusType.Passive))
					{
						SearchableDropdown.Show(GUILayoutUtility.GetLastRect(), "Element", m_ElementNames, index => m_Index = index);
					}
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
			var property = m_Serialized.FindProperty(View.ReservationViewModelFieldName);
			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUILayout.PropertyField(property, new GUIContent("Name"), true);
				if (EditorGUILayout.DropdownButton(GUIContent.none, FocusType.Passive, GUILayout.ExpandWidth(false)))
				{
					var values = new List<string>();
					var names = new List<string>();
					foreach (var name in ViewModelList.GetNames())
					{
						if (name == "None" || name == "UVMBinding.GeneralViewModel")
						{
							continue;
						}
						values.Add(name);
						names.Add(ViewModelList.GetDispName(name));
					}
					var view = m_View;
					SearchableDropdown.Show(GUILayoutUtility.GetLastRect(), "ViewModel", names, index =>
					{
						var so = new SerializedObject(view);
						so.Update();
						so.FindProperty(View.ReservationViewModelFieldName).stringValue = values[index];
						so.ApplyModifiedProperties();
					});
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
				EditorGUILayout.LabelField("Attached Type", vm.GetType().FullName);
				GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
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
			var type = prop.GetBindType();
			if (type == typeof(string))
			{
				prop.TryGetValue<string>(out var value);
				prop.TrySetValue(EditorGUILayout.TextField(name, value));
			}
			else if (type == typeof(byte))
			{
				prop.TryGetValue<byte>(out var value);
				prop.TrySetValue((byte)EditorGUILayout.IntField(name, value));
			}
			else if (type == typeof(ushort))
			{
				prop.TryGetValue<ushort>(out var value);
				prop.TrySetValue((ushort)EditorGUILayout.IntField(name, value));
			}
			else if (type == typeof(uint))
			{
				prop.TryGetValue<uint>(out var value);
				prop.TrySetValue((uint)EditorGUILayout.LongField(name, value));
			}
			else if (type == typeof(sbyte))
			{
				prop.TryGetValue<sbyte>(out var value);
				prop.TrySetValue((sbyte)EditorGUILayout.IntField(name, value));
			}
			else if (type == typeof(short))
			{
				prop.TryGetValue<short>(out var value);
				prop.TrySetValue((short)EditorGUILayout.IntField(name, value));
			}
			else if (type == typeof(int))
			{
				prop.TryGetValue<int>(out var value);
				prop.TrySetValue(EditorGUILayout.IntField(name, value));
			}
			else if (type == typeof(long))
			{
				prop.TryGetValue<long>(out var value);
				prop.TrySetValue(EditorGUILayout.LongField(name, value));
			}
			else if (type == typeof(float))
			{
				prop.TryGetValue<float>(out var value);
				prop.TrySetValue(EditorGUILayout.FloatField(name, value));
			}
			else if (type == typeof(double))
			{
				prop.TryGetValue<double>(out var value);
				prop.TrySetValue(EditorGUILayout.DoubleField(name, value));
			}
			else if (type == typeof(bool))
			{
				prop.TryGetValue<bool>(out var value);
				prop.TrySetValue(EditorGUILayout.Toggle(name, value));
			}
			else
			{
				EditorGUILayout.LabelField(name, prop.ToString());
			}
		}

	}

}