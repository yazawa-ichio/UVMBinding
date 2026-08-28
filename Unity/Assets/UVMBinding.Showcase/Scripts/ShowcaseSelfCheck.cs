using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace UVMBinding.Showcase
{
	//ストリッピング検証用の自己診断。
	//[SerializeReference] の型が落ちても例外は出ず null になるだけなので、
	//実行して目視するだけでは検出できない。走査して機械可読な形で出力する。
	//Editor 実行の結果を基準にして、プレイヤー実行の結果と突き合わせる。
	public class ShowcaseSelfCheck : MonoBehaviour
	{
		const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
		const int MaxDepth = 8;
		const string Tag = "[SelfCheck]";
		const char Newline = (char)10;

		[SerializeField]
		int m_DelayFrames = 3;

		readonly List<string> m_Errors = new List<string>();
		readonly List<string> m_Texts = new List<string>();

		IEnumerator Start()
		{
			Application.logMessageReceived += OnLog;
			for (int i = 0; i < m_DelayFrames; i++)
			{
				yield return null;
			}
			//全画面を巡回してバインドを実際に走らせる。
			//逆直列化が通っても、リフレクション経路やジェネリック実体化は
			//バインドを実行しないと確かめられないため。
			var tabs = FindFirstObjectByType<ShowcaseTabs>(FindObjectsInactive.Include);
			if (tabs != null)
			{
				for (int i = 0; i < tabs.ScreenCount; i++)
				{
					tabs.Select(i);
					for (int f = 0; f < m_DelayFrames; f++)
					{
						yield return null;
					}
					//表示中のテキストを控える。例外が出ないことと
					//正しく表示されることは別なので、内容そのものを突き合わせる
					SnapshotTexts();
				}
				tabs.Select(0);
				yield return null;
			}
			Application.logMessageReceived -= OnLog;
			Run();
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
				//シーン上の実体のみを対象にする。アセットや隠しオブジェクトは除外
				if (behaviour == null || !behaviour.gameObject.scene.IsValid())
				{
					continue;
				}
				Scan(behaviour, GetPath(behaviour.transform) + ":" + behaviour.GetType().Name, 0, types, nulls, ref slots);
			}

			//走査順は決まらないので並べ替える。基準値との差分が順序で揺れないようにするため
			nulls.Sort(StringComparer.Ordinal);
			m_Texts.Sort(StringComparer.Ordinal);
			m_Errors.Sort(StringComparer.Ordinal);

			var sb = new StringBuilder();
			sb.Append(Tag).Append(" BEGIN\n");
			foreach (var pair in types)
			{
				sb.Append(Tag).Append(" type ").Append(pair.Key).Append(' ').Append(pair.Value).Append('\n');
			}
			foreach (var path in nulls)
			{
				sb.Append(Tag).Append(" null ").Append(path).Append('\n');
			}
			foreach (var text in m_Texts)
			{
				sb.Append(Tag).Append(" text ").Append(text).Append(Newline);
			}
			foreach (var error in m_Errors)
			{
				sb.Append(Tag).Append(" error ").Append(Flatten(error)).Append(Newline);
			}
			sb.Append(Tag).Append(" END slots=").Append(slots)
				.Append(" nulls=").Append(nulls.Count)
				.Append(" types=").Append(types.Count)
				.Append(" errors=").Append(m_Errors.Count)
				.Append(" texts=").Append(m_Texts.Count);
			//1 メッセージが長すぎるとログ側で切り詰められるため、本体はファイルに書く。
			//出力先は -selfcheck-out で指定でき、既定は persistentDataPath。
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
				+ " texts=" + m_Texts.Count + " out=" + outputPath);
			//検証を 1 コマンドで完結させるため、指示があれば自分で終了する
			if (HasArg("-selfcheck-quit"))
			{
				Application.Quit(m_Errors.Count == 0 ? 0 : 1);
			}
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
			//プレイヤーでは実行ファイルの隣に出す。Makefile 側でパスを組み立てずに済む
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
			//入れ子の SerializeReference（Concatenate の段や Provider の合成）まで辿る
			Scan(value, path, depth + 1, types, nulls, ref slots);
		}

		void SnapshotTexts()
		{
			foreach (var text in FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None))
			{
				if (text.isActiveAndEnabled)
				{
					m_Texts.Add(GetPath(text.transform) + "=" + Flatten(text.text));
				}
			}
		}

		//改行を潰して 1 行にする。エスケープを書かずに済むよう文字コードで指定する
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