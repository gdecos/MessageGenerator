﻿using SWIFT_ISO2022MessageGenerator.Helpers;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace SWIFT_ISO2022MessageGenerator
{
    internal class MessageAssemblyGenerator
    {
        private string _filesBaseLocation = @"D:\Swift Messaging\_ouput\ISO-20022";
        private readonly string _csFilesLocation = $@"cs";
        private readonly int _maxFilesToGenerate = 5;

        public MessageAssemblyGenerator() => new MessageAssemblyGenerator(_filesBaseLocation);
        public MessageAssemblyGenerator(string filesBaseLocation)
        {
            _filesBaseLocation = filesBaseLocation;
        }

        public void Run()
        {
            List<string> files = new List<string>();

            files.AddRange(
                Directory.GetFiles($@"{_filesBaseLocation}\{_csFilesLocation}", "*.cs", SearchOption.AllDirectories));           

            Console.WriteLine($"Total Files: {files.Count}");

            int idx = 0;

            foreach (string file in files)
            {
                FileInfo fi = new FileInfo(file);
                string fileContents = System.IO.File.ReadAllText(file);
                string csFileContents = fileContents;

                var OutputAssemblyName = fi.Name.Replace(fi.Extension, string.Empty).Replace(".", "");
                var OutputAssemblyFile = $@"{_filesBaseLocation}\{_csFilesLocation}\Assemblies\{OutputAssemblyName}.dll";
                string fileLocationTypesInfoJSON = $@"{_filesBaseLocation}\{_csFilesLocation}\TypesInfoJson";

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

                var entryPoint = AssemblyHelper.GetAllDocumentTypes(assembly).First();

                Console.WriteLine(string.Format("\t {0}", "Creating PHYSICAL ASSEMBLY FROM SOURCE"));
                Console.WriteLine(string.Format("\t {0}", entryPoint.FullName));
                Console.WriteLine(string.Format("\t {0}", OutputAssemblyFile));

                var physicalAsseblyCompilerResults = CompileHelper.CompileToAssemblyFromSource(fileContents, entryPoint.Name, OutputAssemblyFile);

                try
                {
                    var allTypes = physicalAsseblyCompilerResults.CompiledAssembly.GetTypes().Select(s => new { Name = s.Name, FullName = s.FullName }).ToList();
                    string filenameJson = entryPoint.FullName.Replace(".DOCUMENT", "").ToString();

                    System.Reflection.MethodInfo methodSaveJSON = typeof(FileHelpers).GetMethod("SaveJSONFile");
                    if (methodSaveJSON.IsGenericMethod)
                        methodSaveJSON = methodSaveJSON.MakeGenericMethod(allTypes.GetType());
                    var invokeMethodSaveJSONFile = methodSaveJSON.Invoke(allTypes, new object[] { allTypes, filenameJson, fileLocationTypesInfoJSON });

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