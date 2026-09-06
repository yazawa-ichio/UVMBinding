namespace UVMBinding
{
	public enum SemanticRole
	{
		None = 0,
		Text,
		Heading,
		Image,
		Button,
		Link,
		TextBox,
		SearchBox,
		Checkbox,
		Switch,
		Slider,
		Scrollbar,
		ComboBox,
		List,
		ListItem,
		Group,
		Dialog,
		Tab,
		TabList,
		ProgressBar,
	}

	[System.AttributeUsage(System.AttributeTargets.Class, Inherited = true)]
	public sealed class SemanticRoleAttribute : System.Attribute
	{
		public readonly SemanticRole Role;

		public SemanticRoleAttribute(SemanticRole role)
		{
			Role = role;
		}
	}
}
