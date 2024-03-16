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
    internal class LocalLibraryMessageCompilerAndGenerator
    {
        private string _filesBaseLocation = @"D:\Swift Messaging\_ouput\ISO-20022";
        private readonly string _xmlOutputFileLocation = $@"xml\{String.Format("{0:yyyy_MM_dd}", DateTime.Now)}";
        private readonly int _maxFilesToGenerate = 5;

        public LocalLibraryMessageCompilerAndGenerator() => new LocalLibraryMessageCompilerAndGenerator(_filesBaseLocation);
        public LocalLibraryMessageCompilerAndGenerator(string filesBaseLocation)
        {
            _filesBaseLocation = filesBaseLocation;
        }

        public void Run()
        {    
            var outputLocation = $@"{_filesBaseLocation}\{_xmlOutputFileLocation}";
            if (!System.IO.Directory.Exists(outputLocation))
            {
                System.IO.Directory.CreateDirectory(outputLocation);
            }

            var localLibs = AssemblyHelper.GetAllDocumentTypes(Assembly.GetExecutingAssembly())
                //.Where(w => w.)
                .OrderBy(o=>o.FullName).ToList();

            Console.WriteLine($"LOCAL LIBS Total Files: {localLibs.Count}");
            Console.WriteLine($"========================================================================================");
            localLibs.ToList().ForEach(t => Console.WriteLine(string.Format("[{0}] {1}", t.Name, t.AssemblyQualifiedName)));
            Console.WriteLine($"========================================================================================");

            int idx = 0;

            foreach (Type lib in localLibs)
            {
                Console.WriteLine(string.Format("[{1}] {0}", lib.FullName, ++idx));

                Console.WriteLine(string.Format("\t {0}", "Creating Instance"));
                Console.WriteLine(string.Format("\t {0}", lib.FullName));
                string filename = lib.FullName.Replace(".DOCUMENT", "").ToString();
                filename = string.Concat(filename, "-", Guid.NewGuid(), "_", String.Format("{0:yyyyMMdd_hhmmss_ttttt}", DateTime.Now));

                try
                {
                    var myObj = Activator.CreateInstance(Type.GetType(lib.FullName));
                    Type objectType = myObj.GetType();

                    System.Reflection.MethodInfo method = typeof(ReflectionHelper).GetMethod("GetDocument");
                    if (method.IsGenericMethod)
                        method = method.MakeGenericMethod(objectType);
                    var documentObj = method.Invoke(myObj, null);

                    System.Reflection.MethodInfo methodGetXML = typeof(FileHelpers).GetMethod("GetXML");
                    if (methodGetXML.IsGenericMethod)
                        methodGetXML = methodGetXML.MakeGenericMethod(documentObj.GetType());
                    var outXML = methodGetXML.Invoke(objectType, new object[] { documentObj });

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