using Microsoft.CodeAnalysis;
using MessageGenerator.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MessageGenerator
{
    internal class FIXMessageCompilerAndGenerator_XcGen
    {
        private string _filesBaseLocation = @"D:\Swift Messaging\_ouput\FIX";
        private readonly string _csFilesLocation = $@"cs-xcgen";
        private readonly string _xmlOutputFileLocation = $@"xml\{String.Format("{0:yyyy_MM_dd}", DateTime.Now)}_xcgen_";
        private readonly int _maxFilesToGenerate = 5;

        public FIXMessageCompilerAndGenerator_XcGen() => new FIXMessageCompilerAndGenerator_XcGen(_filesBaseLocation);
        public FIXMessageCompilerAndGenerator_XcGen(string filesBaseLocation)
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

                    throw new Exception("Compile Error");
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