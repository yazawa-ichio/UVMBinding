namespace UVMBinding
{
	internal interface IBindingProperty
	{
		event System.Action OnPostChanged;
		string Path { get; }
		int Hash { get; }
		object GetObject();
		void SetObject(object val);
		System.Type GetBindType();
		bool IsAssignable<T>();
		void SetDirty();
	}

	internal interface IBindingProperty<T> : IBindingProperty
	{
		T Value { get; set; }
		event System.Action<T> OnChanged;
	}
}