using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UVMBinding.Lint;

namespace UVMBinding.EditorTests
{
	public class LintScopeTests
	{
		readonly List<Object> m_Assets = new List<Object>();

		[TearDown]
		public void TearDown()
		{
			foreach (var asset in m_Assets)
			{
				if (asset != null) Object.DestroyImmediate(asset);
			}
			m_Assets.Clear();
		}

		T Create<T>() where T : ScriptableObject
		{
			var asset = ScriptableObject.CreateInstance<T>();
			m_Assets.Add(asset);
			return asset;
		}

		static void SetStrings(SerializedProperty list, params string[] values)
		{
			list.arraySize = values.Length;
			for (int i = 0; i < values.Length; i++)
			{
				list.GetArrayElementAtIndex(i).stringValue = values[i];
			}
		}

		LintSettings CreateSettings(string[] excludeFolders = null, LintRule[] disabled = null,
			LintBaseline baseline = null)
		{
			var settings = Create<LintSettings>();
			var so = new SerializedObject(settings);
			SetStrings(so.FindProperty("m_ExcludeFolders"), excludeFolders ?? new string[0]);
			var rules = so.FindProperty("m_DisabledRules");
			rules.arraySize = disabled != null ? disabled.Length : 0;
			for (int i = 0; disabled != null && i < disabled.Length; i++)
			{
				rules.GetArrayElementAtIndex(i).enumValueIndex = (int)disabled[i];
			}
			so.FindProperty("m_Baseline").objectReferenceValue = baseline;
			so.ApplyModifiedPropertiesWithoutUndo();
			return settings;
		}

		LintBaseline CreateBaseline(string expires, string reason, params LintViolation[] entries)
		{
			var baseline = Create<LintBaseline>();
			var so = new SerializedObject(baseline);
			so.FindProperty("m_Expires").stringValue = expires;
			so.FindProperty("m_Reason").stringValue = reason;
			var list = so.FindProperty("m_Entries");
			list.arraySize = entries.Length;
			for (int i = 0; i < entries.Length; i++)
			{
				list.GetArrayElementAtIndex(i).stringValue = LintBaseline.Key(entries[i]);
			}
			so.ApplyModifiedPropertiesWithoutUndo();
			return baseline;
		}

		static LintViolation Sample(LintRule rule = LintRule.UnknownBindPath)
		{
			return new LintViolation("Assets/A.prefab", "Root/Child", rule, "detail");
		}

		[Test]
		public void 除外フォルダの下は対象外になる()
		{
			var settings = CreateSettings(new[] { "Assets/Plugins" });

			Assert.IsTrue(settings.IsExcluded("Assets/Plugins/Third/A.prefab"));
			Assert.IsFalse(settings.IsExcluded("Assets/Game/A.prefab"));
		}

		[Test]
		public void 除外フォルダは区切りまで見る()
		{
			var settings = CreateSettings(new[] { "Assets/Plugin" });

			Assert.IsFalse(settings.IsExcluded("Assets/Plugins/A.prefab"));
			Assert.IsTrue(settings.IsExcluded("Assets/Plugin/A.prefab"));
		}

		[Test]
		public void 末尾の区切りがあってもなくても同じ()
		{
			Assert.IsTrue(CreateSettings(new[] { "Assets/Plugins/" }).IsExcluded("Assets/Plugins/A.prefab"));
			Assert.IsTrue(CreateSettings(new[] { "Assets/Plugins" }).IsExcluded("Assets/Plugins/A.prefab"));
		}

		[Test]
		public void 切った検査項目は違反にならず件数に出る()
		{
			var settings = CreateSettings(disabled: new[] { LintRule.UnknownBindPath });
			var report = new LintReport { Settings = settings };

			report.Add(Sample());

			Assert.AreEqual(0, report.Violations.Count);
			Assert.AreEqual(1, report.Disabled.Count, "黙って消さない");
		}

		[Test]
		public void 切っていない検査項目はそのまま出る()
		{
			var settings = CreateSettings(disabled: new[] { LintRule.UnknownEventName });
			var report = new LintReport { Settings = settings };

			report.Add(Sample());

			Assert.AreEqual(1, report.Violations.Count);
			Assert.AreEqual(0, report.Disabled.Count);
		}

		[Test]
		public void baselineに載っている違反は抑えられ件数に出る()
		{
			var violation = Sample();
			var baseline = CreateBaseline("2999-12-31", "移行中", violation);
			var report = new LintReport { Settings = CreateSettings(), Baseline = baseline };

			report.Add(violation);

			Assert.AreEqual(0, report.Violations.Count);
			Assert.AreEqual(1, report.Baselined.Count, "黙って消さない");
		}

		[Test]
		public void baselineに載っていない違反は出る()
		{
			var baseline = CreateBaseline("2999-12-31", "移行中", Sample());
			var report = new LintReport { Settings = CreateSettings(), Baseline = baseline };

			report.Add(Sample(LintRule.UnknownEventName));

			Assert.AreEqual(1, report.Violations.Count);
		}

		[Test]
		public void 期限切れのbaselineは効かない()
		{
			var baseline = CreateBaseline("2000-01-01", "移行中", Sample());

			Assert.IsFalse(baseline.IsValid(System.DateTime.Now, out var reason));
			StringAssert.Contains("expired", reason);
		}

		[Test]
		public void 理由の無いbaselineは効かない()
		{
			var baseline = CreateBaseline("2999-12-31", "", Sample());

			Assert.IsFalse(baseline.IsValid(System.DateTime.Now, out var reason));
			StringAssert.Contains("reason", reason);
		}

		[Test]
		public void 期限の書かれていないbaselineは効かない()
		{
			var baseline = CreateBaseline("", "移行中", Sample());

			Assert.IsFalse(baseline.IsValid(System.DateTime.Now, out var reason));
			StringAssert.Contains("expiry", reason);
		}

		[Test]
		public void 期限と理由が揃っていれば効く()
		{
			var baseline = CreateBaseline("2999-12-31", "移行中", Sample());

			Assert.IsTrue(baseline.IsValid(System.DateTime.Now, out _));
		}

		[Test]
		public void 鍵は場所と規則で決まる()
		{
			var a = new LintViolation("Assets/A.prefab", "Root/Child", LintRule.UnknownBindPath, "detail A");
			var b = new LintViolation("Assets/A.prefab", "Root/Child", LintRule.UnknownBindPath, "detail B");

			Assert.AreEqual(LintBaseline.Key(a), LintBaseline.Key(b));
		}

		[Test]
		public void 書き出すと今の違反で置き換わる()
		{
			var baseline = CreateBaseline("2999-12-31", "移行中", Sample());

			baseline.Replace(new List<LintViolation> { Sample(LintRule.UnknownEventName) });

			Assert.AreEqual(1, baseline.Count);
			Assert.IsFalse(baseline.Contains(Sample()), "古い分は残らない");
			Assert.IsTrue(baseline.Contains(Sample(LintRule.UnknownEventName)));
		}
	}
}
