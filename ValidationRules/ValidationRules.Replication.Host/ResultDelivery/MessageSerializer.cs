using Version = NuClear.ValidationRules.Storage.Model.Messages.Version;

namespace NuClear.ValidationRules.Replication.Host.ResultDelivery
{
    public sealed class MessageSerializer
    {
        public string Serialize(Version.ValidationResult result)
        {
            return result.MessageParams.ToString();
        }
    }
}