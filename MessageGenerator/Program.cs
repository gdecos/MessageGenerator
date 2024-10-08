﻿using Ardalis.GuardClauses;
using Microsoft.Extensions.Configuration;

namespace MessageGenerator
{
    class Program
    {
        private static IConfigurationRoot configurationRoot;
        private static string folderPath;
        private static void Configure()
        {
            configurationRoot = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();
        }

        static void Main(string[] args)
        {
            Configure();

            Ardalis.GuardClauses.Guard.Against.Null(configurationRoot);

            folderPath = configurationRoot.GetSection("FileLocationSettings").GetValue<string>("SwiftMXOutputLocation")!;

            /************************************************************************************************/
            /*
             * 
             XSLTC /settings:script+ /class:SwiftMTTransform MessageViewer.xslt

                XslCompiledTransform xslt = new XslCompiledTransform();
                xslt.Load(typeof(Transform));
                
                To dynamically link to the compiled assembly, replace
                xslt.Load(System.Reflection.Assembly.Load("Transform").GetType("Transform"));  

            */
            /************************************************************************************************/

            /************************************************************************************************/
            // Not Finished
            // generates xml info files based on enriched schemas (ISO 20022). 
            // these can be used for Tag Names and descriptiopns as well as to list tags by message type
            /* */
            //  ExtractIS20022SwiftMessageInfoFromEnriched.DoXSDEnriched();
            //return;
            /************************************************************************************************/


            /************************************************************************************************/
            // Not Finished
            // generates xml files based on enriched schemas (ISO 15022). 
            // these can be used for Tag Names and descriptiopns as well as to list tags by message type
            /* */
            //  ExtractSwiftMessageInfoFromEnriched.DoXSDEnriched();
            //return;
            /************************************************************************************************/


            /************************************************************************************************/
            // MX - compiles at runtime and generates assemblies
            /* NOTE : After this - RUN ProcessAsseblies BAT to create serializers*/
            var mxMessageAssemblyGenerator = new MXMessageAssemblyGenerator();
//            mxMessageAssemblyGenerator.Run();

            var mxMessageAssemblyGenerator_XcGen = new MXMessageAssemblyGenerator_XcGen();
//            mxMessageAssemblyGenerator_XcGen.Run();

            //return;
            /************************************************************************************************/

            /************************************************************************************************/
            // FIX - compiles at runtime and generates assemblies
            /* NOTE : After this - RUN ProcessAsseblies BAT to create serializers*/
            var fiMessageAssemblyGenerator = new FIXMessageAssemblyGenerator();
//            fiMessageAssemblyGenerator.Run();
            var fiMessageAssemblyGenerator_XcGen = new FIXMessageAssemblyGenerator_XcGen();
 //           fiMessageAssemblyGenerator_XcGen.Run();

            //return;
            /************************************************************************************************/

            // FIX M
            /************************************************************************************************/
            //compiles at runtime and generates full message (rnd)

            //!! finished - needs hardcoded root creation 
            var fixGenerator = new FIXMessageCompilerAndGenerator();
//            fixGenerator.Run();
            var fixGenerator_XcGen = new FIXMessageCompilerAndGenerator_XcGen();
//            fixGenerator_XcGen.Run();

            //return;
            /************************************************************************************************/

            /************************************************************************************************/
            //dynamically deserializes the messages created by the code CompileAllFiles

            var loadAndDeSerializeGeneratedXMLFiles = new LoadAndDeSerializeGeneratedXMLFiles();
//            loadAndDeSerializeGeneratedXMLFiles.Run();

            //return;
            /************************************************************************************************/

            // HEAD MESSAGE - BUSINESS APPLICATION HEADER 
            /************************************************************************************************/
            //compiles at runtime and generates full message (rnd)

            var headGenerator = new HEADMessageCompilerAndGenerator();
//            headGenerator.Run();
            var headGenerator_XcGen = new HEADMessageCompilerAndGenerator_XcGen();
//            headGenerator_XcGen.Run();

            //return;
            /************************************************************************************************/


            // DOCUMENT MESSAGE - MAIN MESSAGES 
            /************************************************************************************************/
            //compiles at runtime and generates full message (rnd)

            var documentGenerator = new DocumentMessageCompilerAndGenerator();
//            documentGenerator.Run();
            var documentGenerator_XcGen = new DocumentMessageCompilerAndGenerator_XcGen();
//            documentGenerator_XcGen.Run();

            //return;
            /************************************************************************************************/


            // BIZ MESSAGE - NVLP
            /************************************************************************************************/
            //compiles at runtime and generates full message (rnd)

            var nvlpGenerator = new NVLPMessageCompilerAndGenerator();
//            nvlpGenerator.Run();
            var nvlpGenerator_XcGen = new NVLPMessageCompilerAndGenerator_XcGen();
//            nvlpGenerator_XcGen.Run();

            //return;
            /************************************************************************************************/


            // (LIBS) DOCUMENT MESSAGE - MAIN MESSAGES 
            /************************************************************************************************/
            //processes the cs files in the libs folder by generating an xml file and saving it in the destination

            var localLibDocumentGenerator = new LocalLibraryMessageCompilerAndGenerator();
            //localLibDocumentGenerator.Run();

            /************************************************************************************************/
        }
    }
}