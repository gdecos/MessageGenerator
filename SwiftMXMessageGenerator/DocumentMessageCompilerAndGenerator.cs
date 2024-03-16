using SWIFT_ISO2022MessageGenerator.Helpers;
using SwiftMXMessageGenerator.Helpers;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace SwiftMXMessageGenerator
{
    internal class DocumentMessageCompilerAndGenerator
    {
        private string _filesBaseLocation = @"D:\Swift Messaging\_ouput\ISO-20022";
        private readonly string _csFilesLocation = $@"cs";
        private readonly string _xmlOutputFileLocation = $@"xml\{String.Format("{0:yyyy_MM_dd}", DateTime.Now)}";
        private readonly int _maxFilesToGenerate = 5;

        public DocumentMessageCompilerAndGenerator() => new DocumentMessageCompilerAndGenerator(_filesBaseLocation);
        public DocumentMessageCompilerAndGenerator(string filesBaseLocation)
        {
            _filesBaseLocation = filesBaseLocation;
        }

        public void Run()
        {
            List<string> files = new List<string>();

            files.AddRange(
                Directory.GetFiles($@"{_filesBaseLocation}\{_csFilesLocation}")
                    .Where(name => 
                        !name.StartsWith($@"{_filesBaseLocation}\{_csFilesLocation}\$ahV10", StringComparison.OrdinalIgnoreCase) &&
                        !name.StartsWith($@"{_filesBaseLocation}\{_csFilesLocation}\ahV10", StringComparison.OrdinalIgnoreCase) &&
                        !name.StartsWith($@"{_filesBaseLocation}\{_csFilesLocation}\head", StringComparison.OrdinalIgnoreCase) &&
                        !name.StartsWith($@"{_filesBaseLocation}\{_csFilesLocation}\nvlp", StringComparison.OrdinalIgnoreCase)
                     ));           

            var outputLocation = $@"{_filesBaseLocation}\{_xmlOutputFileLocation}";
            if (!System.IO.Directory.Exists(outputLocation))
            {
                System.IO.Directory.CreateDirectory(outputLocation);
            }

            Console.WriteLine($"HEAD Total Files: {files.Count}");

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

                var entryPoint = AssemblyHelper.GetDocumentTypes(assembly);

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