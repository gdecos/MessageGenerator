using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace MessageGenerator.Helpers
{
    public class CompileHelper
    {
        public CompileHelper() { }

        internal static Tuple<Boolean, Assembly, IEnumerable<Diagnostic>> CompileInMemoryFromSource(string fileContents)
        {
            Assembly assembly = null!;
            var runtimeDirectory = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
            string assemblyName = Path.GetRandomFileName();

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(fileContents);
            MetadataReference[] references =
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(@$"{runtimeDirectory}\System.Runtime.dll"),
                MetadataReference.CreateFromFile(@$"{runtimeDirectory}\System.ComponentModel.dll"),
                MetadataReference.CreateFromFile(@$"{runtimeDirectory}\System.ComponentModel.Primitives.dll"),
                MetadataReference.CreateFromFile(@$"{runtimeDirectory}\System.Xml.dll"),
                MetadataReference.CreateFromFile(@$"{runtimeDirectory}\System.Private.Xml.dll"),
                MetadataReference.CreateFromFile(@$"{runtimeDirectory}\System.Xml.XmlSerializer.dll"),
                MetadataReference.CreateFromFile(@$"{runtimeDirectory}\System.Xml.Serialization.dll"),
            ];

            //foreach (var r in ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)!.Split(Path.PathSeparator))
            //{
            //    references = references.Append(MetadataReference.CreateFromFile(r)).ToArray();
            //}

            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                                .WithOptimizationLevel(OptimizationLevel.Release));

            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);

                if (!result.Success)
                {
                    return new Tuple<bool, Assembly, IEnumerable<Diagnostic>>(result.Success, assembly, result.Diagnostics);
                }
                else
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    assembly = Assembly.Load(ms.ToArray());

                    return new Tuple<bool, Assembly, IEnumerable<Diagnostic>>(result.Success, assembly, result.Diagnostics);
                }
            }
        }

        internal static Tuple<Boolean, IEnumerable<Diagnostic>> CompileToAssemblyFromSource(string fileContents, string mainClass, string outputAssemblyFile)
        {
            var runtimeDirectory = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
            string assemblyName = mainClass;

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(fileContents);
            MetadataReference[] references =
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(@$"{runtimeDirectory}\System.Runtime.dll"),
                MetadataReference.CreateFromFile(@$"{runtimeDirectory}\System.ComponentModel.dll"),
                MetadataReference.CreateFromFile(@$"{runtimeDirectory}\System.ComponentModel.Primitives.dll"),
                MetadataReference.CreateFromFile(@$"{runtimeDirectory}\System.Xml.dll"),
                MetadataReference.CreateFromFile(@$"{runtimeDirectory}\System.Private.Xml.dll"),
                MetadataReference.CreateFromFile(@$"{runtimeDirectory}\System.Xml.XmlSerializer.dll"),
                MetadataReference.CreateFromFile(@$"{runtimeDirectory}\System.Xml.Serialization.dll"),
            ];

            //foreach (var r in ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)!.Split(Path.PathSeparator))
            //{
            //    references = references.Append(MetadataReference.CreateFromFile(r)).ToArray();
            //}

            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary) { }
                                .WithOptimizationLevel(OptimizationLevel.Release));

            EmitResult result = compilation.Emit(outputAssemblyFile);

            return new Tuple<bool, IEnumerable<Diagnostic>>(result.Success, result.Diagnostics);
        }

        [Obsolete("Net 4.8 ONLY", true)]
        internal static CompilerResults CompileInMemoryFromSource_Net48(string fileContents)
        {
            CSharpCodeProvider provider = new CSharpCodeProvider();
            CompilerParameters parameters = new CompilerParameters();

            parameters.ReferencedAssemblies.Add("System.dll");
            parameters.ReferencedAssemblies.Add("System.Xml.dll");
            parameters.ReferencedAssemblies.Add("System.Xml.Serialization.dll");

            // True - memory generation, false - external file generation
            parameters.GenerateInMemory = true;
            // True - exe file generation, false - dll file generation
            parameters.GenerateExecutable = false;

            parameters.CompilerOptions = "/optimize";

            var compileResults = provider.CompileAssemblyFromSource(parameters, fileContents);

            provider.Dispose();

            return compileResults;
        }

        [Obsolete("Net 4.8 ONLY", true)]
        internal static CompilerResults CompileToAssemblyFromSource_Net48(string fileContents, string mainClass, string outputAssemblyFile)
        {
            CSharpCodeProvider provider = new CSharpCodeProvider();
            CompilerParameters parameters = new CompilerParameters();

            parameters.ReferencedAssemblies.Add("System.dll");
            parameters.ReferencedAssemblies.Add("System.Xml.dll");
            parameters.ReferencedAssemblies.Add("System.Xml.Serialization.dll");

            parameters.GenerateInMemory = false;
            parameters.IncludeDebugInformation = true;
            parameters.MainClass = mainClass;
            parameters.OutputAssembly = outputAssemblyFile;

            parameters.CompilerOptions = "/optimize";

            var compileResults = provider.CompileAssemblyFromSource(parameters, fileContents);

            provider.Dispose();

            return compileResults;
        }
    }
}
