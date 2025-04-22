namespace UVMBinding
{
	internal interface IBindingProperty
	{
		string Path { get; }
		int Hash { get; }
		object GetObject();
		void SetObject(object val);
		System.Type GetBindType();
		bool IsAssignable<T>();
		void SetDirty();
		event System.Action<string, object> OnChangedObject;
	}

	internal interface IBindingProperty<T> : IBindingProperty
	{
		T Value { get; set; }
		event System.Action<T> OnChanged;
	}
}