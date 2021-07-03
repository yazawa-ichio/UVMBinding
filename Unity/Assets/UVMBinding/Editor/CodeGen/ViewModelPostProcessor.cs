using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Linq;
using Unity.CompilationPipeline.Common.Diagnostics;

namespace UVMBinding.CodeGen
{
	public class ViewModelPostProcessor
	{
		static readonly string s_BindAttribute = "UVMBinding.BindAttribute";
		static readonly string s_EventAttribute = "UVMBinding.EventAttribute";
		static readonly string s_IgnoreAttribute = "UVMBinding.IgnoreCodeGenAttribute";
		static readonly string s_ReactiveType = "UVMBinding.Property`1<";
		static readonly string s_CollectionType = "UVMBinding.Collection`1<";
		static readonly string s_ValueCollectionType = "UVMBinding.ValueCollection`1<";

		public enum InitType
		{
			Value,
			Property,
			Collection,
		}

		class InitProperty
		{
			public InitType Type;
			public string Path;
			public FieldDefinition BackingField;
			public PropertyDefinition Property;
		}

		TypeDefinition m_TypeDefinition;
		ViewModelReference m_ViewModelReference;
		ActionReference m_ActionReference;
		List<InitProperty> m_InitProperty = new List<InitProperty>();
		List<DiagnosticMessage> m_Diagnostics;

		public ViewModelPostProcessor(TypeDefinition typeDefinition, TypeReference vmReference, List<DiagnosticMessage> diagnostics)
		{
			m_TypeDefinition = typeDefinition;
			m_ViewModelReference = new ViewModelReference(typeDefinition.Module, vmReference);
			m_ActionReference = new ActionReference(typeDefinition.Module);
			m_Diagnostics = diagnostics;
		}

		bool TryGetPropertyPath(IMemberDefinition member, out string path)
		{
			return TryGetPath(s_BindAttribute, member, out path);
		}

		bool TryGetEventPath(IMemberDefinition member, out string path)
		{
			return TryGetPath(s_EventAttribute, member, out path);
		}

		bool TryGetPath(string attributeName, IMemberDefinition member, out string path)
		{
			path = member.Name;
			if (member.CustomAttributes.Any(x => x.AttributeType.FullName == s_IgnoreAttribute))
			{
				return false;
			}
			var attr = member.CustomAttributes.FirstOrDefault(x => x.AttributeType.FullName == attributeName);
			if (attr == null)
			{
				return false;
			}
			foreach (var arg in attr.ConstructorArguments)
			{
				path = arg.Value.ToString();
			}
			return true;
		}

		public void Process()
		{
			foreach (var property in m_TypeDefinition.Properties)
			{
				if (TryGetPropertyPath(property, out var path))
				{
					ProcessProperty(property, path);
				}
			}
			foreach (var _event in m_TypeDefinition.Events)
			{
				if (TryGetEventPath(_event, out var path))
				{
					ProcessEvent(_event, path);
				}
			}
			foreach (var method in m_TypeDefinition.Methods)
			{
				if (!method.IsConstructor || method.IsStatic)
				{
					continue;
				}
				ProcessConstructor(method);
			}

		}

		FieldDefinition GetBackingField(PropertyDefinition property)
		{
			var index = property.FullName.IndexOf("::");
			var field = property.FullName.Substring(0, index) + "::<" + property.Name + ">";
			return m_TypeDefinition.Fields.FirstOrDefault(x => x.FullName.StartsWith(field));
		}

		void ProcessProperty(PropertyDefinition property, string path)
		{
			var backingField = GetBackingField(property);
			if (backingField == null)
			{
				return;
			}
			var name = property.PropertyType.FullName;
			InitType type;
			if (name.StartsWith(s_ReactiveType))
			{
				type = InitType.Property;
			}
			else if (name.StartsWith(s_CollectionType) || name.StartsWith(s_ValueCollectionType))
			{
				type = InitType.Collection;
			}
			else
			{
				type = InitType.Value;
				ProcessPropertyValue(property, path);
			}
			m_InitProperty.Add(new InitProperty
			{
				Type = type,
				Path = path,
				BackingField = backingField,
				Property = property,
			});
		}

		void ProcessPropertyValue(PropertyDefinition property, string path)
		{
			if (property.SetMethod != null)
			{
				var body = property.SetMethod.Body;
				body.Instructions.Clear();
				var processor = body.GetILProcessor();
				processor.Emit(OpCodes.Ldarg_0);
				processor.Emit(OpCodes.Ldstr, path);
				processor.Emit(OpCodes.Ldarg_1);
				processor.Emit(OpCodes.Call, m_ViewModelReference.SetMethod(property.PropertyType));
				processor.Emit(OpCodes.Nop);
				processor.Emit(OpCodes.Ret);
			}
			if (property.GetMethod != null)
			{
				var body = property.GetMethod.Body;
				body.Instructions.Clear();
				var processor = body.GetILProcessor();
				processor.Emit(OpCodes.Ldarg_0);
				processor.Emit(OpCodes.Ldstr, path);
				processor.Emit(OpCodes.Call, m_ViewModelReference.GetMethod(property.PropertyType));
				processor.Emit(OpCodes.Nop);
				processor.Emit(OpCodes.Ret);
			}
		}

		void ProcessEvent(EventDefinition _event, string path)
		{
			if (_event.AddMethod != null)
			{
				var body = _event.AddMethod.Body;
				body.Instructions.Clear();
				var processor = body.GetILProcessor();
				processor.Emit(OpCodes.Ldarg_0);
				processor.Emit(OpCodes.Call, m_ViewModelReference.GetEvent());
				processor.Emit(OpCodes.Ldstr, path);
				processor.Emit(OpCodes.Ldarg_1);
				if (_event.EventType is IGenericInstance gi)
				{
					processor.Emit(OpCodes.Callvirt, m_ViewModelReference.GetEventSubscribe(gi.GenericArguments[0]));
				}
				else
				{
					processor.Emit(OpCodes.Callvirt, m_ViewModelReference.GetEventSubscribe());
				}
				processor.Emit(OpCodes.Pop);
				processor.Emit(OpCodes.Ret);
			}
			if (_event.RemoveMethod != null)
			{
				var body = _event.RemoveMethod.Body;
				body.Instructions.Clear();
				var processor = body.GetILProcessor();
				processor.Emit(OpCodes.Ldarg_0);
				processor.Emit(OpCodes.Call, m_ViewModelReference.GetEvent());
				processor.Emit(OpCodes.Ldstr, path);
				processor.Emit(OpCodes.Ldarg_1);
				if (_event.EventType is IGenericInstance gi)
				{
					processor.Emit(OpCodes.Callvirt, m_ViewModelReference.GetEventUnsubscribe(gi.GenericArguments[0]));
				}
				else
				{
					processor.Emit(OpCodes.Callvirt, m_ViewModelReference.GetEventUnsubscribe());
				}
				processor.Emit(OpCodes.Nop);
				processor.Emit(OpCodes.Ret);
			}
		}

		void ProcessConstructor(MethodDefinition method)
		{
			var end = method.Body.Instructions.FirstOrDefault(x =>
			{
				if (x.OpCode != OpCodes.Call || x.Operand == null)
				{
					return false;
				}
				return (x.Operand is MethodReference m && m.Resolve().IsConstructor);
			}).Next;
			var processor = method.Body.GetILProcessor();
			foreach (var init in m_InitProperty)
			{
				switch (init.Type)
				{
					case InitType.Property:
					case InitType.Collection:
						processor.InsertBefore(end, Instruction.Create(OpCodes.Ldarg_0));
						processor.InsertBefore(end, Instruction.Create(OpCodes.Ldstr, init.Path));
						processor.InsertBefore(end, Instruction.Create(OpCodes.Ldarg_0));

						var arg = m_TypeDefinition.Module.ImportReference((init.Property.PropertyType as IGenericInstance).GenericArguments[0]);
						var constructorBase = init.Property.PropertyType.Resolve().Methods.Where(x => x.IsConstructor && x.Parameters.Count == 2).FirstOrDefault();
						var constructor = constructorBase.MakeHostInstanceGeneric(arg);
						processor.InsertBefore(end, Instruction.Create(OpCodes.Newobj, m_TypeDefinition.Module.ImportReference(constructor)));
						processor.InsertBefore(end, Instruction.Create(OpCodes.Call, init.Property.SetMethod));
						break;
					default:
						processor.InsertBefore(end, Instruction.Create(OpCodes.Ldarg_0));
						processor.InsertBefore(end, Instruction.Create(OpCodes.Ldarg_0));
						processor.InsertBefore(end, Instruction.Create(OpCodes.Ldfld, init.BackingField));
						processor.InsertBefore(end, Instruction.Create(OpCodes.Call, init.Property.SetMethod));
						break;
				}
			}
			foreach (var property in m_TypeDefinition.Properties)
			{
				if (TryGetEventPath(property, out var path) && property.GetMethod != null)
				{
					ProcessRegisterProcessAction(end, processor, property.GetMethod, path);
				}
			}
			foreach (var eventMethod in m_TypeDefinition.Methods)
			{
				if (TryGetEventPath(eventMethod, out var path))
				{
					ProcessRegisterMethod(end, processor, eventMethod, path);
				}
			}
		}

		void ProcessRegisterProcessAction(Instruction end, ILProcessor processor, MethodDefinition eventMethod, string path)
		{
			processor.InsertBefore(end, Instruction.Create(OpCodes.Ldarg_0));
			processor.InsertBefore(end, Instruction.Create(OpCodes.Call, m_ViewModelReference.GetEvent()));
			processor.InsertBefore(end, Instruction.Create(OpCodes.Ldstr, path));
			processor.InsertBefore(end, Instruction.Create(OpCodes.Ldarg_0));
			processor.InsertBefore(end, Instruction.Create(OpCodes.Ldftn, eventMethod));
			if (eventMethod.ReturnType.FullName != typeof(System.Action).FullName)
			{
				var arg = (eventMethod.ReturnType as GenericInstanceType).GenericArguments[0];
				processor.InsertBefore(end, Instruction.Create(OpCodes.Newobj, m_ActionReference.NewFuncAcion(arg)));
				processor.InsertBefore(end, Instruction.Create(OpCodes.Callvirt, m_ViewModelReference.GetEventSubscribeByFunc(arg)));
			}
			else
			{
				processor.InsertBefore(end, Instruction.Create(OpCodes.Newobj, m_ActionReference.NewFuncAcion()));
				processor.InsertBefore(end, Instruction.Create(OpCodes.Callvirt, m_ViewModelReference.GetEventSubscribeByFunc()));
			}
			processor.InsertBefore(end, Instruction.Create(OpCodes.Pop));
		}

		void ProcessRegisterMethod(Instruction end, ILProcessor processor, MethodDefinition eventMethod, string path)
		{
			processor.InsertBefore(end, Instruction.Create(OpCodes.Ldarg_0));
			processor.InsertBefore(end, Instruction.Create(OpCodes.Call, m_ViewModelReference.GetEvent()));
			processor.InsertBefore(end, Instruction.Create(OpCodes.Ldstr, path));
			processor.InsertBefore(end, Instruction.Create(OpCodes.Ldarg_0));
			processor.InsertBefore(end, Instruction.Create(OpCodes.Ldftn, eventMethod));
			if (eventMethod.HasParameters)
			{
				var arg = eventMethod.Parameters[0].ParameterType;
				processor.InsertBefore(end, Instruction.Create(OpCodes.Newobj, m_ActionReference.NewMethod(arg)));
				processor.InsertBefore(end, Instruction.Create(OpCodes.Callvirt, m_ViewModelReference.GetEventSubscribe(arg)));
			}
			else
			{
				processor.InsertBefore(end, Instruction.Create(OpCodes.Newobj, m_ActionReference.NewMethod()));
				processor.InsertBefore(end, Instruction.Create(OpCodes.Callvirt, m_ViewModelReference.GetEventSubscribe()));
			}
			processor.InsertBefore(end, Instruction.Create(OpCodes.Pop));
		}

		void Log(string message)
		{
			m_Diagnostics.Add(new DiagnosticMessage()
			{
				DiagnosticType = DiagnosticType.Warning,
				MessageData = message
			});
		}

	}

}