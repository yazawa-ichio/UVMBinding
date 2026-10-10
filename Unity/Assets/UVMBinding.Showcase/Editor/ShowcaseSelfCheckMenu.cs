using UnityEditor;
using UVMBinding.Showcase;

public static class ShowcaseSelfCheckMenu
{
	const string MenuPath = "UVMBinding/Showcase/Self Check On Play";

	[MenuItem(MenuPath)]
	static void Toggle()
	{
		var value = !EditorPrefs.GetBool(ShowcaseSelfCheck.EditorEnabledKey, false);
		EditorPrefs.SetBool(ShowcaseSelfCheck.EditorEnabledKey, value);
	}

	[MenuItem(MenuPath, true)]
	static bool ToggleValidate()
	{
		Menu.SetChecked(MenuPath, EditorPrefs.GetBool(ShowcaseSelfCheck.EditorEnabledKey, false));
		return true;
	}
}
