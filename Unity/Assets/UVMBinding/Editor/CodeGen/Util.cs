using Mono.Cecil;

namespace UVMBinding.CodeGen
{

	public static class Util
	{

		public static MethodReference MakeHostInstanceGeneric(this MethodReference self, TypeReference arg)
		{
			var declaringType = new GenericInstanceType(self.DeclaringType) { GenericArguments = { arg } };
			var reference = new MethodReference(self.Name, self.ReturnType, declaringType)
			{
				HasThis = self.HasThis,
				ExplicitThis = self.ExplicitThis,
				CallingConvention = self.CallingConvention
			};
			foreach (var parameter in self.Parameters)
			{
				reference.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
			}
			foreach (var genericParam in self.GenericParameters)
			{
				reference.GenericParameters.Add(new GenericParameter(genericParam.Name, reference));
			}
			return reference;
		}


	}

}