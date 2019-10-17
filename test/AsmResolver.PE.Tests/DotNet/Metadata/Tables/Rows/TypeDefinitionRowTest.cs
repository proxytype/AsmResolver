using AsmResolver.PE.DotNet.Metadata.Tables;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using Xunit;

namespace AsmResolver.PE.Tests.DotNet.Metadata.Tables.Rows
{
    public class TypeDefinitionRowTest
    {
        [Fact]
        public void ReadRow_SmallString_Small_Extends_SmallField_SmallMethod()
        {
            var peImage = PEImage.FromBytes(Properties.Resources.HelloWorld);
            var tablesStream = peImage.DotNetDirectory.Metadata.GetStream<TablesStream>();

            var typeDefTable = tablesStream.GetTable<TypeDefinitionRow>();
            Assert.Equal(2, typeDefTable.Count);
            Assert.Equal(
                new TypeDefinitionRow(0, 0x0001, 0x0000, 0x0000, 0x0001, 0x0001),
                typeDefTable[0]);
            Assert.Equal(
                new TypeDefinitionRow((TypeAttributes) 0x00100001, 0x016F, 0x013, 0x0031, 0x0001, 0x0001),
                typeDefTable[typeDefTable.Count - 1]);
        }
    }
}