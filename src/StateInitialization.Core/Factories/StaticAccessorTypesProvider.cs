using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using NuClear.Replication.Core.DataObjects;

namespace NuClear.StateInitialization.Core.Factories
{
    public sealed class StaticAccessorTypesProvider : IAccessorTypesProvider
    {
        private static readonly Lazy<IReadOnlyDictionary<Type, Type[]>> AccessorTypes = new Lazy<IReadOnlyDictionary<Type, Type[]>>(LoadAccessorTypes);

        private static IReadOnlyDictionary<Type, Type[]> LoadAccessorTypes()
            => AppDomain.CurrentDomain.GetAssemblies()
                        .Where(x => !x.IsDynamic)
                        .SelectMany(SafeGetAssemblyExportedTypes)
                        .SelectMany(type => type.GetInterfaces(), (type, @interface) => new { type, @interface })
                        .Where(x => !x.type.IsAbstract && IsAccessor(x.@interface))
                        .Select(x => new { GenericArgument = x.@interface.GetGenericArguments()[0], Type = x.type })
                        .GroupBy(x => x.GenericArgument, x => x.Type)
                        .ToDictionary(x => x.Key, x => x.ToArray());

        private static IEnumerable<Type> SafeGetAssemblyExportedTypes(Assembly assembly)
        {
            try
            {
                return assembly.ExportedTypes;
            }
            catch
            {
                return Array.Empty<Type>();
            }
        }

        private static bool IsAccessor(Type interfaceType)
            => interfaceType.IsGenericType &&
               (interfaceType.GetGenericTypeDefinition() == typeof(IStorageBasedDataObjectAccessor<>) ||
                interfaceType.GetGenericTypeDefinition() == typeof(IMemoryBasedDataObjectAccessor<>));

        public IReadOnlyCollection<Type> GetAccessorsFor(Type dataObjectType)
        {
            Type[] result;
            return AccessorTypes.Value.TryGetValue(dataObjectType, out result) ? result : Array.Empty<Type>();
        }
    }
}