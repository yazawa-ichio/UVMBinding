using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ILib
{
	public static class SolutionSync
	{
		[MenuItem("Tools/SolutionSync")]
		public static void Run()
		{
			AssetDatabase.Refresh();
			var assembly = typeof(Editor).Assembly;
			var syncVS = assembly.GetType("UnityEditor.SyncVS");
			var synchronizer = assembly.GetType("UnityEditor.VisualStudioIntegration.SolutionSynchronizer");
			var obj = syncVS.GetField("Synchronizer", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
			synchronizer.GetMethod("Sync", BindingFlags.Instance | BindingFlags.Public).Invoke(obj, new object[] { });
			Debug.Log("Sync");
		}
	}
}