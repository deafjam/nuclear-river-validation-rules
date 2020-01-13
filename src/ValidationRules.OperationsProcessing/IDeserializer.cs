using System.Collections.Generic;

namespace NuClear.ValidationRules.OperationsProcessing
{
    public interface IDeserializer<in TMessage, out TDto>
    {
        IEnumerable<TDto> Deserialize(IEnumerable<TMessage> message);
    }
}