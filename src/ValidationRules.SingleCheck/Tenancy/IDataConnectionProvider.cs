using LinqToDB.Data;

namespace NuClear.ValidationRules.SingleCheck.Tenancy
{
    public interface IDataConnectionProvider
    {
        DataConnection CreateConnection(string name);
    }

    public static class DataConnectionName
    {
        public const string Erm = "Erm";
        public const string ValidationRules = "ValidationRules";
    }
}
