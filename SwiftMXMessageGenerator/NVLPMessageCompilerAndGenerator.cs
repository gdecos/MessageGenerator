using Microsoft.CSharp;
using SWIFT_ISO2022MessageGenerator.Helpers;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SWIFT_ISO2022MessageGenerator
{
    internal class NVLPMessageCompilerAndGenerator
    {
        private string _filesBaseLocation = @"D:\Swift Messaging\_ouput\ISO-20022";
        private readonly string _csFilesLocation = $@"cs";
        private readonly string _xmlOutputFileLocation = $@"xml\{String.Format("{0:yyyy_MM_dd}", DateTime.Now)}";
        private readonly int _maxFilesToGenerate = 5;

        public NVLPMessageCompilerAndGenerator() => new NVLPMessageCompilerAndGenerator(_filesBaseLocation);
        public NVLPMessageCompilerAndGenerator(string filesBaseLocation) {
            _filesBaseLocation = filesBaseLocation;
        }

        public void Run()
        {
            List<string> files = Directory.GetFiles($@"{_filesBaseLocation}\{_csFilesLocation}", "*nvlp*.cs", SearchOption.AllDirectories).ToList();

            var outputLocation = $@"{_filesBaseLocation}\{_xmlOutputFileLocation}";
            if (!System.IO.Directory.Exists(outputLocation))
            {
                System.IO.Directory.CreateDirectory(outputLocation);
            }

            Console.WriteLine($"NVLP Total Files: {files.Count}");

            int idx = 0;

            foreach (string file in files)
            {
                string fileContents = System.IO.File.ReadAllText(file);

                var compileResults = CompileHelper.CompileInMemoryFromSource(fileContents);

                foreach (CompilerError ce in compileResults.Errors)
                {
                    if (ce.IsWarning) continue;
                    Console.WriteLine("{5}\t{0} ({1},{2}) [{3}] {4}", ce.FileName, ce.Line, ce.Column, ce.ErrorNumber, ce.ErrorText, ce.IsWarning ? "WARN" : "ERROR");
                }

                if (compileResults.Errors.Count > 0)
                {
                    throw new Exception("Complie Error");
                }

                Console.WriteLine($"[{++idx}] {file}");

                Assembly assembly = compileResults.CompiledAssembly;

                var entryPoint = AssemblyHelper.GetNVLPTypes(assembly);

                Console.WriteLine(string.Format("\t {0}", "Creating Instance"));
                Console.WriteLine(string.Format("\t {0}", entryPoint.FullName));
                string filename = entryPoint.FullName.Replace(".DOCUMENT", "").ToString();
                filename = string.Concat(filename, "-", Guid.NewGuid(), "_", String.Format("{0:yyyyMMdd_hhmmss_ttttt}", DateTime.Now));

                var myObj = Activator.CreateInstance(entryPoint);

                Console.WriteLine(string.Format("\t {0}", filename));

                try
                {
                    Type objectType = myObj.GetType();

                    System.Reflection.MethodInfo method = typeof(ReflectionHelper).GetMethod("GetDocument");
                    if (method.IsGenericMethod)
                        method = method.MakeGenericMethod(objectType);
                    var documentObj = method.Invoke(myObj, null);

                    System.Reflection.MethodInfo saveXMLMethod = typeof(FileHelpers).GetMethod("SaveXmlFile");
                    if (saveXMLMethod.IsGenericMethod)
                        saveXMLMethod = saveXMLMethod.MakeGenericMethod(objectType);
                    var invokeMethodSaveXmlFile = saveXMLMethod.Invoke(objectType, new object[] { documentObj, filename, outputLocation });

                    Console.WriteLine(string.Format("\t {0}", "OK"));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("\t {0}", "***ERROR***"));
                    throw ex;
                }
            }
        }
    }
}