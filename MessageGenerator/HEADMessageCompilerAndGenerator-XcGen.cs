﻿using MessageGenerator.Helpers;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MessageGenerator
{
    internal class HEADMessageCompilerAndGenerator_XcGen
    {
        private string _filesBaseLocation = @"D:\Swift Messaging\_ouput\ISO-20022";
        private readonly string _csFilesLocation = $@"cs";
        private readonly string _xmlOutputFileLocation = $@"xml\{String.Format("{0:yyyy_MM_dd}", DateTime.Now)}_xcgen_";
        private readonly int _maxFilesToGenerate = 5;

        public HEADMessageCompilerAndGenerator_XcGen() => new HEADMessageCompilerAndGenerator(_filesBaseLocation);
        public HEADMessageCompilerAndGenerator_XcGen(string filesBaseLocation)
        {
            _filesBaseLocation = filesBaseLocation;
        }

        public void Run()
        {
            List<string> files = Directory.GetFiles($@"{_filesBaseLocation}\{_csFilesLocation}", "*head*.cs", SearchOption.AllDirectories).ToList();
            files.AddRange(Directory.GetFiles($@"{_filesBaseLocation}\{_csFilesLocation}", "*ahv10*.cs", SearchOption.AllDirectories));

            //files = Directory.GetFiles($@"{_filesBaseLocation}\{_csFilesLocation}", "head_002_001_01.cs", SearchOption.AllDirectories).ToList();

            var outputLocation = $@"{_filesBaseLocation}\{_xmlOutputFileLocation}";
            if (!System.IO.Directory.Exists(outputLocation))
            {
                System.IO.Directory.CreateDirectory(outputLocation);
            }
            else
            {
                Console.WriteLine($"Deleting Existing (Re-RUN");
                Directory.EnumerateFiles($@"{_filesBaseLocation}\{_xmlOutputFileLocation}", "*head*.xml").ToList().ForEach(x => File.Delete(x));
                Directory.EnumerateFiles($@"{_filesBaseLocation}\{_xmlOutputFileLocation}", "*ahv10*.xml").ToList().ForEach(x => File.Delete(x));
            }

            Console.WriteLine($"HEAD Total Files: {files.Count}");

            int idx = 0;

            foreach (string file in files)
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

                    throw new Exception("Complie Error");
                }
                else
                {
                    Console.WriteLine($"[{++idx}] {file}");

                    Assembly assembly = compileResults.Item2;

                    var entryPoint = AssemblyHelper.GetBAHEADERTypes(assembly);

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
}