namespace UVMBinding.Showcase
{
	public class ShowcaseGlobalState
	{
		public static readonly ShowcaseGlobalState Instance = new ShowcaseGlobalState();

		[UnityEngine.Scripting.Preserve]
		public string Title { get; set; } = "グローバル状態";

		[UnityEngine.Scripting.Preserve]
		public int Count { get; set; } = 7;
	}
}
