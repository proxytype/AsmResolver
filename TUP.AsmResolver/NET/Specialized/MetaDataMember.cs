﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TUP.AsmResolver.PE.Readers;

namespace TUP.AsmResolver.NET.Specialized
{
    public abstract class MetaDataMember : IDisposable , ICacheProvider
    {
        internal uint metadatatoken;
        internal MetaDataRow metadatarow;
        internal MetaDataTableType table;
        internal NETHeader netheader;

        public uint MetaDataToken
        {
            get { return metadatatoken; }
        }
        public MetaDataRow MetaDataRow
        {
            get { return metadatarow; }
            set { metadatarow = value; }
        }
        public NETHeader NETHeader
        {
            get { return netheader; }
        }
        public MetaDataTableType Table
        {
            get { return table; }
        }

        public object ProcessPartType(int partindex, object value)
        {
            return Convert.ChangeType(value, metadatarow.parts[partindex].GetType());
        }
        public bool HasImage
        {
            get { return netheader != null; }
        }

        public bool HasSavedMetaDataRow
        {
            get { return HasImage && metadatarow != null; }
        }

        public void ApplyChanges()
        {
            if (HasSavedMetaDataRow && metadatarow.offset != 0)
            {
                byte[] generatedBytes = metadatarow.GenerateBytes();
                netheader.ParentAssembly.peImage.Write((int)metadatarow.offset, generatedBytes);

            }
           // else
           //     throw new ArgumentException("Cannot apply changes to a member without a metadata row.");
        }

        public void Dispose()
        {
            metadatarow = null;
            ClearCache();
        }

        public abstract void ClearCache();
    
    
    }
}