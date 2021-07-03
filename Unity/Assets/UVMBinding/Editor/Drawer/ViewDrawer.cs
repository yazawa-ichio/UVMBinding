using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UVMBinding.Drawer
{
	[CustomEditor(typeof(View), true)]
	public class ViewDrawer : Editor
	{
		static Dictionary<int, BindingViewer> m_Viewers = new Dictionary<int, BindingViewer>();

		[SerializeField]
		protected BindingViewer m_BindingViewer;

		protected View m_View;

		private void OnEnable()
		{
			m_View = target as View;
			if (m_BindingViewer == null)
			{
				if (!m_Viewers.TryGetValue(m_View.GetInstanceID(), out m_BindingViewer))
				{
					m_BindingViewer = new BindingViewer();
				}
			}
			m_BindingViewer.Setup(target as View);
			m_Viewers[m_View.GetInstanceID()] = m_BindingViewer;
		}

		public override void OnInspectorGUI()
		{
			using (new EditorGUI.DisabledGroupScope(true))
			{
				EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
			}
			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_UpdateOnAttach"));
			m_BindingViewer.Draw();
			if (!Application.isPlaying)
			{
				if (target is View view && view != null)
				{
					view.TryUpdate();
				}
			}
			serializedObject.ApplyModifiedProperties();
		}

	}

}