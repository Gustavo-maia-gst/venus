using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Venus.DependencyResolver
{
    public class Resolver : IResolver
    {
        private readonly Dictionary<Type, Func<object>?> _resolveMapping;
        private readonly Dictionary<Type, Type> _implementerMapping;

        public Resolver()
        {
            _resolveMapping = [];
            _implementerMapping = [];
        }

        public void RegisterDefault()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            foreach (Type type in assembly.GetTypes())
                TryRegister(type);

            Type[] types = _resolveMapping.Keys.ToArray();
            bool[] processed = new bool[types.Length];

            void ValidateCircularDependency(int iCurrent, int iStart)
            {
                if (_resolveMapping[types[iCurrent]] == null)
                    throw new CouldNotResolveException(types[iCurrent]);

                processed[iCurrent] = true;
                var dependencies = GetTypeDependencies(types[iCurrent]);
                if (dependencies == null)
                    return;

                foreach (var dependency in dependencies)
                {
                    if (dependency == types[iStart])
                        throw new CouldNotResolveException(types[iStart], "Circular dependency on type");

                    int iNext = 0;
                    try { while (types[iNext] != dependency) iNext++; }
                    catch (IndexOutOfRangeException) { throw new CouldNotResolveException(types[iCurrent], $"Could not resolve dependency {dependency}"); }
                    
                    ValidateCircularDependency(iNext, iStart);
                }
            }

            for (int i  = 0; i < types.Length; i++)
            {
                if (processed[i]) continue;
                ValidateCircularDependency(i, i);
            }
        }

        public object Resolve(Type type)
        {
            if (type.IsInterface)
            {
                if (!_implementerMapping.TryGetValue(type, out type) || type == null)
                    throw new CouldNotResolveException(type);
            }

            if (!_resolveMapping.TryGetValue(type, out Func<object>? factory) || factory == null)
                throw new CouldNotResolveException(type);

            return factory.Invoke();
        }

        private object GetInstance(Type type)
        {
            object?[]? objParams = null;

            Type[]? dependencies = GetTypeDependencies(type);
            if (dependencies != null)
                objParams = BuildParams(dependencies);

            try
            {
                object? obj = Activator.CreateInstance(type, objParams);
                return obj ?? throw new Exception();
            }
            catch (Exception ex)
            {
                throw new CouldNotResolveException(type, ex.Message);
            }
        }

        private Type[]? GetTypeDependencies(Type type)
        {
            ConstructorInfo[] constructors = type.GetConstructors();
            if (constructors.Length == 0)
                return null;

            if (constructors.Length > 1)
                throw new CouldNotResolveException(type, "Type contains more than one constructor");

            ConstructorInfo constructor = constructors.First();
            ParameterInfo[] parameters = constructor.GetParameters();

            return parameters
                .Where(param => !param.HasDefaultValue)
                .Select(param => param.ParameterType.IsInterface 
                                 ? _implementerMapping[param.ParameterType]
                                 : param.ParameterType)
                .ToArray();
        }

        private void TryRegister(Type type)
        {
            if (type.IsInterface)
                return;

            bool transient = type.GetInterface(typeof(ITransientDependency).FullName!) != null;
            bool singleton = type.GetInterface(typeof(ISingletonDependency).FullName!) != null;

            if (!transient && !singleton)
                return;
            if (transient && singleton)
                throw new InvalidOperationException($"The class {type} implements {transient} and {singleton}, but it can implements only one");

            if (transient)
                Register(type, RegisterTransient);
            if (singleton)
                Register(type, RegisterSingleton);
        }

        private void Register(Type concreteType, Action<Type, Type> registerAction)
        {
            Type[] interfaces = concreteType.GetInterfaces();

            registerAction(concreteType, concreteType);

            foreach (var type in interfaces)
            {
                if (!type.IsInterface || type == typeof(ITransientDependency) || type == typeof(ISingletonDependency))
                    continue;

                if (_resolveMapping.ContainsKey(type))
                    _resolveMapping[type] = null;

                registerAction(type, concreteType);
            }
        }

        private object?[] BuildParams(Type[] ctorParams)
        {
            object?[] objParams = new object[ctorParams.Length];

            for (int i = 0; i < ctorParams.Length; i++)
            {
                var param = ctorParams[i];
                objParams[i] = Resolve(param);
            }

            return objParams;
        }

        private void RegisterTransient(Type type, Type concreteType)
        {
            _resolveMapping.TryAdd(
                type, () => GetInstance(concreteType)
            );

            if (type.IsInterface)
                _implementerMapping.Add(type, concreteType);
        }

        private void RegisterSingleton(Type type, Type concreteType)
        {
            object instance = GetInstance(concreteType);
            _resolveMapping.TryAdd(
                type, () => instance
            );

            if (type.IsInterface)
                _implementerMapping.Add(type, concreteType);
        }
    }
}
