using Microsoft.AspNetCore.Authorization.Infrastructure;
using System.Reflection;

namespace Venus.DependencyResolver
{
    public class Resolver : IResolver
    {
        private readonly Dictionary<Type, Func<object>?> _resolveMapping = [];

        public void RegisterDefault()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            foreach (Type type in assembly.GetTypes())
                TryRegister(type);
        }

        public object Resolve(Type type)
        {
            if (!_resolveMapping.TryGetValue(type, out Func<object>? factory))
                return GetInstance(type);

            if (factory == null)
                throw new CouldNotResolveException(type);

            return factory.Invoke();
        }

        private object GetInstance(Type type)
        {
            object?[]? objParams = null;

            ConstructorInfo[] constructors = type.GetConstructors();
            if (constructors.Length == 1)
            {
                ConstructorInfo constructor = constructors.First();
                ParameterInfo[] ctorParams = constructor.GetParameters();

                objParams = BuildParams(ctorParams);
            }
            else if (constructors.Length > 1)
                throw new NotImplementedException($"The class {type.FullName} contains more than one constructor");

            try
            {
                object? obj = Activator.CreateInstance(type, objParams);
                return obj ?? throw new Exception();
            }
            catch (Exception)
            {
                throw new CouldNotResolveException(type);
            }
        }

        private void TryRegister(Type type)
        {
            if (type.IsInterface)
                return;

            string transientName = typeof(ITransientDependency).FullName!;
            string singletonName = typeof(ISingletonDependency).FullName!;
            bool transient = type.GetInterface(transientName) != null;
            bool singleton = type.GetInterface(singletonName) != null;

            if (!transient && !singleton)
                return;
            if (transient && singleton)
                throw new InvalidOperationException($"The class {type} implements {transient} and {singleton}, but it can implements only one");

            Type[] interfaces = type.GetInterfaces();
            void register(Type concreteType, Type[] types, Action<Type, Type> registerAction)
            {
                if (_resolveMapping.ContainsKey(concreteType))
                    _resolveMapping[concreteType] = null;
                else
                    registerAction(concreteType, concreteType);

                foreach (var type in types)
                {
                    if (!type.IsInterface || type == typeof(ITransientDependency) || type == typeof(ISingletonDependency))
                        continue;

                    if (_resolveMapping.ContainsKey(type))
                        _resolveMapping[type] = null;

                    registerAction(type, concreteType);
                }
            }

            if (transient)
                register(type, interfaces, RegisterTransient);
            if (singleton)
                register(type, interfaces, RegisterSingleton);
        }

        private object?[] BuildParams(ParameterInfo[] ctorParams)
        {
            object?[] objParams = new object[ctorParams.Length];

            for (int i = 0; i < ctorParams.Length; i++)
            {
                var param = ctorParams[i];
                var instance = Resolve(param.ParameterType);
                if (instance == null)
                    if (param.HasDefaultValue)
                        instance = param.DefaultValue;
                    else
                        throw new CouldNotResolveException(param.ParameterType);

                objParams[i] = instance;
            }

            return objParams;
        }

        private void RegisterTransient(Type type, Type? concreteType = null)
        {
            _resolveMapping.Add(
                type, () => GetInstance(concreteType ?? type)
            );
        }

        private void RegisterSingleton(Type type, Type? concreteType = null)
        {
            object instance = GetInstance(concreteType ?? type);
            _resolveMapping.Add(
                type, () => instance
            );
        }
    }
}
