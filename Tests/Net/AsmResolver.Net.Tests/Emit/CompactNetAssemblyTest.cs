﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AsmResolver.Net;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Emit;
using AsmResolver.Net.Metadata;
using AsmResolver.Net.Signatures;
using AsmResolver.X86;
using Xunit;

namespace AsmResolver.Tests.Net.Emit
{
    public class CompactNetAssemblyTest : IClassFixture<TemporaryDirectoryFixture>
    {
        private readonly TemporaryDirectoryFixture _context;
        
        private const string DummyAssemblyName = "SomeAssembly";
        private const string TypeNamespace = "SomeNamespace";
        private const string TypeName = "SomeType";
        private const string MainMethodName = "Main";

        public CompactNetAssemblyTest(TemporaryDirectoryFixture context)
        {
            _context = context;
        }
        
        private static WindowsAssembly CreateTempAssembly()
        {
            var assembly = NetAssemblyFactory.CreateAssembly(DummyAssemblyName, false);
            var image = assembly.NetDirectory.MetadataHeader.LockMetadata();
            var importer = new ReferenceImporter(image);

            var type = new TypeDefinition(
                TypeNamespace, 
                TypeName, 
                TypeAttributes.Public,
                importer.ImportType(typeof(object)));
            
            image.Assembly.Modules[0].TopLevelTypes.Add(type);

            var mainMethod = new MethodDefinition(MainMethodName, MethodAttributes.Public | MethodAttributes.Static,
                new MethodSignature(new[] {importer.ImportTypeSignature(typeof(string[]))},
                    image.TypeSystem.Void));
            type.Methods.Add(mainMethod);

            mainMethod.MethodBody = new CilMethodBody(mainMethod);
            image.ManagedEntrypoint = mainMethod;
            return assembly;
        }

        [Fact]
        public void PersistentManagedSmallMethod()
        {
            const string expectedOutput = "Hello, world!";
            
            var assembly = CreateTempAssembly();
            var image = assembly.NetDirectory.MetadataHeader.Image;
            var importer = new ReferenceImporter(image);
            var mainMethod = image.Assembly.Modules[0].TopLevelTypes.First(x => x.Name == TypeName).Methods.First(x => x.Name == MainMethodName);

            var instructions = mainMethod.CilMethodBody.Instructions;
            instructions.AddRange(new[]
            {
                CilInstruction.Create(CilOpCodes.Ldstr, expectedOutput),
                CilInstruction.Create(CilOpCodes.Call,
                    importer.ImportMethod(typeof(Console).GetMethod("WriteLine", new[] {typeof(string)}))),
                CilInstruction.Create(CilOpCodes.Ret)
            });

            assembly.NetDirectory.MetadataHeader.UnlockMetadata();
            _context.VerifyOutput(assembly, expectedOutput);
        }

        [Fact]
        public void PersistentManagedFatMethodVariables()
        {
            const string expectedOutput = "Hello, world!";
            
            var assembly = CreateTempAssembly();
            var image = assembly.NetDirectory.MetadataHeader.Image;
            var importer = new ReferenceImporter(image);
            var mainMethod = image.Assembly.Modules[0].TopLevelTypes.First(x => x.Name == TypeName).Methods.First(x => x.Name == MainMethodName);

            mainMethod.CilMethodBody.Signature = new StandAloneSignature(
                new LocalVariableSignature(new[] { image.TypeSystem.String }));
            
            var instructions = mainMethod.CilMethodBody.Instructions;
            instructions.AddRange(new[]
            {
                CilInstruction.Create(CilOpCodes.Ldstr, expectedOutput),
                CilInstruction.Create(CilOpCodes.Stloc_0),
                CilInstruction.Create(CilOpCodes.Ldloc_0),
                CilInstruction.Create(CilOpCodes.Call,
                    importer.ImportMethod(typeof(Console).GetMethod("WriteLine", new[] {typeof(string)}))),
                CilInstruction.Create(CilOpCodes.Ret)
            });
            
            assembly.NetDirectory.MetadataHeader.UnlockMetadata();
            
            _context.VerifyOutput(assembly, expectedOutput);
        }

        [Fact]
        public void PersistentManagedFatMethodExceptionHandlers()
        {
            const string expectedOutput = "Hello, world!";
            
            var assembly = CreateTempAssembly();
            var image = assembly.NetDirectory.MetadataHeader.Image;
            var importer = new ReferenceImporter(image);
            var mainMethod = image.Assembly.Modules[0].TopLevelTypes.First(x => x.Name == TypeName).Methods.First(x => x.Name == MainMethodName);

            var instructions = mainMethod.CilMethodBody.Instructions;
            var tryStart = CilInstruction.Create(CilOpCodes.Nop);
            var handlerStart = CilInstruction.Create(CilOpCodes.Nop);
            var handlerEnd = CilInstruction.Create(CilOpCodes.Nop);

            instructions.AddRange(new[]
            {
                tryStart,
                CilInstruction.Create(CilOpCodes.Ldstr, expectedOutput),
                CilInstruction.Create(CilOpCodes.Newobj, importer.ImportMethod(typeof(Exception).GetConstructor(new[] {typeof(string)}))),
                CilInstruction.Create(CilOpCodes.Throw),
                CilInstruction.Create(CilOpCodes.Leave, handlerEnd),

                handlerStart,
                CilInstruction.Create(CilOpCodes.Callvirt, importer.ImportMethod(typeof(Exception).GetMethod("get_Message"))),
                CilInstruction.Create(CilOpCodes.Call, importer.ImportMethod(typeof(Console).GetMethod("WriteLine", new[] {typeof(string)}))),
                CilInstruction.Create(CilOpCodes.Leave_S, handlerEnd),
                handlerEnd,

                CilInstruction.Create(CilOpCodes.Ret),
            });

            mainMethod.CilMethodBody.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Exception)
            {
                TryStart = tryStart,
                TryEnd = handlerStart,
                HandlerStart = handlerStart,
                HandlerEnd = handlerEnd,
                CatchType = importer.ImportType(typeof(Exception))
            });
            
            assembly.NetDirectory.MetadataHeader.UnlockMetadata();
            
            _context.VerifyOutput(assembly, expectedOutput);
        }

        [Fact]
        public void PersistentNativeMethod()
        {
            var assembly = CreateTempAssembly();
            assembly.NetDirectory.Flags &= ~ImageNetDirectoryFlags.IlOnly;
            var image = assembly.NetDirectory.MetadataHeader.Image;
            var importer = new ReferenceImporter(image);
            
            var nativeMethod = new MethodDefinition("MyNativeMethod",
                MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.PInvokeImpl,
                new MethodSignature(image.TypeSystem.Int32));

            nativeMethod.ImplAttributes = MethodImplAttributes.Native
                                          | MethodImplAttributes.Unmanaged
                                          | MethodImplAttributes.PreserveSig;

            var nativeBody = new X86MethodBody();

            nativeBody.Instructions.Add(new X86Instruction
            {
                Mnemonic = X86Mnemonic.Mov,
                OpCode = X86OpCodes.Mov_Eax_Imm1632,
                Operand1 = new X86Operand(X86Register.Eax),
                Operand2 = new X86Operand(1337),
            });
            nativeBody.Instructions.Add(new X86Instruction
            {
                Mnemonic = X86Mnemonic.Retn,
                OpCode = X86OpCodes.Retn,
            });

            nativeMethod.MethodBody = nativeBody;
            image.Assembly.Modules[0].TopLevelTypes[0].Methods.Add(nativeMethod);
            
            var mainMethod = image.Assembly.Modules[0].TopLevelTypes.First(x => x.Name == TypeName).Methods.First(x => x.Name == MainMethodName);

            var instructions = mainMethod.CilMethodBody.Instructions;
            instructions.AddRange(new[]
            {
                CilInstruction.Create(CilOpCodes.Ldstr, "The secret number is: {0}"),
                CilInstruction.Create(CilOpCodes.Call, nativeMethod),
                CilInstruction.Create(CilOpCodes.Box, importer.ImportType(typeof(int))),
                CilInstruction.Create(CilOpCodes.Call,
                    importer.ImportMethod(
                        typeof(Console).GetMethod("WriteLine", new[] {typeof(string), typeof(object)}))),
                CilInstruction.Create(CilOpCodes.Ret)
            });
            
            assembly.NetDirectory.MetadataHeader.UnlockMetadata();
            
            _context.VerifyOutput(assembly, "The secret number is: 1337");
        }

        [Fact]
        public void PersistentNativeResources()
        {
            var contents = new byte[] {0, 1, 2, 3, 4, 5, 6};
            
            var assembly = CreateTempAssembly();
            assembly.NetDirectory.MetadataHeader.UnlockMetadata();
            
            var rootDirectory = new ImageResourceDirectory();
            rootDirectory.Entries.Add(new ImageResourceDirectoryEntry
            {
                ResourceType = ImageResourceDirectoryType.VersionInfo,
                SubDirectory = new ImageResourceDirectory
                {
                    Entries =
                    {
                        new ImageResourceDirectoryEntry(1)
                        {
                            SubDirectory = new ImageResourceDirectory
                            {
                                Entries =
                                {
                                    new ImageResourceDirectoryEntry
                                    {
                                        DataEntry = new ImageResourceDataEntry(contents)
                                    }
                                }
                            }
                        }
                    }
                }
            });

            assembly.RootResourceDirectory = rootDirectory;
            
            using (var stream = new MemoryStream())
            {
                assembly.Write(new BinaryStreamWriter(stream), new CompactNetAssemblyBuilder(assembly));
                assembly = WindowsAssembly.FromBytes(stream.ToArray());
                
                Utilities.ValidateResourceDirectory(rootDirectory, assembly.RootResourceDirectory);
            }
        }

        [Fact]
        public void PersistentManagedResource()
        {
            var contents = new byte[] {0, 1, 2, 3, 4, 5, 6};
            
            var assembly = CreateTempAssembly();
            var image = assembly.NetDirectory.MetadataHeader.Image;

            image.Assembly.Resources.Add(new ManifestResource("SomeResource", ManifestResourceAttributes.Public,
                contents));
            
            assembly.NetDirectory.MetadataHeader.UnlockMetadata();

            using (var stream = new MemoryStream())
            {
                assembly.Write(new BinaryStreamWriter(stream), new CompactNetAssemblyBuilder(assembly));
                assembly = WindowsAssembly.FromBytes(stream.ToArray());
                image = assembly.NetDirectory.MetadataHeader.LockMetadata();
                Assert.Single(image.Assembly.Resources);
                Assert.Equal(contents, image.Assembly.Resources[0].Data);
            }
        }

        [Fact]
        public void PersistentStrongName()
        {
            var strongNameData = new byte[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9};
            var assembly = CreateTempAssembly();
            assembly.NetDirectory.MetadataHeader.UnlockMetadata();
            assembly.NetDirectory.StrongNameData = new DataSegment(strongNameData);
            
            using (var stream = new MemoryStream())
            {
                assembly.Write(new BinaryStreamWriter(stream), new CompactNetAssemblyBuilder(assembly));
                assembly = WindowsAssembly.FromBytes(stream.ToArray());
                Assert.NotNull(assembly.NetDirectory.StrongNameData);
                Assert.Equal(strongNameData, assembly.NetDirectory.StrongNameData.Data);
            }
        }

        [Fact]
        public void PersistentVTables()
        {
            var assembly = CreateTempAssembly();
            var header = assembly.NetDirectory.MetadataHeader;
            var importer = new ReferenceImporter(header.Image);

            var type = new TypeDefinition(null, "SomeType", TypeAttributes.Public, importer.ImportType(typeof(object)));

            for (int i = 0; i < 10; i++)
            {
                var method = new MethodDefinition("Method" + i, MethodAttributes.Public | MethodAttributes.Virtual,
                    new MethodSignature(header.Image.TypeSystem.Void));
                method.MethodBody = new CilMethodBody(method)
                {
                    Instructions = {CilInstruction.Create(CilOpCodes.Ret)}
                };
                type.Methods.Add(method);
            }

            header.Image.Assembly.Modules[0].TopLevelTypes.Add(type);

            var mapping = header.UnlockMetadata();

            var directory = new VTablesDirectory();
            var vTableHeader = new VTableHeader()
            {
                Attributes = VTableAttributes.Is32Bit,
            };

            foreach (var method in type.Methods)
                vTableHeader.Table.Add(header.GetStream<TableStream>().ResolveRow(mapping[method]));
            
            directory.VTableHeaders.Add(vTableHeader);
            
            assembly.NetDirectory.VTablesDirectory = directory;
            
            using (var stream = new MemoryStream())
            {
                assembly.Write(new BinaryStreamWriter(stream), new CompactNetAssemblyBuilder(assembly));
                
                assembly = WindowsAssembly.FromBytes(stream.ToArray());

                directory = assembly.NetDirectory.VTablesDirectory;
                Assert.NotNull(directory);
                Assert.Equal(1, directory.VTableHeaders.Count);
                Assert.Equal(type.Methods.Select(x => mapping[x]),
                    directory.VTableHeaders[0].Table.Select(x => x.MetadataToken));
            }
            
        }

        [Fact]
        public void PersistentExports()
        {
            var assembly = CreateTempAssembly();
            assembly.NetDirectory.MetadataHeader.UnlockMetadata();
            
            var exportDirectory = new ImageExportDirectory
            {
                Name = "somefile.dll",
                OrdinalBase = 2,
                Exports =
                {
                    new ImageSymbolExport(0x1234),
                    new ImageSymbolExport(0x5678, "MyNamedExport1"),
                    new ImageSymbolExport(0x9ABC),
                    new ImageSymbolExport(0xDEF0, "MyNamedExport2"),
                }
            };
            assembly.ExportDirectory = exportDirectory;

            using (var stream = new MemoryStream())
            {
                assembly.Write(new BinaryStreamWriter(stream), new CompactNetAssemblyBuilder(assembly));
                
                assembly = WindowsAssembly.FromBytes(stream.ToArray());

                Assert.NotNull(assembly.ExportDirectory);
                Assert.Equal(exportDirectory.Name, assembly.ExportDirectory.Name);
                Assert.Equal(exportDirectory.Exports.Count, assembly.ExportDirectory.Exports.Count);
                for (int i = 0; i < assembly.ExportDirectory.Exports.Count; i++)
                {
                    var original = exportDirectory.Exports[i];
                    var newExport = assembly.ExportDirectory.Exports[i];
                    Assert.Equal(original.Rva, newExport.Rva);
                    Assert.Equal(original.Name, newExport.Name);
                }
            }
        }
        
    }
}