using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace UVMBinding.Lint
{
	public static class LintRunner
	{
		public static LintReport Run(string[] searchInFolders = null, bool useBaseline = true)
		{
			var report = new LintReport();
			var settings = LintSettings.Find();
			ViewLinter.Settings = settings;
			report.Settings = settings;
			if (useBaseline)
			{
				UseBaseline(settings, report);
			}
			var watch = Stopwatch.StartNew();
			int prefabCount = LintPrefabs(report, searchInFolders);
			int sceneCount = LintScenes(report, searchInFolders);
			LintUnusedEvents(report, searchInFolders);
			ViewLinter.LintConsequences(report);
			watch.Stop();
			UnityEngine.Debug.Log($"[Lint] {report.Summarize()} / prefab {prefabCount} / scene {sceneCount} / {watch.ElapsedMilliseconds}ms");
			return report;
		}

		static void UseBaseline(LintSettings settings, LintReport report)
		{
			var baseline = settings != null ? settings.Baseline : null;
			if (baseline == null)
			{
				return;
			}
			if (baseline.IsValid(System.DateTime.Now, out var reason))
			{
				report.Baseline = baseline;
				return;
			}
			UnityEngine.Debug.LogWarning(
				$"[Lint] {reason} {baseline.Count} entries are no longer suppressed.");
		}

		static void LintUnusedEvents(LintReport report, string[] searchInFolders)
		{
			if (searchInFolders == null)
			{
				ViewLinter.LintUnusedEvents(report);
				return;
			}
			report.Partial.Add(new LintSkipped(string.Empty, string.Empty,
				$"{LintRule.UnusedEventName} skipped because the sweep is limited to specific folders"));
		}

		static int LintPrefabs(LintReport report, string[] searchInFolders)
		{
			var guids = AssetDatabase.FindAssets("t:Prefab", searchInFolders);
			int count = 0;
			foreach (var guid in guids)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				if (IsExcluded(report, path))
				{
					continue;
				}
				var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				if (asset == null)
				{
					continue;
				}
				count++;
				LintRoot(asset, path, report);
			}
			return count;
		}

		static int LintScenes(LintReport report, string[] searchInFolders)
		{
			var guids = AssetDatabase.FindAssets("t:Scene", searchInFolders);
			var opened = new List<string>();
			foreach (var guid in guids)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				if (!path.StartsWith("Assets/") || IsExcluded(report, path))
				{
					continue;
				}
				opened.Add(path);
			}
			if (opened.Count == 0)
			{
				return 0;
			}
			var setup = EditorSceneManager.GetSceneManagerSetup();
			try
			{
				foreach (var path in opened)
				{
					var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
					foreach (var root in scene.GetRootGameObjects())
					{
						LintRoot(root, path, report);
					}
				}
			}
			finally
			{
				if (setup != null && setup.Length > 0)
				{
					EditorSceneManager.RestoreSceneManagerSetup(setup);
				}
			}
			return opened.Count;
		}

		static bool IsExcluded(LintReport report, string path)
		{
			if (report.Settings == null || !report.Settings.IsExcluded(path))
			{
				return false;
			}
			report.ExcludedAssetCount++;
			return true;
		}

		static void LintRoot(GameObject root, string path, LintReport report)
		{
			foreach (var view in root.GetComponentsInChildren<View>(true))
			{
				ViewLinter.Lint(view, path, report);
			}
			foreach (var provider in root.GetComponentsInChildren<ViewModelSourceProvider>(true))
			{
				ViewLinter.Lint(provider, path, report);
			}
		}

		public static void RunFromCommandLine()
		{
			Log(Run());
		}

		[MenuItem("UVMBinding/Lint/Run")]
		static void RunFromMenu()
		{
			Log(Run());
		}

		[MenuItem("UVMBinding/Lint/Update Baseline")]
		static void UpdateBaseline()
		{
			var settings = LintSettings.Find();
			var baseline = settings != null ? settings.Baseline : null;
			if (baseline == null)
			{
				UnityEngine.Debug.LogWarning(
					"[Lint] No LintBaseline is assigned. Create one from Assets/Create/UVMBinding/Lint Baseline"
					+ " and set it on LintSettings.");
				return;
			}
			var report = Run(null, false);
			baseline.Replace(report.Violations);
			EditorUtility.SetDirty(baseline);
			AssetDatabase.SaveAssets();
			UnityEngine.Debug.Log($"[Lint] Baseline updated. {baseline.Count} entries."
				+ " Set an expiry date and a reason, or it will not suppress anything.");
		}

		static void Log(LintReport report)
		{
			foreach (var violation in report.Violations)
			{
				UnityEngine.Debug.LogWarning($"[Lint] {violation}");
			}
			foreach (var warning in report.Warnings)
			{
				UnityEngine.Debug.Log($"[Lint] warning {warning}");
			}
			foreach (var baselined in report.Baselined)
			{
				UnityEngine.Debug.Log($"[Lint] baselined {baselined}");
			}
			foreach (var disabled in report.Disabled)
			{
				UnityEngine.Debug.Log($"[Lint] disabled {disabled}");
			}
			foreach (var partial in report.Partial)
			{
				UnityEngine.Debug.Log($"[Lint] partial {partial}");
			}
			foreach (var skipped in report.Skipped)
			{
				UnityEngine.Debug.Log($"[Lint] skipped {skipped}");
			}
		}
	}
}
