using AsmResolver.PE.DotNet.Metadata;

namespace AsmResolver.DotNet.Builder
{
    /// <summary>
    /// Represents a mutable buffer for a metadata stream. 
    /// </summary>
    public interface IMetadataStreamBuffer
    {
        /// <summary>
        /// Gets the name of the stream.
        /// </summary>
        string Name
        {
            get;
        }

        /// <summary>
        /// Serializes the buffer to a metadata stream.
        /// </summary>
        /// <returns>The stream.</returns>
        IMetadataStream CreateStream();
    }
}