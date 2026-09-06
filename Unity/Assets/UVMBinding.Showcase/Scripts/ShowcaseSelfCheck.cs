using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace UVMBinding.Showcase
{
	public class ShowcaseSelfCheck : MonoBehaviour
	{
		const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
		const int MaxDepth = 8;
		const string Tag = "[SelfCheck]";
		const char Newline = (char)10;

		[SerializeField]
		int m_DelayFrames = 3;
		[SerializeField]
		float m_SettleSeconds = 0.5f;

		readonly List<string> m_Errors = new List<string>();
		readonly List<string> m_States = new List<string>();

		public const string EditorEnabledKey = "UVMBinding.Showcase-SelfCheckOnPlay";

		IEnumerator Start()
		{
#if UNITY_EDITOR
			if (!UnityEditor.EditorPrefs.GetBool(EditorEnabledKey, false))
			{
				yield break;
			}
#endif
			Application.logMessageReceived += OnLog;
			for (int i = 0; i < m_DelayFrames; i++)
			{
				yield return null;
			}
			var tabs = FindFirstObjectByType<ShowcaseTabs>(FindObjectsInactive.Include);
			if (tabs != null)
			{
				for (int i = 0; i < tabs.ScreenCount; i++)
				{
					tabs.Select(i);
					yield return Settle();
					var screen = tabs.GetScreen(i);
					Snapshot(screen, "before");
					Interact(screen);
					yield return Settle();
					Snapshot(screen, "after");
					CheckLayout(screen);
				}
				tabs.Select(0);
				yield return null;
			}
			Application.logMessageReceived -= OnLog;
			Run();
		}

		IEnumerator Settle()
		{
			for (int f = 0; f < m_DelayFrames; f++)
			{
				yield return null;
			}
			yield return new WaitForSecondsRealtime(m_SettleSeconds);
			for (int f = 0; f < m_DelayFrames; f++)
			{
				yield return null;
			}
		}

		void OnLog(string condition, string stackTrace, LogType type)
		{
			if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
			{
				m_Errors.Add(condition);
			}
		}

		[ContextMenu("Run")]
		public void Run()
		{
			var types = new SortedDictionary<string, int>(StringComparer.Ordinal);
			var nulls = new List<string>();
			int slots = 0;

			foreach (var behaviour in Resources.FindObjectsOfTypeAll<MonoBehaviour>())
			{
				if (behaviour == null || !behaviour.gameObject.scene.IsValid())
				{
					continue;
				}
				Scan(behaviour, GetPath(behaviour.transform) + ":" + behaviour.GetType().Name, 0, types, nulls, ref slots);
			}

			nulls.Sort(StringComparer.Ordinal);
			m_Errors.Sort(StringComparer.Ordinal);

			var sb = new StringBuilder();
			sb.Append(Tag).Append(" BEGIN").Append(Newline);
			foreach (var pair in types)
			{
				sb.Append(Tag).Append(" type ").Append(pair.Key).Append(' ').Append(pair.Value).Append(Newline);
			}
			foreach (var path in nulls)
			{
				sb.Append(Tag).Append(" null ").Append(path).Append(Newline);
			}
			foreach (var state in m_States)
			{
				sb.Append(Tag).Append(" state ").Append(state).Append(Newline);
			}
			foreach (var error in m_Errors)
			{
				sb.Append(Tag).Append(" error ").Append(Flatten(error)).Append(Newline);
			}
			sb.Append(Tag).Append(" END slots=").Append(slots)
				.Append(" nulls=").Append(nulls.Count)
				.Append(" types=").Append(types.Count)
				.Append(" errors=").Append(m_Errors.Count)
				.Append(" states=").Append(m_States.Count)
				.Append(" layout=").Append(m_LayoutChecked);

			var outputPath = GetOutputPath();
			try
			{
				System.IO.File.WriteAllText(outputPath, sb.ToString());
			}
			catch (Exception e)
			{
				Debug.LogWarning(Tag + " write failed " + outputPath + " " + e.Message);
			}
			Debug.Log(Tag + " END slots=" + slots + " nulls=" + nulls.Count
				+ " types=" + types.Count + " errors=" + m_Errors.Count
				+ " states=" + m_States.Count + " layout=" + m_LayoutChecked + " out=" + outputPath);
			if (HasArg("-selfcheck-quit"))
			{
				Application.Quit(m_Errors.Count == 0 ? 0 : 1);
			}
		}

		int m_LayoutChecked;

		void CheckLayout(GameObject screen)
		{
			if (screen == null)
			{
				return;
			}
			var issues = new List<UVMBinding.Semantics.LayoutIssue>();
			foreach (var node in UVMBinding.Semantics.ViewTraversal.CaptureAll(
				screen, UVMBinding.Semantics.ViewTraversal.Options.ForExport))
			{
				m_LayoutChecked += UVMBinding.Semantics.LayoutCheck.Run(node, issues);
			}
			foreach (var issue in issues)
			{
				m_Errors.Add("layout " + issue);
			}
		}

		void Snapshot(GameObject screen, string phase)
		{
			if (screen == null)
			{
				return;
			}
			var rows = new List<string>();
			foreach (var target in screen.GetComponentsInChildren<Transform>(true))
			{
				var state = Describe(target);
				if (state.Length == 0)
				{
					continue;
				}
				rows.Add(phase + " " + GetPath(target) + " " + state);
			}
			rows.Sort(StringComparer.Ordinal);
			m_States.AddRange(rows);
		}

		static string Describe(Transform target)
		{
			var go = target.gameObject;
			var graphic = go.GetComponent<Graphic>();
			var group = go.GetComponent<CanvasGroup>();
			var selectable = go.GetComponent<Selectable>();
			bool binder = HasBinder(go);
			if (!binder && graphic == null && group == null && selectable == null)
			{
				return string.Empty;
			}

			var sb = new StringBuilder();
			Append(sb, "active", go.activeSelf ? "1" : "0");

			var text = go.GetComponent<Text>();
			if (text != null)
			{
				Append(sb, "text", Flatten(text.text));
			}
			if (graphic != null)
			{
				Append(sb, "col", ColorUtility.ToHtmlStringRGBA(graphic.color));
			}
			var image = go.GetComponent<Image>();
			if (image != null)
			{
				Append(sb, "spr", image.sprite != null ? image.sprite.name : "-");
				Append(sb, "fill", Round(image.fillAmount, 3));
			}
			var raw = go.GetComponent<RawImage>();
			if (raw != null)
			{
				Append(sb, "tex", raw.texture != null ? raw.texture.name : "-");
			}
			if (group != null)
			{
				Append(sb, "alpha", Round(group.alpha, 3));
			}
			if (selectable != null)
			{
				Append(sb, "inter", selectable.interactable ? "1" : "0");
			}
			var slider = go.GetComponent<Slider>();
			if (slider != null)
			{
				Append(sb, "val", Round(slider.value, 3));
			}
			var bar = go.GetComponent<Scrollbar>();
			if (bar != null)
			{
				Append(sb, "val", Round(bar.value, 3));
			}
			var toggle = go.GetComponent<Toggle>();
			if (toggle != null)
			{
				Append(sb, "val", toggle.isOn ? "1" : "0");
			}
			var dropdown = go.GetComponent<Dropdown>();
			if (dropdown != null)
			{
				Append(sb, "val", dropdown.value.ToString(CultureInfo.InvariantCulture));
			}
			var input = go.GetComponent<InputField>();
			if (input != null)
			{
				Append(sb, "val", Flatten(input.text));
			}
			if (binder)
			{
				var rect = target as RectTransform;
				Append(sb, "pos", rect != null
					? Vector2String(rect.anchoredPosition, 1)
					: Vector(target.localPosition, 1));
				Append(sb, "scale", Vector(target.localScale, 2));
				if (rect != null)
				{
					Append(sb, "size", Round(rect.rect.width, 1) + "," + Round(rect.rect.height, 1));
				}
				Append(sb, "rot", Vector(target.localEulerAngles, 1));
			}
			return sb.ToString();
		}

		static void Append(StringBuilder sb, string key, string value)
		{
			if (sb.Length > 0)
			{
				sb.Append(' ');
			}
			sb.Append(key).Append('=').Append(value);
		}

		static string Round(float value, int digits)
		{
			float scale = Mathf.Pow(10f, digits);
			float rounded = Mathf.Round(value * scale) / scale;
			if (rounded == 0f)
			{
				rounded = 0f;
			}
			return rounded.ToString("F" + digits, CultureInfo.InvariantCulture);
		}

		static string Vector(Vector3 value, int digits)
		{
			return Round(value.x, digits) + "," + Round(value.y, digits) + "," + Round(value.z, digits);
		}

		static string Vector2String(Vector2 value, int digits)
		{
			return Round(value.x, digits) + "," + Round(value.y, digits);
		}

		static bool HasBinder(GameObject go)
		{
			foreach (var component in go.GetComponents<MonoBehaviour>())
			{
				if (component == null)
				{
					continue;
				}
				for (var type = component.GetType(); type != null; type = type.BaseType)
				{
					if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Binder<>))
					{
						return true;
					}
				}
			}
			return false;
		}

		void Interact(GameObject screen)
		{
			if (screen == null)
			{
				return;
			}
			var targets = new List<KeyValuePair<string, Selectable>>();
			foreach (var selectable in screen.GetComponentsInChildren<Selectable>(true))
			{
				if (!selectable.gameObject.activeInHierarchy)
				{
					continue;
				}
				targets.Add(new KeyValuePair<string, Selectable>(GetPath(selectable.transform), selectable));
			}
			targets.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
			foreach (var pair in targets)
			{
				if (pair.Value == null)
				{
					continue;
				}
				Apply(pair.Value);
			}
		}

		static void Apply(Selectable target)
		{
			switch (target)
			{
				case Slider slider:
					slider.value = 0.75f;
					break;
				case Scrollbar bar:
					bar.value = 0.25f;
					break;
				case Toggle toggle:
					toggle.isOn = !toggle.isOn;
					break;
				case Dropdown dropdown:
					dropdown.value = 1;
					break;
				case InputField input:
					input.text = "SelfCheck";
					input.onEndEdit.Invoke(input.text);
					break;
				case Button button:
					button.onClick.Invoke();
					break;
			}
		}

		static void Scan(object target, string path, int depth, SortedDictionary<string, int> types, List<string> nulls, ref int slots)
		{
			if (target == null || depth > MaxDepth)
			{
				return;
			}
			for (var type = target.GetType(); type != null && type != typeof(MonoBehaviour) && type != typeof(object); type = type.BaseType)
			{
				foreach (var field in type.GetFields(Flags))
				{
					if (!Attribute.IsDefined(field, typeof(SerializeReference)))
					{
						continue;
					}
					var value = field.GetValue(target);
					var name = path + "." + field.Name;
					if (value is IList list && !(value is string))
					{
						for (int i = 0; i < list.Count; i++)
						{
							Visit(list[i], name + "[" + i + "]", depth, types, nulls, ref slots);
						}
					}
					else
					{
						Visit(value, name, depth, types, nulls, ref slots);
					}
				}
			}
		}

		static void Visit(object value, string path, int depth, SortedDictionary<string, int> types, List<string> nulls, ref int slots)
		{
			slots++;
			if (value == null)
			{
				nulls.Add(path);
				return;
			}
			var name = value.GetType().FullName;
			types.TryGetValue(name, out int count);
			types[name] = count + 1;
			Scan(value, path, depth + 1, types, nulls, ref slots);
		}

		static string GetOutputPath()
		{
			var args = Environment.GetCommandLineArgs();
			for (int i = 0; i < args.Length - 1; i++)
			{
				if (args[i] == "-selfcheck-out")
				{
					return args[i + 1];
				}
			}
			var directory = Application.isEditor
				? Application.persistentDataPath
				: System.IO.Path.GetDirectoryName(Application.dataPath);
			return System.IO.Path.Combine(directory, "selfcheck.txt");
		}

		static bool HasArg(string name)
		{
			var args = Environment.GetCommandLineArgs();
			for (int i = 0; i < args.Length; i++)
			{
				if (args[i] == name)
				{
					return true;
				}
			}
			return false;
		}

		static string Flatten(string value)
		{
			return value.Replace((char)10, (char)32).Replace((char)13, (char)32);
		}

		static string GetPath(Transform transform)
		{
			var sb = new StringBuilder(transform.name);
			for (var parent = transform.parent; parent != null; parent = parent.parent)
			{
				sb.Insert(0, '/').Insert(0, parent.name);
			}
			return sb.ToString();
		}
	}
}