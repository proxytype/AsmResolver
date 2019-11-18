using System;
using System.Collections.Generic;
using AsmResolver.DotNet.Collections;
using AsmResolver.PE.DotNet.Metadata;
using AsmResolver.PE.DotNet.Metadata.Strings;
using AsmResolver.PE.DotNet.Metadata.Tables;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;

namespace AsmResolver.DotNet.Serialized
{
    /// <summary>
    /// Represents a lazily initialized implementation of <see cref="TypeDefinition"/>  that is read from a
    /// .NET metadata image. 
    /// </summary>
    public class SerializedTypeDefinition : TypeDefinition
    {
        private readonly IMetadata _metadata;
        private readonly SerializedModuleDefinition _parentModule;
        private readonly TypeDefinitionRow _row;

        /// <summary>
        /// Creates a type definition from a type metadata row.
        /// </summary>
        /// <param name="metadata">The object providing access to the underlying metadata streams.</param>
        /// <param name="parentModule"></param>
        /// <param name="token">The token to initialize the type for.</param>
        /// <param name="row">The metadata table row to base the type definition on.</param>
        public SerializedTypeDefinition(IMetadata metadata, SerializedModuleDefinition parentModule, MetadataToken token, TypeDefinitionRow row)
            : base(token)
        {
            _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            _parentModule = parentModule;
            _row = row;
            Attributes = row.Attributes;
        }

        /// <inheritdoc />
        protected override string GetNamespace() =>
            _metadata.GetStream<StringsStream>()?.GetStringByIndex(_row.Namespace);

        /// <inheritdoc />
        protected override string GetName() =>
            _metadata.GetStream<StringsStream>()?.GetStringByIndex(_row.Name);

        /// <inheritdoc />
        protected override ITypeDefOrRef GetBaseType()
        {
            if (_row.Extends == 0)
                return null;
            
            var decoder = _metadata
                .GetStream<TablesStream>()
                .GetIndexEncoder(CodedIndex.TypeDefOrRef);
            
            var token = decoder.DecodeIndex(_row.Extends);
            return (ITypeDefOrRef) _parentModule.LookupMember(token);
        }

        /// <inheritdoc />
        protected override IList<TypeDefinition> GetNestedTypes()
        {
            var result = new OwnedCollection<TypeDefinition, TypeDefinition>(this);
            
            var rids = _parentModule.GetNestedTypeRids(MetadataToken.Rid);
            foreach (uint rid in rids)
            {
                var nestedType = (TypeDefinition) _parentModule.LookupMember(new MetadataToken(TableIndex.TypeDef, rid));
                result.Add(nestedType);
            }

            return result;
        }

        /// <inheritdoc />
        protected override TypeDefinition GetDeclaringType()
        {
            uint parentTypeRid = _parentModule.GetParentTypeRid(MetadataToken.Rid);
            return parentTypeRid != 0
                ? (TypeDefinition) _parentModule.LookupMember(new MetadataToken(TableIndex.TypeDef, parentTypeRid))
                : null;
        }
        
    }
}