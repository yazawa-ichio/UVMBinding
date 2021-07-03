using UnityEditor;
using UnityEngine;
using UVMBinding.Core;

namespace UVMBinding.Drawer
{
	public class ElementViewer : IElementViewer
	{
		MonoBehaviour m_Element;
		Editor m_Editor;
		IBinder m_Binder;

		public ElementViewer(MonoBehaviour element)
		{
			m_Element = element;
			m_Editor = Editor.CreateEditor(element);
			m_Binder = m_Element as IBinder;
		}

		public void DrawInspector()
		{
			EditorGUILayout.ObjectField("Bind GameObject", m_Element.gameObject, typeof(GameObject), allowSceneObjects: true);
			m_Editor.OnInspectorGUI();
		}

		public string GetName()
		{
			if (m_Binder == null)
			{
				return $"{m_Element.name}({m_Element.GetType().Name})";
			}
			else
			{
				return $"{m_Binder.Path}({m_Binder.GetType().Name})";
			}
		}

		public bool CanRemove()
		{
			return false;
		}

		public void Remove()
		{
		}

		public bool IsBindable()
		{
			return m_Binder != null;
		}

		public bool DrawBinding(bool selected)
		{
			bool ret;
			var c = GUI.backgroundColor;
			GUI.backgroundColor = new Color(1f, 1f, 1f, 0.75f);
			if (selected)
			{
				GUILayout.BeginHorizontal("SelectionRect");
			}
			else
			{
				GUILayout.BeginHorizontal();
			}
			{
				ret = GUILayout.Button(GetName(), "Label");
			}
			GUILayout.EndHorizontal();
			GUI.backgroundColor = c;
			return ret;
		}
	}

}