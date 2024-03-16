using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SwiftMXMessageGenerator
{
    public static class ExtractIS20022SwiftMessageInfoFromEnriched
    {
        public static string fileOutputPath = @"D:\Swift Messaging\_ouput\ISO-20022\xml";
        public static string filesPath = @"D:\Swift Messaging\ISO-20022\xsd";

        public static void DoXSDEnriched()
        {
            List<string> files;

            //files = Directory.GetFiles(filesPath + "", "*.xsd", SearchOption.AllDirectories).ToList();
            files = Directory.GetFiles(filesPath + "", "*pain.998.001.03*.xsd", SearchOption.AllDirectories).ToList();

            foreach (string file in files)
            {
                var xsdDocument = XDocument.Load(file);


                var documentRoot = xsdDocument.Root;
                var ns = documentRoot.GetDefaultNamespace();
                var NamespaceOfPrefix = documentRoot.GetNamespaceOfPrefix("xs");
                var targetNamespace = documentRoot.Attribute(XName.Get("targetNamespace")).Value;

                var document = documentRoot.Element(XName.Get("element", NamespaceOfPrefix.NamespaceName)); // NamespaceOfPrefix + "element");
                var documentType = document.Attribute("type");
                var documentComplexType = GetComplexType(documentType.Value, xsdDocument);

                var documentXsSequenceNode = documentComplexType.FirstNode;//xs:sequence
                var elem = (documentXsSequenceNode as XElement).FirstNode; //xs:element

                var documentXElement = (elem as XElement);
                var documentXElementName = documentXElement.Attribute("name");
                var documentXElementType = documentXElement.Attribute("type");

                var elements = (documentRoot as XElement).Descendants((XName.Get("element", NamespaceOfPrefix.NamespaceName))).ToList();
                var simpleTypes = (documentRoot as XElement).Descendants((XName.Get("simpleType", NamespaceOfPrefix.NamespaceName))).ToList();
                var complexTypes = (documentRoot as XElement).Descendants((XName.Get("complexType", NamespaceOfPrefix.NamespaceName))).ToList();



                GetISO20022MTTypeDetails(documentXElementType.Value, xsdDocument);
            }
        }

        public static void GetISO20022MTTypeDetails(string type, XDocument document)
        {
            var namespaceName = document.Document.Root.GetNamespaceOfPrefix("xs").NamespaceName;
            var messageComplexType = GetComplexType(type, document);

            var complexContent = messageComplexType.FirstNode; //xs:sequence



            GetRootTags(complexContent, namespaceName, document);
        }

        public static void GetRootTags(XNode nodesContainer, string namespaceName, XDocument document)
        {
            var tags = (nodesContainer as XElement).Descendants((XName.Get("element", namespaceName))).ToList();
            foreach (var tag in tags)
            {
                var tagName = tag.Attribute(XName.Get("name")).Value;
                var tagType = tag.Attribute(XName.Get("type")).Value;

                var minOccurs = tag.Attribute(XName.Get("minOccurs"));
                var maxOccurs = tag.Attribute(XName.Get("maxOccurs"));

            }
        }

        private static XElement GetComplexType(string typeName, XDocument xsdSchema)
        {
            XNamespace ns = "http://www.w3.org/2001/XMLSchema";
            XElement complexType = xsdSchema.Descendants(ns + "complexType")
                .Where(a => a.Attributes("name").FirstOrDefault() != null && a.Attribute("name").Value == typeName)
                .FirstOrDefault();

            return complexType;
        }

        private static XElement GetSimpleType(string typeName, XDocument xsdSchema)
        {
            XNamespace ns = "http://www.w3.org/2001/XMLSchema";
            XElement simpleType = xsdSchema.Descendants(XName.Get("simpleType", xsdSchema.Root.GetNamespaceOfPrefix("xs").NamespaceName))
                .Where(a => a.Attributes("name").FirstOrDefault() != null && a.Attribute("name").Value == typeName)
                .FirstOrDefault();

            return simpleType;
        }

    }
}