﻿using NuClear.Model.Common;
using NuClear.Storage.API.ConnectionStrings;

namespace NuClear.River.Hosting.Common.Identities.Connections
{
    public sealed class TransportConnectionStringIdentity : IdentityBase<TransportConnectionStringIdentity>, IConnectionStringIdentity
    {
        public override int Id => 3;

        public override string Description => "Operations transport DB connection string identity";
    }
}