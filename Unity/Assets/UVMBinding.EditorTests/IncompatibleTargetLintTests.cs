using NUnit.Framework;
using UnityEngine;
using UVMBinding.Lint;

namespace UVMBinding.EditorTests
{
	public class IncompatibleTargetLintTests
	{
		class TargetTestViewModel : ViewModel
		{
			[Bind] public Vector3 Offset { get; set; }
			[Bind] public bool Visible { get; set; }
		}

		GameObject m_Root;

		[TearDown]
		public void TearDown()
		{
			if (m_Root != null)
			{
				Object.DestroyImmediate(m_Root);
				m_Root = null;
			}
		}

		View CreateView()
		{
			m_Root = new GameObject("View");
			var view = m_Root.AddComponent<View>();
			var so = new UnityEditor.SerializedObject(view);
			so.FindProperty(View.ReservationViewModelFieldName).stringValue = typeof(TargetTestViewModel).FullName;
			so.ApplyModifiedPropertiesWithoutUndo();
			return view;
		}

		T AddChild<T>(string name, bool rect) where T : Component
		{
			var go = rect ? new GameObject(name, typeof(RectTransform)) : new GameObject(name);
			go.transform.SetParent(m_Root.transform, false);
			return go.AddComponent<T>();
		}

		static void SetPath(Component binder, string path)
		{
			var so = new UnityEditor.SerializedObject(binder);
			so.FindProperty("m_Path").stringValue = path;
			so.ApplyModifiedPropertiesWithoutUndo();
		}

		static LintReport Lint(View view)
		{
			var report = new LintReport();
			ViewLinter.Lint(view, "test", report);
			return report;
		}

		[Test]
		public void RectTransformの上のPosition3DBinderを検出できる()
		{
			var view = CreateView();
			SetPath(AddChild<Binders.Position3DBinder>("Moving", true), "Offset");

			var report = Lint(view);

			Assert.AreEqual(1, report.Violations.Count, report.Summarize());
			Assert.AreEqual(LintRule.IncompatibleTarget, report.Violations[0].Rule);
			StringAssert.Contains("AnchoredPositionBinder", report.Violations[0].Detail,
				"代わりに何を使うかまで出す");
		}

		[Test]
		public void RectTransformでなければ違反にならない()
		{
			var view = CreateView();
			SetPath(AddChild<Binders.Position3DBinder>("Moving", false), "Offset");

			var report = Lint(view);

			Assert.AreEqual(0, report.Violations.Count, report.Summarize());
		}

		[Test]
		public void 宣言を持たないBinderは影響を受けない()
		{
			var view = CreateView();
			SetPath(AddChild<Binders.ActiveBinder>("Panel", true), "Visible");

			var report = Lint(view);

			Assert.AreEqual(0, report.Violations.Count, report.Summarize());
		}

		[Test]
		public void LintIgnoreで除外できる()
		{
			var view = CreateView();
			var binder = AddChild<Binders.Position3DBinder>("Moving", true);
			SetPath(binder, "Offset");
			var ignore = binder.gameObject.AddComponent<LintIgnore>();
			var so = new UnityEditor.SerializedObject(ignore);
			so.FindProperty("m_Reason").stringValue = "意図した使い方";
			so.ApplyModifiedPropertiesWithoutUndo();

			var report = Lint(view);

			Assert.AreEqual(0, report.Violations.Count);
			Assert.AreEqual(1, report.Ignored.Count);
		}
	}
}
