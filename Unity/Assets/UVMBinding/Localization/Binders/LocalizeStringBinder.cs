using UnityEngine;
using UnityEngine.Localization.Components;

namespace UVMBinding.Binders
{
	[RequireComponent(typeof(LocalizeStringEvent))]
	public class LocalizeStringBinder : Binder<LocalizeTextKey, LocalizeStringEvent>
	{
		protected override void UpdateValue(LocalizeTextKey value)
		{
			if (value.RawText)
			{
				Target.OnUpdateString?.Invoke(value.EntryName);
			}
			else
			{
				if (!string.IsNullOrEmpty(value.Table))
				{
					Target.SetTable(value.Table);
				}
				if (!string.IsNullOrEmpty(value.EntryName))
				{
					Target.SetEntry(value.EntryName);
				}
			}
		}
	}

}