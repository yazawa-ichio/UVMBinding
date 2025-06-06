﻿using Mono.Cecil;
using System.Linq;
using UnityEngine.Events;

namespace UVMBinding.CodeGen
{
	public class ActionReference
	{
		ModuleDefinition m_Module;
		MethodDefinition m_Action;
		MethodDefinition m_UnityEvent;
		TypeReference m_ActionGeneric;
		TypeReference m_UnityEventGeneric;
		TypeReference m_FuncGeneric;

		public ActionReference(ModuleDefinition module)
		{
			m_Module = module;
			m_Action = module.ImportReference(typeof(System.Action)).Resolve().Methods.First(x => x.IsConstructor);
			m_UnityEvent = module.ImportReference(typeof(UnityEngine.Events.UnityEvent)).Resolve().Methods.First(x => x.IsConstructor);
			m_ActionGeneric = module.ImportReference(typeof(System.Action<>)).Resolve();
			m_UnityEventGeneric = module.ImportReference(typeof(UnityEngine.Events.UnityEvent<>)).Resolve();
			m_FuncGeneric = module.ImportReference(typeof(System.Func<>)).Resolve();
		}

		public MethodReference NewMethod()
		{
			return m_Module.ImportReference(m_Action);
		}

		public MethodReference NewMethod(TypeReference arg)
		{
			var actionType = new GenericInstanceType(m_ActionGeneric)
			{
				GenericArguments = { arg }
			};
			var actionConstructor = actionType.Resolve().Methods.First(x => x.IsConstructor);
			return m_Module.ImportReference(actionConstructor.MakeHostInstanceGeneric(arg));
		}

		public MethodReference NewFuncAcion()
		{
			return NewFunc(m_Module.ImportReference(typeof(System.Action)).Resolve());
		}

		public MethodReference NewFuncUnityEvent()
		{
			return NewFunc(m_Module.ImportReference(typeof(UnityEvent)).Resolve());
		}

		public MethodReference NewFuncAcion(TypeReference arg)
		{
			GenericInstanceType type = new GenericInstanceType(m_ActionGeneric.Resolve())
			{
				GenericArguments = { arg }
			};
			return NewFunc(m_Module.ImportReference(type));
		}

		public MethodReference NewFuncUnityEvent(TypeReference arg)
		{
			GenericInstanceType type = new GenericInstanceType(m_UnityEventGeneric.Resolve())
			{
				GenericArguments = { arg }
			};
			return NewFunc(m_Module.ImportReference(type));
		}

		public MethodReference NewFunc(TypeReference arg)
		{
			var funcType = new GenericInstanceType(m_FuncGeneric)
			{
				GenericArguments = { arg }
			};
			var actionConstructor = m_Module.ImportReference(funcType).Resolve().Methods.First(x => x.IsConstructor);
			return m_Module.ImportReference(actionConstructor.MakeHostInstanceGeneric(arg));
		}

	}

}