using Microsoft.CSharp;
using System.CodeDom.Compiler;

namespace SwiftMXMessageGenerator.Helpers
{
    public class CompileHelper
    {
        public CompileHelper() { }

        internal static CompilerResults CompileInMemoryFromSource(string fileContents)
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

        internal static CompilerResults CompileToAssemblyFromSource(string fileContents, string mainClass, string outputAssemblyFile)
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
