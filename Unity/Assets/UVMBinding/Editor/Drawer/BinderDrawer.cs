using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UVMBinding.Converters;
using UVMBinding.Core;
using UVMBinding.Lint;

namespace UVMBinding.Drawer
{

	[CustomEditor(typeof(Binder<>), true)]
	[CanEditMultipleObjects]
	public class BinderDrawer : Editor
	{
		const string FoldoutKey = "UVMBinding.Drawer-ConverterFoldout";
		const float ButtonWidth = 22f;

		static readonly Dictionary<System.Type, IBindSource> s_Sources = new Dictionary<System.Type, IBindSource>();
		static readonly List<ConverterStage> s_Stages = new List<ConverterStage>();
		static readonly List<int> s_Direct = new List<int>();
		static readonly List<int> s_NeedsCast = new List<int>();
		static readonly List<int> s_Mismatch = new List<int>();

		int m_Remove = -1;
		int m_MoveFrom = -1;
		int m_MoveTo = -1;

		public override void OnInspectorGUI()
		{
			serializedObject.UpdateIfRequiredOrScript();
			SerializedProperty property = serializedObject.GetIterator();
			bool expanded = true;
			SerializedProperty converters = null;
			while (property.NextVisible(expanded))
			{
				if (property.propertyPath == "m_Converters")
				{
					converters = property.Copy();
					continue;
				}
				if (property.propertyPath == "m_AllowCast")
				{
					continue;
				}
				using (new EditorGUI.DisabledScope(property.propertyPath == "m_Script"))
				{
					EditorGUILayout.PropertyField(property, true);
				}
				expanded = false;
			}
			DrawConverters(converters);
			serializedObject.ApplyModifiedProperties();
		}

		protected void DrawPath(SerializedProperty property)
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUILayout.PropertyField(property, true);
				if (EditorGUILayout.DropdownButton(GUIContent.none, FocusType.Passive, GUILayout.ExpandWidth(false)))
				{
					var rect = GUILayoutUtility.GetLastRect();
					if (!DrawerMultiSelect.TryResolveShared(targets, FindSourceType, out var type, out var reason))
					{
						DrawerMultiSelect.ShowReason(rect, reason);
						return;
					}
					var names = BindSourceData.GetBindPaths(type);
					var propertyPath = property.propertyPath;
					SearchableDropdown.Show(rect, "Property Path", names, index =>
					{
						var so = serializedObject;
						so.Update();
						so.FindProperty(propertyPath).stringValue = names[index];
						so.ApplyModifiedProperties();
					});
				}
			}
		}

		internal static System.Type FindSourceType(UnityEngine.Object target)
		{
			if (!(target is MonoBehaviour binder))
			{
				return null;
			}
			if (BindSourceProvider.IsOwnedByProvider(binder))
			{
				foreach (var provider in binder.GetComponents<BindSourceProvider>())
				{
					if (provider.IsTarget(binder))
					{
						return ViewLinter.GetDeclaredSourceType(provider.GetType());
					}
				}
				return null;
			}
			foreach (var v in binder.GetComponentsInParent<IView>(true))
			{
				if (!(binder as IBinder).CanUse(v))
				{
					return null;
				}
				return v is View concrete ? BindSourceData.FindType(concrete.ReservationViewModelName) : null;
			}
			return null;
		}

		void DrawConverters(SerializedProperty list)
		{
			if (list == null)
			{
				return;
			}
			var info = target as IBinderInfo;
			var declared = ResolveDeclaredType(out var reason);
			s_Stages.Clear();
			info?.GetConverterStages(s_Stages);

			bool open = EditorPrefs.GetBool(FoldoutKey, true);
			using (new EditorGUILayout.HorizontalScope())
			{
				var next = EditorGUILayout.Foldout(open, "Converter", true);
				if (next != open)
				{
					EditorPrefs.SetBool(FoldoutKey, next);
					open = next;
				}
				GUILayout.FlexibleSpace();
				GUILayout.Label(Summary(info, declared), EditorStyles.miniLabel);
			}
			if (!open)
			{
				return;
			}
			using (new EditorGUI.IndentLevelScope())
			{
				if (reason != null)
				{
					EditorGUILayout.HelpBox(reason, MessageType.None);
				}
				if (list.hasMultipleDifferentValues)
				{
					EditorGUILayout.LabelField("Multiple different values");
					return;
				}
				DrawFlow(list, info, declared);
			}
			ApplyPending(list);
		}

		void DrawFlow(SerializedProperty list, IBinderInfo info, System.Type declared)
		{
			bool editable = targets.Length == 1;
			bool allowCast = info != null && info.AllowCast;
			var path = info != null && !string.IsNullOrEmpty(info.Path) ? info.Path : "(no path)";
			EditorGUILayout.LabelField($"{path} : {Name(declared)}", EditorStyles.miniLabel);

			var current = declared;
			for (int i = 0; i < list.arraySize; i++)
			{
				var stage = i < s_Stages.Count ? s_Stages[i] : ConverterStage.Empty;
				DrawJunction(current, stage.IsEmpty ? null : stage.InputType, allowCast, i, editable);
				DrawStage(list, i, stage, editable);
				current = stage.IsEmpty ? null : stage.OutputType;
			}
			DrawJunction(current, info?.BindType, allowCast, list.arraySize, editable);
			EditorGUILayout.LabelField($"Binder<{Name(info?.BindType)}>", EditorStyles.miniLabel);
		}

		void DrawJunction(System.Type from, System.Type to, bool allowCast, int index, bool editable)
		{
			var label = Describe(from, to, allowCast);
			if (to == typeof(object) && from != null && from.IsValueType)
			{
				label += "   boxed";
			}
			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.Label("│  " + label, EditorStyles.miniLabel);
				GUILayout.FlexibleSpace();
				using (new EditorGUI.DisabledScope(!editable))
				{
					if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(ButtonWidth)))
					{
						ShowPicker(GUILayoutUtility.GetLastRect(), index);
					}
				}
			}
			if (NeedsCast(from, to, allowCast))
			{
				DrawAllowCast(editable);
			}
		}

		void DrawStage(SerializedProperty list, int index, ConverterStage stage, bool editable)
		{
			var element = list.GetArrayElementAtIndex(index);
			using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
			{
				using (new EditorGUILayout.HorizontalScope())
				{
					var name = stage.IsEmpty ? "(missing)" : Short(DispName(element));
					GUILayout.Label(name, EditorStyles.boldLabel);
					using (new EditorGUI.DisabledScope(!editable))
					{
						GUILayout.FlexibleSpace();
						using (new EditorGUI.DisabledScope(index == 0))
						{
							if (GUILayout.Button("▲", EditorStyles.miniButtonLeft, GUILayout.Width(ButtonWidth)))
							{
								m_MoveFrom = index;
								m_MoveTo = index - 1;
							}
						}
						using (new EditorGUI.DisabledScope(index + 1 >= list.arraySize))
						{
							if (GUILayout.Button("▼", EditorStyles.miniButtonMid, GUILayout.Width(ButtonWidth)))
							{
								m_MoveFrom = index;
								m_MoveTo = index + 1;
							}
						}
						if (GUILayout.Button("×", EditorStyles.miniButtonRight, GUILayout.Width(ButtonWidth)))
						{
							m_Remove = index;
						}
					}
				}
				if (stage.IsEmpty || !element.hasChildren)
				{
					return;
				}
				using (new EditorGUI.IndentLevelScope())
				{
					var end = element.GetEndProperty();
					var walker = element.Copy();
					bool enter = true;
					while (walker.NextVisible(enter) && !SerializedProperty.EqualContents(walker, end))
					{
						EditorGUILayout.PropertyField(walker, true);
						enter = false;
					}
				}
			}
		}

		void ApplyPending(SerializedProperty list)
		{
			if (m_Remove >= 0)
			{
				int size = list.arraySize;
				list.DeleteArrayElementAtIndex(m_Remove);
				if (list.arraySize == size)
				{
					list.DeleteArrayElementAtIndex(m_Remove);
				}
				m_Remove = -1;
			}
			else if (m_MoveFrom >= 0)
			{
				list.MoveArrayElement(m_MoveFrom, m_MoveTo);
				m_MoveFrom = -1;
			}
		}

		void ShowPicker(Rect rect, int index)
		{
			ResolveJunction(index, out var inputType, out var requiredType);
			s_Direct.Clear();
			s_NeedsCast.Clear();
			s_Mismatch.Clear();
			ConverterFilter.GetGroups(inputType, requiredType, s_Direct, s_NeedsCast, s_Mismatch);

			var all = ConverterList.GetDispNames();
			var names = new List<string>();
			var picks = new List<int>();
			int groups = (s_Direct.Count > 0 ? 1 : 0) + (s_NeedsCast.Count > 0 ? 1 : 0)
				+ (s_Mismatch.Count > 0 ? 1 : 0);
			void Fill(List<int> group, string branch)
			{
				foreach (var i in group)
				{
					names.Add(groups > 1 ? branch + Short(all[i]) : Short(all[i]));
					picks.Add(i);
				}
			}
			Fill(s_Direct, $"Connects ({s_Direct.Count})/");
			Fill(s_NeedsCast, $"Needs cast ({s_NeedsCast.Count})/");
			Fill(s_Mismatch, $"Type mismatch ({s_Mismatch.Count})/");

			var key = $"Converter.{Name(inputType)}=>{Name(requiredType)}";
			var serialized = serializedObject;
			SearchableDropdown.Show(rect, "Converter", names,
				picked => Apply(serialized, index, picks[picked]), key);
		}

		void ResolveJunction(int index, out System.Type inputType, out System.Type requiredType)
		{
			inputType = index == 0 ? ResolveDeclaredType(out _) : Output(index - 1);
			requiredType = index < s_Stages.Count && !s_Stages[index].IsEmpty
				? s_Stages[index].InputType
				: (target as IBinderInfo)?.BindType;
		}

		static System.Type Output(int index)
		{
			return index < s_Stages.Count && !s_Stages[index].IsEmpty ? s_Stages[index].OutputType : null;
		}

		static void Apply(SerializedObject serialized, int index, int converterIndex)
		{
			var type = ConverterList.GetType(converterIndex);
			if (type == null)
			{
				return;
			}
			serialized.Update();
			var list = serialized.FindProperty("m_Converters");
			list.InsertArrayElementAtIndex(index);
			list.GetArrayElementAtIndex(index).managedReferenceValue = System.Activator.CreateInstance(type);
			serialized.ApplyModifiedProperties();
		}

		System.Type ResolveDeclaredType(out string reason)
		{
			reason = null;
			if (targets.Length != 1)
			{
				reason = "Multiple objects selected. Stages are read-only.";
				return null;
			}
			var sourceType = FindSourceType(target);
			var source = FindSource(sourceType);
			if (source == null)
			{
				reason = "Bind source type is not declared on the parent View or Provider.";
				return null;
			}
			if (source.IsDynamic)
			{
				reason = $"Paths of {sourceType.Name} are dynamic.";
				return null;
			}
			if (!(target is IBinderInfo info) || string.IsNullOrEmpty(info.Path))
			{
				reason = "Path is not set.";
				return null;
			}
			var declared = source.GetDeclaredType(info.Path);
			if (declared == null)
			{
				reason = $"Path '{info.Path}' does not exist on {sourceType.Name}.";
			}
			return declared;
		}

		string Summary(IBinderInfo info, System.Type declared)
		{
			if (info == null)
			{
				return string.Empty;
			}
			var text = $"{Name(declared)} ▶ {Name(info.BindType)}";
			if (Application.isPlaying)
			{
				text += $"   [runtime: {info.Kind}]";
			}
			return Connected(info, declared) ? text : "⚠ " + text;
		}

		bool Connected(IBinderInfo info, System.Type declared)
		{
			var current = declared;
			foreach (var stage in s_Stages)
			{
				if (stage.IsEmpty || !Connects(current, stage.InputType, info.AllowCast))
				{
					return false;
				}
				current = stage.OutputType;
			}
			return Connects(current, info.BindType, info.AllowCast);
		}

		static bool Connects(System.Type from, System.Type to, bool allowCast)
		{
			if (from == null || to == null)
			{
				return false;
			}
			if (to.IsAssignableFrom(from) || CastRegistry.IsImplicit(from, to))
			{
				return true;
			}
			return allowCast && CastRegistry.IsRegistered(from, to);
		}

		static string Describe(System.Type from, System.Type to, bool allowCast)
		{
			if (from == null || to == null)
			{
				return $"?  {Name(from)} → {Name(to)}";
			}
			if (from == to)
			{
				return "match";
			}
			if (to.IsAssignableFrom(from))
			{
				return "assign";
			}
			if (CastRegistry.IsImplicit(from, to))
			{
				return "widen";
			}
			if (allowCast && CastRegistry.IsRegistered(from, to))
			{
				return $"cast  {from.Name} → {to.Name}";
			}
			return $"✕  {from.Name} → {to.Name}";
		}

		static bool NeedsCast(System.Type from, System.Type to, bool allowCast)
		{
			if (allowCast || from == null || to == null || Connects(from, to, false))
			{
				return false;
			}
			return CastRegistry.IsRegistered(from, to);
		}

		void DrawAllowCast(bool editable)
		{
			EditorGUILayout.HelpBox(
				"This link needs a conversion that can change the value. Allowing it does not add a stage.",
				MessageType.Warning);
			using (new EditorGUI.DisabledScope(!editable))
			{
				if (GUILayout.Button("Allow cast"))
				{
					var serialized = serializedObject;
					serialized.Update();
					serialized.FindProperty("m_AllowCast").boolValue = true;
					serialized.ApplyModifiedProperties();
				}
			}
		}

		static string DispName(SerializedProperty element)
		{
			var names = ConverterList.GetDispNames();
			return names[ConverterList.GetIndex(element.managedReferenceFullTypename)];
		}

		static string Short(string name)
		{
			return name.StartsWith("Core/") ? name.Substring("Core/".Length) : name;
		}

		static string Name(System.Type type) => type != null ? type.Name : "?";

		internal static IBindSource FindSource(System.Type sourceType)
		{
			if (sourceType == null)
			{
				return null;
			}
			if (!s_Sources.TryGetValue(sourceType, out var source))
			{
				s_Sources[sourceType] = source = BindSourceType.Create(sourceType);
			}
			return source;
		}

		internal static System.Type FindDeclaredType(System.Type sourceType, string path)
		{
			var source = FindSource(sourceType);
			return source == null || source.IsDynamic ? null : source.GetDeclaredType(path);
		}
	}

}
