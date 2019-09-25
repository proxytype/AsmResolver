// AsmResolver - Executable file format inspection library 
// Copyright (C) 2016-2019 Washi
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 3.0 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA

using System;
using System.Collections.Generic;
using System.Threading;
using AsmResolver.Lazy;
using AsmResolver.PE.File;
using AsmResolver.PE.Imports;
using AsmResolver.PE.Relocations;
using AsmResolver.PE.Win32Resources;

namespace AsmResolver.PE
{
    /// <summary>
    /// When derived from this class, represents an image of a portable executable (PE) file, exposing high level
    /// mutable structures. 
    /// </summary>
    public abstract class PEImageBase
    {
        /// <summary>
        /// Opens a PE image from a specific file on the disk.
        /// </summary>
        /// <param name="filePath">The </param>
        /// <returns>The PE image that was opened.</returns>
        /// <exception cref="BadImageFormatException">Occurs when the file does not follow the PE file format.</exception>
        public static PEImageBase FromFile(string filePath)
        {
            return FromPEFile(PEFile.FromFile(filePath));
        }
        
        /// <summary>
        /// Opens a PE image from a buffer.
        /// </summary>
        /// <param name="bytes">The bytes to interpret.</param>
        /// <returns>The PE iamge that was opened.</returns>
        /// <exception cref="BadImageFormatException">Occurs when the file does not follow the PE file format.</exception>
        public static PEImageBase FromBytes(byte[] bytes)
        {
            return FromPEFile(PEFile.FromBytes(bytes));
        }
        
        /// <summary>
        /// Opens a PE image from an input stream.
        /// </summary>
        /// <param name="reader">The input stream.</param>
        /// <returns>The PE image that was opened.</returns>
        /// <exception cref="BadImageFormatException">Occurs when the file does not follow the PE file format.</exception>
        public static PEImageBase FromReader(IBinaryStreamReader reader)
        {
            return FromPEFile(PEFile.FromReader(reader));
        }

        private static PEImageBase FromPEFile(PEFile peFile)
        {
            return new PEImageInternal(peFile);
        }

        private IList<ModuleImportEntryBase> _imports;
        private readonly LazyVariable<ResourceDirectoryBase> _resources;
        private IList<RelocationBlockBase> _relocations;

        protected PEImageBase()
        {
            _resources = new LazyVariable<ResourceDirectoryBase>(GetResources);
        }
        
        /// <summary>
        /// Gets a collection of modules that were imported into the PE, according to the import data directory.
        /// </summary>
        public IList<ModuleImportEntryBase> Imports
        {
            get
            {
                if (_imports is null) 
                    Interlocked.CompareExchange(ref _imports, GetImports(), null);
                return _imports;
            }
        }

        /// <summary>
        /// Gets or sets the root resource directory in the PE, if available.
        /// </summary>
        public ResourceDirectoryBase Resources
        {
            get => _resources.Value;
            set => _resources.Value = value;
        }

        public IList<RelocationBlockBase> Relocations
        {
            get
            {
                if (_relocations is null)
                    Interlocked.CompareExchange(ref _relocations, GetRelocations(), null);
                return _relocations;
            }
        }

        /// <summary>
        /// Obtains the list of modules that were imported into the PE.
        /// </summary>
        /// <returns>The imported modules.</returns>
        /// <remarks>
        /// This method is called upon initialization of the <see cref="Imports"/> property.
        /// </remarks>
        protected abstract IList<ModuleImportEntryBase> GetImports();

        /// <summary>
        /// Obtains the root resource directory in the PE.
        /// </summary>
        /// <returns>The root resource directory.</returns>
        /// <remarks>
        /// This method is called upon initialization of the <see cref="Resources"/> property.
        /// </remarks>
        protected abstract ResourceDirectoryBase GetResources();

        protected abstract IList<RelocationBlockBase> GetRelocations();
    }
}