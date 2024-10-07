using Microsoft.CodeAnalysis;
using MessageGenerator.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MessageGenerator
{
    internal class FIXMessageAssemblyGenerator_XcGen
    {
        private string _filesBaseLocation = @"D:\Swift Messaging\_ouput\FIX";
        private readonly string _csFilesLocation = $@"cs-xcgen";
        private readonly int _maxFilesToGenerate = 5;

        public FIXMessageAssemblyGenerator_XcGen() => new FIXMessageAssemblyGenerator_XcGen(_filesBaseLocation);
        public FIXMessageAssemblyGenerator_XcGen(string filesBaseLocation)
        {
            _filesBaseLocation = filesBaseLocation;
        }

        public void Run()
        {
            List<string> files = new List<string>();

            files.AddRange(
                Directory.GetFiles($@"{_filesBaseLocation}\{_csFilesLocation}", "*.cs", SearchOption.AllDirectories));

            Console.WriteLine($"Total Files: {files.Count}");

            Console.WriteLine($"Deleting Dll's");
            Directory.EnumerateFiles($@"{_filesBaseLocation}\\{_csFilesLocation}\\Assemblies", "*.dll").ToList().ForEach(x => File.Delete(x));
            Console.WriteLine($"Deleting Types (JSON)");
            Directory.EnumerateFiles($@"{_filesBaseLocation}\\{_csFilesLocation}\\TypesInfoJson", "*.json").ToList().ForEach(x => File.Delete(x));

            int idx = 0;

            foreach (string file in files)
            {
                FileInfo fi = new FileInfo(file);
                string fileContents = System.IO.File.ReadAllText(file);
                string csFileContents = fileContents;

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

                var OutputAssemblyName = fi.Name.Replace(fi.Extension, string.Empty);
                var OutputAssemblyFile = $@"{_filesBaseLocation}\{_csFilesLocation}\Assemblies\{OutputAssemblyName}.dll";
                string fileLocationTypesInfoJSON = $@"{_filesBaseLocation}\{_csFilesLocation}\TypesInfoJson";

                var compileResults = CompileHelper.CompileInMemoryFromSource(fileContents, [.. additionalFileContents]);

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

                    var allEntryPointTypes = AssemblyHelper.GetAllFIXDocumentTypesForXcGen(assembly);

                    Type entryPoint = null;
                    string genericEntryPoint = string.Empty;

                    if (fi.Name == "OramaTech.Fix.Fixml.Allocation_Base_4_4_Fia_1_1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "AllocationInstructionMessageT").First();
                    }
                    else if (fi.Name == "OramaTech.Fix.Fixml.Collateral_Base_4_4_Fia_1_1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "CollateralRequestMessageT").First();
                    }
                    else if (fi.Name == "OramaTech.Fix.Fixml.Confirmation_Base_4_4_Fia_1_1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "ConfirmationMessageT").First();
                    }
                    else if (fi.Name == "OramaTech.Fix.Fixml.Crossorders_Base_4_4_Fia_1_1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "NewOrderCrossMessageT").First();
                    }
                    else if (fi.Name == "OramaTech.Fix.Fixml.Dds_Eod_Occ_1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "DdseodMessageMessageT").First();
                    }
                    else if (fi.Name == "OramaTech.Fix.Fixml.Dds_Sod_Occ_1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "DdssodMessageMessageT").First();
                    }
                    else if (fi.Name == "OramaTech.Fix.Fixml.Indications_Base_4_4_Fia_1_1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "IoiMessageT").First();
                    }
                    else if (fi.Name == "OramaTech.Fix.Fixml.Listorders_Base_4_4_Fia_1_1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "NewOrderListMessageT").First();
                    }
                    else if (fi.Name == "OramaTech.Fix.Fixml.Marginrequirements_Base_4_4_Fia_1_1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "MarginRequirementReportMessageT").First();
                    }
                    else if (fi.Name == "OramaTech.Fix.Fixml.Marketdata_Base_4_4_Fia_1_1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "MarketDataRequestMessageT").First();
                    }
                    else if (fi.Name == "OramaTech.Fix.Fixml.Multilegorders_Base_4_4_Fia_1_1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "NewOrderMultilegMessageT").First();
                    }
                    else if (fi.Name == "OramaTech.Fix.Fixml.Newsevents_Base_4_4_Fia_1_1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "NewsMessageT").First();
                    }
                    else if (fi.Name == "OramaTech.Fix.Fixml.Order_Base_4_4_Fia_1_1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "ExecutionReportMessageT").First();
                    }
                    else if (fi.Name == "OramaTech.Fix.Fixml.Positions_Base_4_4_Fia_1_1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "PositionMaintenanceRequestMessageT").First();
                    }
                    else if (fi.Name == "OramaTech.Fix.Fixml.Positions_Base_4_4_Fia_1_1_Orig.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "PositionMaintenanceRequestMessageT").First();
                    }
                    else if (fi.Name == "OramaTech.Fix.Fixml.Positions_Impl_4_4_Fia_1_1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "ContraryIntentionReportMessageT").First();
                    }
                    else if (fi.Name == "OramaTech.Fix.Fixml.Quotation_Base_4_4_Fia_1_1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "QuoteRequestMessageT").First();
                    }
                    else if (fi.Name == "OramaTech.Fix.Fixml.Registration_Base_4_4_Fia_1_1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "RegistrationInstructionsMessageT").First();
                    }
                    else if (fi.Name == "OramaTech.Fix.Fixml.Securitystatus_Base_4_4_Fia_1_1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "SecurityDefinitionRequestMessageT").First();
                    }
                    else if (fi.Name == "OramaTech.Fix.Fixml.Securitystatus_Impl_4_4_Fia_1_1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "SecurityListUpdateMessageT").First();
                    }
                    else if (fi.Name == "OramaTech.Fix.Fixml.Settlement_Base_4_4_Fia_1_1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "SettlementInstructionsMessageT").First();
                    }
                    else if (fi.Name == "OramaTech.Fix.Fixml.Tradecapture_Base_4_4_Fia_1_1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "TradeCaptureReportRequestMessageT").First();
                    }
                    else if (fi.Name == "OramaTech.Fix.Fixml.Components_Base_4_4_Fia_1_1.cs")
                    {
                        entryPoint = allEntryPointTypes.Where(w => w.Name == "Fixml").First();
                    }
                    else if (fi.Name == "OramaTech.Fix.Fixml.Components_Impl_4_4_Fia_1_1.cs")
                    {
                        genericEntryPoint = "Components";
                        //continue;
                    }
                    else if (fi.Name == "OramaTech.Fix.Fixml.Datatypes_4_4_Fia_1_1.cs")
                    {
                        genericEntryPoint = "Datatypes";
                        //continue;
                    }
                    else if (fi.Name == "OramaTech.Fix.Fixml.Fields_Base_4_4_Fia_1_1.cs")
                    {
                        //entryPoint = allEntryPointTypes.Where(w => w.Name == "HopT").First();

                        genericEntryPoint = "FieldsBase";
                        //continue;
                    }
                    else if (fi.Name == "OramaTech.Fix.Fixml.Fields_Impl_4_4_Fia_1_1.cs")
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

                    //Type entryPoint = allEntryPointTypes.First();

                    Console.WriteLine(string.Format("\t {0}", "Creating PHYSICAL ASSEMBLY FROM SOURCE"));
                    Console.WriteLine(string.Format("\t {0}", entryPointType));
                    Console.WriteLine(string.Format("\t {0}", OutputAssemblyFile));

                    var physicalAsseblyCompilerResults = CompileHelper.CompileToAssemblyFromSource(fileContents, [.. additionalFileContents], entryPointType, OutputAssemblyFile);

                    if (physicalAsseblyCompilerResults.Item1 == false)
                    {
                        throw new Exception("An error occured");
                    }

                    try
                    {
                        var allTypes = assembly.GetTypes().Select(s => new { Name = s.Name, FullName = s.FullName }).ToList();


                        string filenameJson = string.Empty;

                        if (entryPoint != null)
                            filenameJson = entryPoint.FullName.Replace(".DOCUMENT", "").ToString();
                        else
                            filenameJson = fi.Name.Replace(".cs",".") +  entryPointType;

                        System.Reflection.MethodInfo methodSaveJSON = typeof(FileHelpers).GetMethod("SaveJSONFile");
                        if (methodSaveJSON.IsGenericMethod)
                            methodSaveJSON = methodSaveJSON.MakeGenericMethod(allTypes.GetType());
                        var invokeMethodSaveJSONFile = methodSaveJSON.Invoke(allTypes, [allTypes, filenameJson, fileLocationTypesInfoJSON]);

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