using Mono.Cecil;

namespace UVMBinding.CodeGen
{
	public class ViewModelReference
	{
		ModuleDefinition m_Module;
		TypeReference m_Reference;

		MethodDefinition m_Set;
		MethodDefinition m_Get;

		MethodDefinition m_EventSubscribe;
		MethodDefinition m_EventSubscribeGeneric;
		MethodDefinition m_EventSubscribeByFunc;
		MethodDefinition m_EventSubscribeByFuncGeneric;
		MethodDefinition m_EventUnsubscribe;
		MethodDefinition m_EventUnsubscribeGeneric;

		MethodDefinition m_Event;
		MethodDefinition m_Property;

		public ViewModelReference(ModuleDefinition module, TypeReference reference)
		{
			m_Module = module;
			m_Reference = reference;
			foreach (var method in reference.Resolve().Methods)
			{
				switch (method.Name)
				{
					case "SetImpl":
						m_Set = method;
						break;
					case "GetImpl":
						m_Get = method;
						break;
					case "get_Event":
						m_Event = method;
						break;
					case "get_Property":
						m_Property = method;
						break;
				}
			}
			var e = m_Event.ReturnType.Resolve();
			foreach (var method in e.Methods)
			{
				switch (method.Name)
				{
					case "Subscribe":
						if (!method.HasGenericParameters)
						{
							m_EventSubscribe = method;
						}
						else
						{
							m_EventSubscribeGeneric = method;
						}
						break;
					case "SubscribeByFunc":
						if (!method.HasGenericParameters)
						{
							m_EventSubscribeByFunc = method;
						}
						else
						{
							m_EventSubscribeByFuncGeneric = method;
						}
						break;
					case "Unsubscribe":
						if (!method.HasGenericParameters)
						{
							m_EventUnsubscribe = method;
						}
						else
						{
							m_EventUnsubscribeGeneric = method;
						}
						break;
				}
			}
		}

		public MethodReference SetMethod(TypeReference type)
		{
			return m_Module.ImportReference(new GenericInstanceMethod(m_Set)
			{
				GenericArguments = { type }
			});
		}

		public MethodReference GetMethod(TypeReference type)
		{
			return m_Module.ImportReference(new GenericInstanceMethod(m_Get)
			{
				GenericArguments = { type }
			});
		}

		public MethodReference GetEvent()
		{
			return m_Module.ImportReference(m_Event);
		}

		public MethodReference GetEventSubscribe()
		{
			return m_Module.ImportReference(m_EventSubscribe);
		}

		public MethodReference GetEventSubscribe(TypeReference type)
		{
			return m_Module.ImportReference(new GenericInstanceMethod(m_EventSubscribeGeneric)
			{
				GenericArguments = { type }
			});
		}

		public MethodReference GetEventSubscribeByFunc()
		{
			return m_Module.ImportReference(m_EventSubscribeByFunc);
		}

		public MethodReference GetEventSubscribeByFunc(TypeReference type)
		{
			return m_Module.ImportReference(new GenericInstanceMethod(m_EventSubscribeByFuncGeneric)
			{
				GenericArguments = { type }
			});
		}

		public MethodReference GetEventUnsubscribe()
		{
			return m_Module.ImportReference(m_EventUnsubscribe);
		}

		public MethodReference GetEventUnsubscribe(TypeReference type)
		{
			return m_Module.ImportReference(new GenericInstanceMethod(m_EventUnsubscribeGeneric)
			{
				GenericArguments = { type }
			});
		}

	}

}