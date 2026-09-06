using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Linq;
using Unity.CompilationPipeline.Common.Diagnostics;
using UnityEngine.Events;

namespace UVMBinding.CodeGen
{
	public class ViewModelPostProcessor
	{
		static readonly string s_BindAttribute = "UVMBinding.BindAttribute";
		static readonly string s_EventAttribute = "UVMBinding.EventAttribute";
		static readonly string s_IgnoreAttribute = "UVMBinding.IgnoreCodeGenAttribute";
		static readonly string s_DependsOnAttribute = "UVMBinding.DependsOnAttribute";
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

		class ComputedProperty
		{
			public string Path;
			public PropertyDefinition Property;
			public List<string> Sources = new List<string>();
		}

		Dictionary<string, List<ComputedProperty>> m_Dependents = new Dictionary<string, List<ComputedProperty>>();
		List<ComputedProperty> m_Computed = new List<ComputedProperty>();

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
			CollectComputed();
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

		void CollectComputed()
		{
			var paths = new Dictionary<string, PropertyDefinition>();
			foreach (var property in m_TypeDefinition.Properties)
			{
				if (TryGetPropertyPath(property, out var path))
				{
					paths[path] = property;
				}
			}
			foreach (var property in m_TypeDefinition.Properties)
			{
				if (!TryGetPropertyPath(property, out var path))
				{
					continue;
				}
				var attr = property.CustomAttributes.FirstOrDefault(x => x.AttributeType.FullName == s_DependsOnAttribute);
				if (attr == null)
				{
					continue;
				}
				if (GetBackingField(property) != null)
				{
					Error($"[DependsOn] is only for computed properties. {property.FullName}");
					continue;
				}
				if (property.GetMethod == null)
				{
					Error($"[DependsOn] requires a getter. {property.FullName}");
					continue;
				}
				var computed = new ComputedProperty { Path = path, Property = property };
				foreach (var arg in attr.ConstructorArguments)
				{
					if (!(arg.Value is CustomAttributeArgument[] values))
					{
						continue;
					}
					foreach (var value in values)
					{
						var source = value.Value as string;
						if (string.IsNullOrEmpty(source))
						{
							continue;
						}
						if (!paths.ContainsKey(source))
						{
							Error($"[DependsOn] unknown path \"{source}\". {property.FullName}");
							continue;
						}
						computed.Sources.Add(source);
					}
				}
				m_Computed.Add(computed);
			}
			if (m_Computed.Count == 0)
			{
				return;
			}
			if (HasCycle())
			{
				m_Computed.Clear();
				return;
			}
			foreach (var computed in m_Computed)
			{
				foreach (var source in computed.Sources)
				{
					if (!m_Dependents.TryGetValue(source, out var list))
					{
						m_Dependents[source] = list = new List<ComputedProperty>();
					}
					list.Add(computed);
				}
			}
		}

		bool HasCycle()
		{
			var byPath = m_Computed.ToDictionary(x => x.Path);
			var state = new Dictionary<string, int>();
			foreach (var computed in m_Computed)
			{
				if (Visit(computed.Path, byPath, state))
				{
					Error($"[DependsOn] circular dependency. {m_TypeDefinition.FullName}::{computed.Path}");
					return true;
				}
			}
			return false;
		}

		bool Visit(string path, Dictionary<string, ComputedProperty> byPath, Dictionary<string, int> state)
		{
			if (state.TryGetValue(path, out var s))
			{
				return s == 1;
			}
			if (!byPath.TryGetValue(path, out var computed))
			{
				return false;
			}
			state[path] = 1;
			foreach (var source in computed.Sources)
			{
				if (Visit(source, byPath, state))
				{
					return true;
				}
			}
			state[path] = 2;
			return false;
		}

		void CollectTransitive(string path, List<ComputedProperty> result)
		{
			if (!m_Dependents.TryGetValue(path, out var list))
			{
				return;
			}
			foreach (var computed in list)
			{
				if (result.Contains(computed))
				{
					continue;
				}
				result.Add(computed);
				CollectTransitive(computed.Path, result);
			}
		}

		void Error(string message)
		{
			m_Diagnostics.Add(new DiagnosticMessage
			{
				DiagnosticType = DiagnosticType.Error,
				MessageData = message,
			});
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
				var dependents = new List<ComputedProperty>();
				CollectTransitive(path, dependents);
				foreach (var computed in dependents)
				{
					EmitPush(processor, computed);
				}
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

		void EmitPush(ILProcessor processor, ComputedProperty computed)
		{
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Ldstr, computed.Path);
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Call, computed.Property.GetMethod);
			processor.Emit(OpCodes.Call, m_ViewModelReference.SetMethod(computed.Property.PropertyType));
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
			foreach (var computed in SortByDepth())
			{
				processor.InsertBefore(end, Instruction.Create(OpCodes.Ldarg_0));
				processor.InsertBefore(end, Instruction.Create(OpCodes.Ldstr, computed.Path));
				processor.InsertBefore(end, Instruction.Create(OpCodes.Ldarg_0));
				processor.InsertBefore(end, Instruction.Create(OpCodes.Call, computed.Property.GetMethod));
				processor.InsertBefore(end, Instruction.Create(OpCodes.Call,
					m_ViewModelReference.SetMethod(computed.Property.PropertyType)));
			}
			foreach (var property in m_TypeDefinition.Properties)
			{
				if (TryGetEventPath(property, out var path) && IsEventProperty(property))
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

		List<ComputedProperty> SortByDepth()
		{
			var byPath = m_Computed.ToDictionary(x => x.Path);
			var result = new List<ComputedProperty>();
			foreach (var computed in m_Computed)
			{
				AddSorted(computed, byPath, result);
			}
			return result;
		}

		void AddSorted(ComputedProperty computed, Dictionary<string, ComputedProperty> byPath,
			List<ComputedProperty> result, HashSet<string> visiting = null)
		{
			if (result.Contains(computed))
			{
				return;
			}
			visiting ??= new HashSet<string>();
			if (!visiting.Add(computed.Path))
			{
				return;
			}
			foreach (var source in computed.Sources)
			{
				if (byPath.TryGetValue(source, out var parent))
				{
					AddSorted(parent, byPath, result, visiting);
				}
			}
			visiting.Remove(computed.Path);
			result.Add(computed);
		}

		bool IsEventProperty(PropertyDefinition property)
		{
			if (property.GetMethod == null)
			{
				return false;
			}
			if (property.PropertyType.FullName == typeof(System.Action).FullName)
			{
				return true;
			}
			if (property.PropertyType.FullName.StartsWith("System.Action`1<"))
			{
				return true;
			}
			if (property.PropertyType.FullName == typeof(UnityEvent).FullName)
			{
				return true;
			}
			if (property.PropertyType.FullName.StartsWith("UnityEngine.Events.UnityEvent`1<"))
			{
				return true;
			}
			m_Diagnostics.Add(new DiagnosticMessage
			{
				DiagnosticType = DiagnosticType.Warning,
				MessageData = $"not [Event] target. {property.FullName}"
			});
			return false;
		}

		void ProcessRegisterProcessAction(Instruction end, ILProcessor processor, MethodDefinition eventMethod, string path)
		{
			processor.InsertBefore(end, Instruction.Create(OpCodes.Ldarg_0));
			processor.InsertBefore(end, Instruction.Create(OpCodes.Call, m_ViewModelReference.GetEvent()));
			processor.InsertBefore(end, Instruction.Create(OpCodes.Ldstr, path));
			processor.InsertBefore(end, Instruction.Create(OpCodes.Ldarg_0));
			processor.InsertBefore(end, Instruction.Create(OpCodes.Ldftn, eventMethod));
			var returnTypeName = eventMethod.ReturnType.FullName;
			if (returnTypeName == typeof(System.Action).FullName)
			{
				processor.InsertBefore(end, Instruction.Create(OpCodes.Newobj, m_ActionReference.NewFuncAcion()));
				processor.InsertBefore(end, Instruction.Create(OpCodes.Callvirt, m_ViewModelReference.GetEventSubscribeByFunc()));
			}
			else if (returnTypeName.StartsWith(typeof(System.Action).FullName))
			{
				var arg = (eventMethod.ReturnType as GenericInstanceType).GenericArguments[0];
				processor.InsertBefore(end, Instruction.Create(OpCodes.Newobj, m_ActionReference.NewFuncAcion(arg)));
				processor.InsertBefore(end, Instruction.Create(OpCodes.Callvirt, m_ViewModelReference.GetEventSubscribeByFunc(arg)));
			}
			else if (returnTypeName == typeof(UnityEvent).FullName)
			{
				processor.InsertBefore(end, Instruction.Create(OpCodes.Newobj, m_ActionReference.NewFuncUnityEvent()));
				processor.InsertBefore(end, Instruction.Create(OpCodes.Callvirt, m_ViewModelReference.GetEventSubscribeFromUnityEvent()));
			}
			else if (returnTypeName.StartsWith(typeof(UnityEvent).FullName))
			{
				var arg = (eventMethod.ReturnType as GenericInstanceType).GenericArguments[0];
				processor.InsertBefore(end, Instruction.Create(OpCodes.Newobj, m_ActionReference.NewFuncUnityEvent(arg)));
				processor.InsertBefore(end, Instruction.Create(OpCodes.Callvirt, m_ViewModelReference.GetEventSubscribeFromUnityEvent(arg)));
			}
			else
			{
				m_Diagnostics.Add(new DiagnosticMessage()
				{
					DiagnosticType = DiagnosticType.Error,
					MessageData = $"Not Found Event Type. {eventMethod.FullName}"
				});
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