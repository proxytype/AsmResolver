using System.IO;
using AsmResolver.DotNet.Blob;
using AsmResolver.DotNet.TestCases.NestedClasses;
using Xunit;

namespace AsmResolver.DotNet.Tests
{
    public class AssemblyResolverTest
    {
        private readonly SignatureComparer _comparer = new SignatureComparer();

        [Fact]
        public void ResolveCorLib()
        {
            var assemblyName = typeof(object).Assembly.GetName();
            var assemblyRef = new AssemblyReference(
                assemblyName.Name,
                assemblyName.Version, 
                false,
                assemblyName.GetPublicKeyToken());
         
            var resolver = new DefaultAssemblyResolver();
            var assemblyDef = resolver.Resolve(assemblyRef);

            Assert.NotNull(assemblyDef);
            Assert.Equal(assemblyName.Name, assemblyDef.Name);
        }

        [Fact]
        public void ResolveLocalLibrary()
        {
            var resolver = new DefaultAssemblyResolver();
            resolver.SearchDirectories.Add(Path.GetDirectoryName(typeof(AssemblyResolverTest).Assembly.Location));
         
            var assemblyDef = AssemblyDefinition.FromFile(typeof(TopLevelClass1).Assembly.Location);
            var assemblyRef = new AssemblyReference(assemblyDef);

            Assert.Equal(assemblyDef, resolver.Resolve(assemblyRef), _comparer);
        }
    }
}