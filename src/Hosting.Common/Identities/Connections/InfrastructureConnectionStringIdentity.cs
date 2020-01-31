﻿using NuClear.Model.Common;
using NuClear.Storage.API.ConnectionStrings;

namespace NuClear.River.Hosting.Common.Identities.Connections
{
    public sealed class InfrastructureConnectionStringIdentity : IdentityBase<InfrastructureConnectionStringIdentity>, IConnectionStringIdentity
    {
        public override int Id => 6;

        public override string Description => "Infrastructure DB connections string identity";
    }
}