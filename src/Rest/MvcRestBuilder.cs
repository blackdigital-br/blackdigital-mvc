using BlackDigital.DataBuilder;
using BlackDigital.Rest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using TypeBuilder = System.Reflection.Emit.TypeBuilder;

namespace BlackDigital.Mvc.Rest
{
    public class MvcRestBuilder
    {
        public MvcRestBuilder(IServiceCollection serviceCollection)
        {
            Services = new();
            ServiceCollection = serviceCollection;
        }

        protected internal List<Type> Services { get; private set; }
        protected IServiceCollection ServiceCollection { get; private set; }
        private const string ASSEMBLYNAME = "BlackDigital.Mvc.Controllers";

        public MvcRestBuilder AddService<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            ServiceCollection.AddScoped<TService, TImplementation>();
            Services.Add(typeof(TService));
            return this;
        }

        public void Build()
        {
            AssemblyName assemblyName = new(ASSEMBLYNAME);
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);

            foreach (var interfaceType in Services)
            {
                var controllerType = BuildType(moduleBuilder, interfaceType);
                //ServiceCollection.AddScoped(controllerType);
            }
        }

        private static Type BuildType(ModuleBuilder moduleBuilder, Type interfaceType)
        {
            var baseControllerType = typeof(BaseController<>);
            var baseType = baseControllerType.MakeGenericType(interfaceType);

            TypeBuilder typeBuilder = moduleBuilder.DefineType($"{ASSEMBLYNAME}.{interfaceType.Name}Controller", TypeAttributes.Public, baseType);
            typeBuilder.AddInterfaceImplementation(interfaceType);
            CreateTypeAttributtes(typeBuilder, interfaceType);
            CreateConstructor(typeBuilder, interfaceType, baseType);

            foreach (var method in interfaceType.GetMethods())
            {
                CreateInterfaceMethod(typeBuilder, method, baseType);
            }

            return typeBuilder.CreateType();
        }

        #region "Create Attributes"

        private static void CreateTypeAttributtes(TypeBuilder typeBuilder, Type interfaceType)
        {
            typeBuilder.AddCustomAttributeToType<ApiControllerAttribute>();
            var serviceAttribute = interfaceType.GetCustomAttribute<ServiceAttribute>();

            if (serviceAttribute != null)
            {
                typeBuilder.AddCustomAttributeToType<Microsoft.AspNetCore.Mvc.RouteAttribute>(serviceAttribute.BaseRoute);

                if (serviceAttribute.Authorize)
                    typeBuilder.AddCustomAttributeToType<AuthorizeAttribute>();
            }

        }

        private static void CreateMethodAttributtes(MethodBuilder methodBuilder, MethodInfo method)
        {
            var action = method.GetCustomAttribute<ActionAttribute>();
            if (action != null)
            {
                Type httpMethod = action.Method switch
                {
                    RestMethod.Post => typeof(HttpPostAttribute),
                    RestMethod.Put => typeof(HttpPutAttribute),
                    RestMethod.Delete => typeof(HttpDeleteAttribute),
                    RestMethod.Get => typeof(HttpGetAttribute),
                    _ => typeof(HttpGetAttribute)
                };

                methodBuilder.AddCustomAttributeToMethod(httpMethod, action.Route);

                if (action.Authorize)
                    methodBuilder.AddCustomAttributeToMethod<AuthorizeAttribute>();


                foreach (var parameter in methodBuilder.GetParameters())
                {
                    var parameterBuilder = methodBuilder.DefineParameter(
                        parameter.Position + 1,
                        parameter.Attributes,
                        parameter.Name
                    );

                    CreateParameterAttributes(parameterBuilder, parameter, method);
                }
            }
        }

        private static void CreateParameterAttributes(ParameterBuilder parameterBuilder,
                                                      ParameterInfo parameter,
                                                      MethodInfo method)
        {
            var parameterInterface = method.GetParameters()
                                                   .FirstOrDefault(p => p.Name == parameter.Name &&
                                                           p.ParameterType == parameter.ParameterType);

            if (parameterInterface != null)
            {
                var body = parameterInterface.GetCustomAttribute<BodyAttribute>();
                if (body != null)
                    parameterBuilder.AddCustomAttributeToParameter<FromBodyAttribute>();

                var header = parameterInterface.GetCustomAttribute<HeaderAttribute>();
                if (header != null)
                    parameterBuilder.AddCustomAttributeToParameter<FromHeaderAttribute>(header.Name ?? parameterInterface.Name);


                var query = parameterInterface.GetCustomAttribute<QueryAttribute>();
                if (query != null)
                    parameterBuilder.AddCustomAttributeToParameter<FromQueryAttribute>(query.Name ?? parameterInterface.Name);
            }
        }

        #endregion

        private static void CreateConstructor(TypeBuilder typeBuilder, Type interfaceType, Type baseType)
        {
            ConstructorBuilder constructor = typeBuilder.DefineConstructor(MethodAttributes.Public,
                                                                           CallingConventions.Standard,
                                                                           new Type[] { interfaceType });

            ILGenerator ilConstructor = constructor.GetILGenerator();

            ilConstructor.Emit(OpCodes.Ldarg_0);
            ilConstructor.Emit(OpCodes.Ldarg_1);
            ilConstructor.Emit(OpCodes.Call, baseType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new Type[] { interfaceType }, null));
            ilConstructor.Emit(OpCodes.Nop); //TODO: remove??
            ilConstructor.Emit(OpCodes.Nop); //TODO: remove??
            ilConstructor.Emit(OpCodes.Ret);
        }

        private static void CreateInterfaceMethod(TypeBuilder typeBuilder, MethodInfo method, Type baseType)
        {
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(method.Name,
                                                                       MethodAttributes.Public | MethodAttributes.Virtual,
                                                                       method.ReturnType,
                                                                       method.GetParameters().Select(x => x.ParameterType)
                                                                       .ToArray());

            CreateMethodAttributtes(methodBuilder, method);

            ILGenerator il = methodBuilder.GetILGenerator();

            var listParamatersType = typeof(Dictionary<string, object>);

            il.Emit(OpCodes.Nop);
            var dictParameters = il.DeclareLocal(listParamatersType);
            il.Emit(OpCodes.Newobj, listParamatersType.GetConstructor(Type.EmptyTypes));
            il.Emit(OpCodes.Stloc, dictParameters);

            foreach (var parameter in method.GetParameters())
            {
                CreateMethodParametersAdd(parameter, il, dictParameters, listParamatersType);
            }

            if (method.ReturnType != typeof(void))
                CreateMethodReturnWithValue(method, il, baseType, dictParameters);
            else
                CreateMethodReturnWithoutValue(method, il, baseType, dictParameters);

            il.Emit(OpCodes.Ret);
            typeBuilder.DefineMethodOverride(methodBuilder, method);
        }

        private static void CreateMethodParametersAdd(ParameterInfo parameter,
                                                      ILGenerator il,
                                                      LocalBuilder dictParameters,
                                                      Type dictParametersType)
        {
            if (parameter.Position > 0)
                il.Emit(OpCodes.Nop); //TODO: remove??


            il.Emit(OpCodes.Ldloc, dictParameters);
            il.Emit(OpCodes.Ldstr, parameter.Name);
            il.Emit(OpCodes.Ldarg, parameter.Position + 1);

            if (parameter.ParameterType.IsValueType)
                il.Emit(OpCodes.Box, parameter.ParameterType);

            il.Emit(OpCodes.Callvirt, dictParametersType.GetMethod("Add"));
        }

        private static void CreateMethodReturnWithValue(MethodInfo method,
                                                        ILGenerator il,
                                                        Type baseType,
                                                        LocalBuilder dictParameters)
        {
            var executeRequestMethod = baseType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                                               .Single(x => x.Name == "ExecuteActionAsync"
                                                        && x.GetGenericArguments().Length == 1
                                                        && x.GetParameters().Length == 2);

            il.Emit(OpCodes.Nop); //TODO: remove??

            var returnType = method.ReturnType;

            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                returnType = returnType.GetGenericArguments()[0];

            var returnVar = il.DeclareLocal(returnType);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldstr, method.Name);
            il.Emit(OpCodes.Ldloc, dictParameters);
            il.Emit(OpCodes.Call, executeRequestMethod.MakeGenericMethod(returnType));
            il.Emit(OpCodes.Stloc, returnVar);

            //il.Emit(OpCodes.Br_S);
            il.Emit(OpCodes.Ldloc, returnVar);
        }

        private static void CreateMethodReturnWithoutValue(MethodInfo method,
                                                        ILGenerator il,
                                                        Type baseType,
                                                        LocalBuilder dictParameters)
        {
            var executeRequestMethod = baseType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                                               .Single(x => x.Name == "ExecuteActionAsync"
                                                        && x.GetGenericArguments().Length == 1
                                                        && x.GetParameters().Length == 2);

            il.Emit(OpCodes.Nop); //TODO: remove??

            var returnType = method.ReturnType;

            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                returnType = returnType.GetGenericArguments()[0];

            var returnVar = il.DeclareLocal(returnType);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldstr, method.Name);
            il.Emit(OpCodes.Ldloc, dictParameters);
            il.Emit(OpCodes.Call, executeRequestMethod.MakeGenericMethod(returnType));
            //il.Emit(OpCodes.Stloc, returnVar);

            //il.Emit(OpCodes.Br_S);
            il.Emit(OpCodes.Ldloc, returnVar);
        }
    }
}
