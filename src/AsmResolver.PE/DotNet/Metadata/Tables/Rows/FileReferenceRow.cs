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

namespace AsmResolver.PE.DotNet.Metadata.Tables.Rows
{
    /// <summary>
    /// Represents a single row in the file metadata table.
    /// </summary>
    public readonly struct FileReferenceRow : IMetadataRow
    {
        /// <summary>
        /// Reads a single file row from an input stream.
        /// </summary>
        /// <param name="reader">The input stream.</param>
        /// <param name="layout">The layout of the file table.</param>
        /// <returns>The row.</returns>
        public static FileReferenceRow FromReader(IBinaryStreamReader reader, TableLayout layout)
        {
            return new FileReferenceRow(
                (FileAttributes) reader.ReadUInt32(),
                reader.ReadIndex((IndexSize) layout.Columns[1].Size),
                reader.ReadIndex((IndexSize) layout.Columns[2].Size));
        }

        public FileReferenceRow(FileAttributes attributes, uint name, uint hashValue)
        {
            Attributes = attributes;
            Name = name;
            HashValue = hashValue;
        }

        /// <inheritdoc />
        public TableIndex TableIndex => TableIndex.File;

        /// <summary>
        /// Gets the attributes associated to the file reference.
        /// </summary>
        public FileAttributes Attributes
        {
            get;
        }

        /// <summary>
        /// Gets an index into the #Strings stream referencing the name of the file.
        /// </summary>
        public uint Name
        {
            get;
        }

        /// <summary>
        /// Gets an index into the #Blob stream referencing the hash value of the file.
        /// </summary>
        public uint HashValue
        {
            get;
        }

        /// <summary>
        /// Determines whether this row is considered equal to the provided file row.
        /// </summary>
        /// <param name="other">The other row.</param>
        /// <returns><c>true</c> if the rows are equal, <c>false</c> otherwise.</returns>
        public bool Equals(FileReferenceRow other)
        {
            return Attributes == other.Attributes && Name == other.Name && HashValue == other.HashValue;
        }

        public override bool Equals(object obj)
        {
            return obj is FileReferenceRow other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int) Attributes;
                hashCode = (hashCode * 397) ^ (int) Name;
                hashCode = (hashCode * 397) ^ (int) HashValue;
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"({(int) Attributes:X8}, {Name:X8}, {HashValue:X8})";
        }
        
    }
}