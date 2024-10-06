using Microsoft.CodeAnalysis;
using MessageGenerator.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MessageGenerator
{
    internal class DocumentMessageCompilerAndGenerator_XcGen
    {
        private string _filesBaseLocation = @"D:\Swift Messaging\_ouput\ISO-20022";
        private readonly string _csFilesLocation = $@"cs-xcgen";
        private readonly string _xmlOutputFileLocation = $@"xml\{String.Format("{0:yyyy_MM_dd}", DateTime.Now)}_xcgen_";
        private readonly int _maxFilesToGenerate = 5;

        public DocumentMessageCompilerAndGenerator_XcGen() => new DocumentMessageCompilerAndGenerator(_filesBaseLocation);
        public DocumentMessageCompilerAndGenerator_XcGen(string filesBaseLocation)
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
                        !name.StartsWith($@"{_filesBaseLocation}\{_csFilesLocation}\OramaTech.Swift.Iso20022.ahV10", StringComparison.OrdinalIgnoreCase) &&
                        !name.StartsWith($@"{_filesBaseLocation}\{_csFilesLocation}\OramaTech.Swift.Iso20022.head", StringComparison.OrdinalIgnoreCase) &&
                        !name.StartsWith($@"{_filesBaseLocation}\{_csFilesLocation}\OramaTech.Swift.Iso20022.nvlp", StringComparison.OrdinalIgnoreCase)
                     ));

            //files = Directory.GetFiles($@"{_filesBaseLocation}\{_csFilesLocation}", "*Pain.v001_001_12.cs", SearchOption.AllDirectories).ToList();
            //files = Directory.GetFiles($@"{_filesBaseLocation}\{_csFilesLocation}", "*Tsmt.v003_001_03.cs", SearchOption.AllDirectories).ToList();

            var outputLocation = $@"{_filesBaseLocation}\{_xmlOutputFileLocation}";
            if (!System.IO.Directory.Exists(outputLocation))
            {
                System.IO.Directory.CreateDirectory(outputLocation);
            }
            else
            {
                //Console.WriteLine($"Deleting Existing (Re-RUN");
                var filesToDelete = Directory.GetFiles($@"{_filesBaseLocation}\{_xmlOutputFileLocation}")
                    .Where(name =>
                        !name.StartsWith($@"{_filesBaseLocation}\{_xmlOutputFileLocation}\$ahV10", StringComparison.OrdinalIgnoreCase) &&
                        !name.StartsWith($@"{_filesBaseLocation}\{_xmlOutputFileLocation}\OramaTech.Swift.Iso20022.ahV10", StringComparison.OrdinalIgnoreCase) &&
                        !name.StartsWith($@"{_filesBaseLocation}\{_xmlOutputFileLocation}\OramaTech.Swift.Iso20022.head", StringComparison.OrdinalIgnoreCase) &&
                        !name.StartsWith($@"{_filesBaseLocation}\{_xmlOutputFileLocation}\OramaTech.Swift.Iso20022.nvlp", StringComparison.OrdinalIgnoreCase)
                     );
                filesToDelete.ToList().ForEach(x => File.Delete(x));
                //Directory.EnumerateFiles($@"{_filesBaseLocation}\{_xmlOutputFileLocation}", "*.xml").ToList().ForEach(x => File.Delete(x));
            }

            Console.WriteLine($"DOCUMENT Total Files: {files.Count}");

            int idx = 0;

            foreach (string file in files.OrderBy(o=>o))
            {
                string fileContents = System.IO.File.ReadAllText(file);

                var compileResults = CompileHelper.CompileInMemoryFromSource(fileContents);

                if (compileResults.Item1 == false)
                {
                    IEnumerable<Diagnostic> failures = compileResults.Item3.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error ||
                        diagnostic.Severity == DiagnosticSeverity.Info);

                    foreach (Diagnostic diagnostic in failures)
                    {
                        Console.Error.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                    }

                    throw new Exception("Compile Error");
                }
                else
                {
                    Console.WriteLine($"[{++idx}] {file}");

                    Assembly assembly = compileResults.Item2;

                    var entryPoint = AssemblyHelper.GetDocumentTypes(assembly);

                    Console.WriteLine(string.Format("\t{0}", "Creating Instance"));
                    Console.WriteLine(string.Format("\t{0}", entryPoint.FullName));
                    string filename = entryPoint.FullName.Replace(".Document", "").ToString();
                    filename = string.Concat(filename, "-", Guid.NewGuid(), "_", String.Format("{0:yyyyMMdd_hhmmss_ttttt}", DateTime.Now));

                    var myObj = Activator.CreateInstance(entryPoint);

                    Console.WriteLine(string.Format("\t{0}", filename));

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

                        Console.WriteLine(string.Format("\t{0}", "OK"));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(string.Format("\t{0}", "***ERROR***"));
                        //throw ex;
                    }

                }
            }
        }
    }
}