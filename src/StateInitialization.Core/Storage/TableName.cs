using System;

namespace NuClear.StateInitialization.Core.Storage
{
    public sealed class TableName
    {
        private string _fullName;

        public TableName(string tableName, string schemaName = null)
        {
            Table = tableName;
            Schema = schemaName;
        }

        public string Table { get; }

        public string Schema { get; }

        public override string ToString()
        {
            return _fullName ?? (_fullName = string.IsNullOrEmpty(Schema)
                                                 ? $"[{Table}]"
                                                 : $"[{Schema}].[{Table}]");
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj.GetType() == GetType() &&
                string.Equals(ToString(), obj.ToString(), StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
