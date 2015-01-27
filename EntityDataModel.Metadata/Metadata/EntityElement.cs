﻿using System.Collections.Generic;
using System.Linq;

using NuClear.AdvancedSearch.EntityDataModel.Metadata.Features;
using NuClear.Metamodeling.Elements;
using NuClear.Metamodeling.Elements.Aspects.Features;
using NuClear.Metamodeling.Elements.Identities;

namespace NuClear.AdvancedSearch.EntityDataModel.Metadata
{
    public sealed class EntityElement : BaseMetadataElement<EntityElement, EntityElementBuilder>
    {
        internal EntityElement(IMetadataElementIdentity identity, IEnumerable<IMetadataFeature> features)
            : base(identity, features)
        {
        }

        public IEnumerable<EntityPropertyElement> Properties
        {
            get
            {
                return Elements.OfType<EntityPropertyElement>();
            }
        }

        public IEnumerable<EntityRelationElement> Relations
        {
            get
            {
                return Elements.OfType<EntityRelationElement>();
            }
        }

        public IEnumerable<EntityPropertyElement> KeyProperties
        {
            get
            {
                return ResolveFeature<EntityIdentityFeature, IEnumerable<EntityPropertyElement>>(f => f.IdentifyingProperties, Enumerable.Empty<EntityPropertyElement>());
            }
        }

        public string EntitySetName
        {
            get
            {
                return ResolveFeature<EntitySetFeature, string>(f => f.EntitySetName);
            }
        }

        public EntityElement MappedEntity
        {
            get
            {
                return ResolveFeature<ElementMappingFeature, EntityElement>(f => (EntityElement)f.MappedElement);
            }
        }
    }
}