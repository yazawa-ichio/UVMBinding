using System;
using System.Collections.Generic;
using UnityEngine;
using UVMBinding.Logger;

namespace UVMBinding.Core
{

	public static class ViewUpdater
	{
		static List<IView> s_Views = new List<IView>();
		static Predicate<IView> s_CheckRemove;

		public static bool IsUpdating { get; private set; }

		static ViewUpdater()
		{
			Canvas.preWillRenderCanvases += Update;
			s_CheckRemove = (x) => !x.IsActive;
		}

		public static void Register(IView view)
		{
			Log.Trace("Register ViewUpdater {0}", view);
			s_Views.Add(view);
		}

		static void UpdateImpl()
		{
			bool remove = false;
			for (int i = 0; i < s_Views.Count; i++)
			{
				var view = s_Views[i];
				if (view.IsActive)
				{
					view.TryUpdate();
				}
				else
				{
					remove = true;
				}
			}
			if (remove)
			{
				var count = s_Views.RemoveAll(s_CheckRemove);
				Log.Trace("Remove ViewUpdater {0}", count);
			}
		}

		public static void Update()
		{
			try
			{
				IsUpdating = true;
				UpdateImpl();
			}
			finally
			{
				IsUpdating = false;
			}
		}

	}

}