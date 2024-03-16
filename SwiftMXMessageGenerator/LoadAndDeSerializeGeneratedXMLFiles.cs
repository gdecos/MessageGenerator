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
using System.Xml.Linq;

namespace SWIFT_ISO2022MessageGenerator
{
    internal class LoadAndDeSerializeGeneratedXMLFiles
    {
        private string _filesBaseLocation = @"D:\Swift Messaging\_ouput\ISO-20022";
        private readonly string _csFilesLocation = $@"cs";
        private readonly string _xmlFileLocation = $@"xml";
        private readonly int _maxFilesToGenerate = 5;

        public LoadAndDeSerializeGeneratedXMLFiles() => new LoadAndDeSerializeGeneratedXMLFiles(_filesBaseLocation);
        public LoadAndDeSerializeGeneratedXMLFiles(string filesBaseLocation) {
            _filesBaseLocation = filesBaseLocation;
        }

        public void Run()
        {
            List<string> files = Directory.GetFiles($@"{_filesBaseLocation}\{_xmlFileLocation}", "*.xml", SearchOption.AllDirectories).ToList();

            Console.WriteLine($"XML Total Files: {files.Count}");

            int idx = 0;

            foreach (string file in files)
            {
                Console.WriteLine($"[{++idx}] {file}");

                var document = XDocument.Load(file);

                var ns = document.Root.GetDefaultNamespace();
                var nsName = ns.NamespaceName;

                var tokens = nsName.Split(':');

                var mainMessage = tokens[tokens.Length - 1];
                var mainMessageCSFile = mainMessage.Replace('.', '_');

                var csFile = $@"{_filesBaseLocation}\{_csFilesLocation}\{mainMessageCSFile}.cs";
                string fileContents = System.IO.File.ReadAllText(csFile);

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

                Assembly assembly = compileResults.CompiledAssembly;

                var entryPoint = AssemblyHelper.GetAllDocumentTypes(assembly).First();

                Console.WriteLine(string.Format("\t {0}", "Creating Instance"));
                Console.WriteLine(string.Format("\t {0}", entryPoint.FullName));

                var myObj = Activator.CreateInstance(entryPoint);

                try
                {
                    Type objectType = myObj.GetType();

                    System.Reflection.MethodInfo methodGetDeserializedXMLDocument = typeof(FileHelpers).GetMethod("GetDeserializedXMLDocument");
                    if (methodGetDeserializedXMLDocument.IsGenericMethod)
                        methodGetDeserializedXMLDocument = methodGetDeserializedXMLDocument.MakeGenericMethod(myObj.GetType());
                    var ISODocument = methodGetDeserializedXMLDocument.Invoke(objectType, new object[] { myObj, file });

                    System.Reflection.MethodInfo methodGetXML = typeof(FileHelpers).GetMethod("GetXML");
                    if (methodGetXML.IsGenericMethod)
                        methodGetXML = methodGetXML.MakeGenericMethod(ISODocument.GetType());
                    var outXML = methodGetXML.Invoke(objectType, new object[] { ISODocument });


                    //Console.WriteLine(string.Format("{0}", outXML));
                    Console.WriteLine(string.Format("\t {0}", "OK"));

                    GC.Collect
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