using System;

using NuClear.Storage.Core;

namespace NuClear.ValidationRules.Replication.Host.Settings
{
    public class DefaultEntityContainerNameResolver : IEntityContainerNameResolver
    {
        private const string Erm = "Erm";
        private const string Facts = "Facts";
        private const string Aggregates = "Aggregates";
        private const string Messages = "Messages";
        private const string Events = "Events";

        public string Resolve(Type objType)
        {
            if (objType.Namespace.Contains(Erm))
            {
                return Erm;
            }

            if (objType.Namespace.Contains(Facts))
            {
                return "ValidationRules";
            }

            if (objType.Namespace.Contains(Aggregates))
            {
                return "ValidationRules";
            }

            if (objType.Namespace.Contains(Messages))
            {
                return "ValidationRules";
            }

            if (objType.Namespace.Contains(Events))
            {
                return "ValidationRules";
            }

            throw new ArgumentException($"Unsupported type {objType.Name}: can not determine scope", nameof(objType));
        }
    }
}