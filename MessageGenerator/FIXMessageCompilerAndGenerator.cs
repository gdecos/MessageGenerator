﻿using Microsoft.CodeAnalysis;
using MessageGenerator.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MessageGenerator
{
    internal class FIXMessageCompilerAndGenerator
    {
        private string _filesBaseLocation = @"D:\Swift Messaging\_ouput\FIX";
        private readonly string _csFilesLocation = $@"cs";
        private readonly string _xmlOutputFileLocation = $@"xml\{String.Format("{0:yyyy_MM_dd}", DateTime.Now)}";
        private readonly int _maxFilesToGenerate = 5;

        public FIXMessageCompilerAndGenerator() => new FIXMessageCompilerAndGenerator(_filesBaseLocation);
        public FIXMessageCompilerAndGenerator(string filesBaseLocation)
        {
            _filesBaseLocation = filesBaseLocation;
        }

        public void Run()
        {
            List<string> files = Directory.GetFiles($@"{_filesBaseLocation}\{_csFilesLocation}", "*.cs", SearchOption.AllDirectories).ToList();

            var outputLocation = $@"{_filesBaseLocation}\{_xmlOutputFileLocation}";
            if (!System.IO.Directory.Exists(outputLocation))
            {
                System.IO.Directory.CreateDirectory(outputLocation);
            }
            else
            {
                Console.WriteLine($"Deleting Existing (Re-RUN");
                Directory.EnumerateFiles($@"{_filesBaseLocation}\{_xmlOutputFileLocation}", "*.xml").ToList().ForEach(x => File.Delete(x));
            }

            Console.WriteLine($"FIX Total Files: {files.Count}");

            int idx = 0;

            foreach (string file in files)
            {
                FileInfo fi = new FileInfo(file);
                string fileContents = System.IO.File.ReadAllText(file);

                List<string> additionalFileContents = new List<string>();
                List<string> additionalFiles = new List<string>();
                additionalFiles.AddRange(
                    Directory.GetFiles($@"{_filesBaseLocation}\{_csFilesLocation}", "*.cs", SearchOption.AllDirectories)
                        .Where(w =>
                            (w != file)
                        //(w.Contains("Components_Base_4_4_Fia_1_1"))
                        )
                    );
                foreach (var item in additionalFiles)
                {
                    var allText = System.IO.File.ReadAllText(item);
                    additionalFileContents.Add(allText);
                }

                var compileResults = CompileHelper.CompileInMemoryFromSource(fileContents, [..additionalFileContents]);

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

                    // gettign all using the xcgen approach
                    var allEntryPointTypes = AssemblyHelper.GetAllFIXDocumentTypesForXcGen(assembly);
                    Type entryPoint = null;
                    string genericEntryPoint = string.Empty;

                    var zzz1 = allEntryPointTypes.Where(w => w.FullName.Contains("Allocation")).ToList();
                    var zzz2 = allEntryPointTypes.Where(w => w.FullName.Contains("Instruction")).ToList();
                    var zzz3 = allEntryPointTypes.Where(w => w.Name.Contains("_message_t") && w.FullName.Contains("Ioi", StringComparison.InvariantCultureIgnoreCase)).ToList();

                    if (fi.Name == "Fixml-Allocation-Base-4-4-Fia-1-1.cs")
                    {
                        //"AllocationInstruction_message_t" 
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "AllocationInstruction_message_t").First();
                    }
                    else if (fi.Name == "Fixml-Collateral-Base-4-4-Fia-1-1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "CollateralRequest_message_t").First();
                    }
                    else if (fi.Name == "Fixml-Confirmation-Base-4-4-Fia-1-1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "Confirmation_message_t").First();
                    }
                    else if (fi.Name == "Fixml-Crossorders-Base-4-4-Fia-1-1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "NewOrderCross_message_t").First();
                    }
                    else if (fi.Name == "Fixml-Dds-Eod-Occ-1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.FullName == "OramaTech.Fix.Fixml.Dds_Eod_Occ_1.DDSEODMessage_message_t").First();
                    }
                    else if (fi.Name == "Fixml-Dds-Sod-Occ-1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.FullName == "OramaTech.Fix.Fixml.Dds_Sod_Occ_1.DDSSODMessage_message_t").First();
                    }
                    else if (fi.Name == "Fixml-Indications-Base-4-4-Fia-1-1.cs")
                    {
                        //OramaTech.Fix.Fixml.Occ.IOI_message_t
                        entryPoint = allEntryPointTypes.Where(w => w.FullName == "OramaTech.Fix.Fixml.Occ.IOI_message_t").First();
                    }
                    else if (fi.Name == "Fixml-Listorders-Base-4-4-Fia-1-1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "NewOrderList_message_t").First();
                    }
                    else if (fi.Name == "Fixml-Marginrequirements-Base-4-4-Fia-1-1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "MarginRequirementReport_message_t").First();
                    }
                    else if (fi.Name == "Fixml-Marketdata-Base-4-4-Fia-1-1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "MarketDataRequest_message_t").First();
                    }
                    else if (fi.Name == "Fixml-Multilegorders-Base-4-4-Fia-1-1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "NewOrderMultileg_message_t").First();
                    }
                    else if (fi.Name == "Fixml-Newsevents-Base-4-4-Fia-1-1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "News_message_t").First();
                    }
                    else if (fi.Name == "Fixml-Order-Base-4-4-Fia-1-1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "ExecutionReport_message_t").First();
                    }
                    else if (fi.Name == "Fixml-Positions-Base-4-4-Fia-1-1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "PositionMaintenanceRequest_message_t").First();
                    }
                    else if (fi.Name == "Fixml-Positions-Base-4-4-Fia-1-1-Orig.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "PositionMaintenanceRequest_message_t").First();
                    }
                    else if (fi.Name == "Fixml-Positions-Impl-4-4-Fia-1-1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "ContraryIntentionReport_message_t").First();
                    }
                    else if (fi.Name == "Fixml-Quotation-Base-4-4-Fia-1-1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "QuoteRequest_message_t").First();
                    }
                    else if (fi.Name == "Fixml-Registration-Base-4-4-Fia-1-1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "RegistrationInstructions_message_t").First();
                    }
                    else if (fi.Name == "Fixml-Securitystatus-Base-4-4-Fia-1-1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "SecurityDefinitionRequest_message_t").First();
                    }
                    else if (fi.Name == "Fixml-Securitystatus-Impl-4-4-Fia-1-1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "SecurityListUpdate_message_t").First();
                    }
                    else if (fi.Name == "Fixml-Settlement-Base-4-4-Fia-1-1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "SettlementInstructions_message_t").First();
                    }
                    else if (fi.Name == "Fixml-Tradecapture-Base-4-4-Fia-1-1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "TradeCaptureReportRequest_message_t").First();
                    }
                    else if (fi.Name == "Fixml-Components-Base-4-4-Fia-1-1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "FIXML").First();
                    }
                    else if (fi.Name == "Fixml-Components-Impl-4-4-Fia-1-1.cs")
                    {
                        genericEntryPoint = "Components";
                        //continue;
                    }
                    else if (fi.Name == "Fixml-Datatypes-4-4-Fia-1-1.cs")
                    {
                        genericEntryPoint = "Datatypes";
                        //continue;
                    }
                    else if (fi.Name == "Fixml-Fields-Base-4-4-Fia-1-1.cs")
                    {
                        //entryPoint = allEntryPointTypes.Where(w => w.Name == "HopT").First();

                        genericEntryPoint = "FieldsBase";
                        //continue;
                    }
                    else if (fi.Name == "Fixml-Fields-Impl-4-4-Fia-1-1.cs")
                    {
                        genericEntryPoint = "FieldsImpl";
                        //continue;
                    }
                    else
                    {

                    }


                    var entryPointType = string.Empty;

                    if (entryPoint != null)
                    {
                        entryPointType = entryPoint.Name;
                    }
                    else
                        entryPointType = genericEntryPoint;

                    if (entryPoint == null)
                        continue;

                    Console.WriteLine(string.Format("\t {0}", "Creating Instance"));
                    Console.WriteLine(string.Format("\t {0}", entryPointType));
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

                        System.Reflection.MethodInfo saveXMLMethod = typeof(FileHelpers).GetMethod("SaveXmlFile_FIX");
                        if (saveXMLMethod.IsGenericMethod)
                            saveXMLMethod = saveXMLMethod.MakeGenericMethod(objectType);
                        var invokeMethodSaveXmlFile = saveXMLMethod.Invoke(objectType, [documentObj, filename, outputLocation]);

                        Console.WriteLine(string.Format("\t {0}", "OK"));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(string.Format("\t {0}", "***ERROR***"));
                        Console.WriteLine(string.Format("\t {0}", ex.InnerException.Message));
                        Console.WriteLine(string.Format("\t {0}", ex.InnerException.InnerException.Message));
                        //throw ex;
                    }
                    //break;
                }
            }
        }
    }
}