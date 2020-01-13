using NuClear.Model.Common;
using NuClear.Storage.API.ConnectionStrings;

namespace NuClear.ValidationRules.Hosting.Common.Identities.Connections
{
    public sealed class RulesetConnectionStringIdentity : IdentityBase<RulesetConnectionStringIdentity>, IConnectionStringIdentity
    {
        public override int Id => 20;

        public override string Description => nameof(RulesetConnectionStringIdentity);
    }
}