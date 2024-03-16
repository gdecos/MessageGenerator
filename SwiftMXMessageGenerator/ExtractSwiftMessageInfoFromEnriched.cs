using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SWIFT_ISO2022MessageGenerator
{
    public static class ExtractSwiftMessageInfoFromEnriched
    {
        public static string fileOutputPath = @"D:\Swift Messaging\_ouput\ISO-15022";
        public static string filesPath = @"D:\Swift Messaging\ISO-15022\xsd";
        public static void DoXSDEnriched()
        {
            var path = @"D:\Swift Messaging\ISO-15022\xsd\SR_2016_MT101_enriched.xsd";
            path = @"D:\Swift Messaging\ISO-15022\xsd\SR_2016_MT103.STP_enriched.xsd";
            path = @"D:\Swift Messaging\ISO-15022\xsd\SR_2016_MT300_enriched.xsd";
            //path = @"D:\Swift Messaging\ISO-15022\xsd\SR_2016_MT565_enriched.xsd";

            List<string> files;

            files = Directory.GetFiles(filesPath + "", "*210*.xsd", SearchOption.AllDirectories).ToList();

            files = Directory.GetFiles(filesPath + "", "*202*COV*.xsd", SearchOption.AllDirectories).ToList();
            //files = Directory.GetFiles(filesPath + "", "*.xsd", SearchOption.AllDirectories).ToList();

            foreach (string file in files)
            {
                FileInfo fi = new FileInfo(file);

                var xsd = XDocument.Load(fi.FullName);

                var documentRoot = xsd.Root;
                var ns = documentRoot.GetDefaultNamespace();
                var NamespaceOfPrefix = documentRoot.GetNamespaceOfPrefix("xs");

                /*
                urn:swift:xsd:fin.101.2016
                urn:swift:xsd:fin.103.STP.2016
                urn:swift:xsd:fin.565.2016
                urn:iso:std:iso:20022:tech:xsd:pain.001.001.03
                */

                string[] swiftMessageInfo = ns.NamespaceName.Split('.');

                if (swiftMessageInfo[0].StartsWith("urn:swift:xsd:fin"))
                {
                    var mtType = swiftMessageInfo[1];
                    var mtTypeYear = int.Parse(swiftMessageInfo[swiftMessageInfo.Count() - 1]);

                    var document = documentRoot.Element(XName.Get("element", NamespaceOfPrefix.NamespaceName)); // NamespaceOfPrefix + "element");
                    var documentType = document.Attribute("type");
                    var documentComplexType = GetComplexType(documentType.Value, xsd);

                    var documentXsSequenceNode = documentComplexType.FirstNode;//xs:sequence
                    var elem = (documentXsSequenceNode as XElement).FirstNode; //xs:element

                    var messageElem = (elem as XElement);
                    var messageName = messageElem.Attribute("name");
                    var messageType = messageElem.Attribute("type");

                    var messageAnnotation = messageElem.FirstNode;
                    var messageFullName = (messageAnnotation as XElement).Descendants().Where(x => x.Attribute(XName.Get("source")).Value == "Name").First();
                    var messageDescription = (messageAnnotation as XElement).Descendants().Where(x => x.Attribute(XName.Get("source")).Value == "Definition").First();

                    MTTypeInfo mtTypeInfo = new MTTypeInfo();

                    mtTypeInfo.Year = mtTypeYear;
                    mtTypeInfo.Type = mtType;
                    mtTypeInfo.Name = messageFullName.Value;
                    mtTypeInfo.Description = messageDescription.Value;


                    GetMTTypeDetails(messageType.Value, xsd, mtTypeInfo);

                    System.Xml.Serialization.XmlSerializer ser = new System.Xml.Serialization.XmlSerializer(typeof(MTTypeInfo));

                    TextWriter writer = new StreamWriter($@"{fileOutputPath}\{fi.Name.Replace(fi.Extension ?? "", "")}-{mtTypeInfo.Year}-{mtTypeInfo.Type}.xml");
                    ser.Serialize(writer, mtTypeInfo);
                    writer.Close();
                }
                else if (swiftMessageInfo[0].StartsWith("urn:iso:std:iso:20022"))
                {
                    throw new Exception("Inavlid/Unhandled Message Type");
                    //urn:iso:std:iso:20022:tech:xsd:pain.001.001.03
                    //urn:iso:std:iso:20022:tech:xsd:pain.002.001.08
                    var components = ns.NamespaceName.Split(':');
                    var messageComponents = components[components.Length - 1].Split('.');

                    var fullMessageType = components[components.Length - 1];
                    var category = messageComponents[0];
                    var mtType = messageComponents[0] + '.' + messageComponents[1];
                    var version = messageComponents[2] + '.' + messageComponents[3];
                    var minversion = messageComponents[3];

                    /*
                    var document = documentRoot.Element(NamespaceOfPrefix + "element");
                    var documentType = document.Attribute("type");
                    var documentComplexType = GetComplexType(documentType.Value, xsd);

                    var documentXsSequenceNode = documentComplexType.FirstNode;//xs:sequence
                    var elem = (documentXsSequenceNode as XElement).FirstNode; //xs:element


                    var messageElem = (elem as XElement);
                    var messageName = messageElem.Attribute("name");
                    var messageType = messageElem.Attribute("type");

                    // **** for 20022 messages the annotation is in the type ******
                    var messageAnnotation = messageElem.FirstNode;
                    var messageFullName = (messageAnnotation as XElement).Descendants().Where(x => x.Attribute(XName.Get("source")).Value == "Name").First();
                    var messageDescription = (messageAnnotation as XElement).Descendants().Where(x => x.Attribute(XName.Get("source")).Value == "Definition").First();

                    ISO20022MTTypeInfo mtTypeInfo = new ISO20022MTTypeInfo();

                    mtTypeInfo.Version = version;
                    mtTypeInfo.Type = mtType;
                    mtTypeInfo.Name = messageFullName.Value;
                    mtTypeInfo.Description = messageDescription.Value;


                    GetMTTypeDetails(messageType.Value, xsd);
                    */
                }
                else if (swiftMessageInfo[0].StartsWith("urn:swift:xsd:$ahV10"))
                {
                    throw new Exception("Inavlid/Unhandled Message Type");
                    //header
                    //urn:swift:xsd:$ahV10
                }
                else
                {
                    throw new Exception("Inavlid/Unhandled Message Type");
                }
            }

        }

        public static void GetMTTypeDetails(string type, XDocument xsd, MTTypeInfo mtTypeInfo)
        {
            var namespaceName = xsd.Document.Root.GetNamespaceOfPrefix("xs").NamespaceName;
            var messageComplexType = GetComplexType(type, xsd);

            var complexContent = messageComplexType.FirstNode;
            var extensionBase = (complexContent as XElement).FirstNode;

            var extensionBaseValue = (extensionBase as XElement).Attribute(XName.Get("base")).Value;

            bool isMTMessage = extensionBaseValue == "MTMessage";
            bool isISO15022Message = extensionBaseValue == "ISO15022Message";
            bool isISO20022Message = isMTMessage == isISO15022Message;

            var nodesContainer = (extensionBase as XElement).FirstNode;

            mtTypeInfo.SwiftFields = new List<SwiftField>();
            mtTypeInfo.SwiftFieldsQueue = new Stack<SwiftField>();

            GetRootTags(nodesContainer, namespaceName, xsd, mtTypeInfo);
        }

        public static void GetRootTags(XNode nodesContainer, string namespaceName, XDocument xsd, MTTypeInfo mtTypeInfo)
        {
            var tags = (nodesContainer as XElement).Descendants((XName.Get("element", namespaceName))).ToList();
            foreach (var tag in tags)
            {
                SwiftField swiftField = new SwiftField();
                swiftField.SwiftFields = new List<SwiftField>();

                mtTypeInfo.SwiftFieldsQueue.Push(swiftField);

                var tagName = tag.Attribute(XName.Get("name")).Value;
                var tagType = tag.Attribute(XName.Get("type")).Value;

                var minOccurs = tag.Attribute(XName.Get("minOccurs"));
                var maxOccurs = tag.Attribute(XName.Get("maxOccurs"));

                swiftField.MinOccurs = minOccurs?.Value;
                swiftField.MaxOccurs = maxOccurs?.Value;

                var tagFullName = tag.Descendants().Where(x => x.HasAttributes && x.Attribute(XName.Get("source")).Value == "Name").FirstOrDefault();

                if (tagName.StartsWith("Seq")) //sequence
                {
                    var annotation = tag.FirstNode; //xs:annotation
                    //documentation - holds thee sequence name
                    var documentation = (annotation as XElement).Element(XName.Get("documentation", namespaceName));
                    var sequenceText = documentation.Value;

                    swiftField.Sequence = tagName;
                    swiftField.SequenceName = sequenceText;

                    GetMTSequenceInfo(tagType, xsd, mtTypeInfo);
                }
                else if (tagName.StartsWith("Loop")) //sequence
                {
                    //var annotation = tag.FirstNode; //xs:annotation
                    ////documentation - holds thee sequence name
                    //var documentation = (annotation as XElement).Element(XName.Get("documentation", namespaceName));
                    //var sequenceText = documentation.Value;

                    swiftField.Sequence = tagName;
                    swiftField.SequenceName = "Loop";

                    mtTypeInfo.SwiftFieldsQueue.Push(swiftField);

                    GetMTSequenceInfo(tagType, xsd, mtTypeInfo);

                    //var dequeuedField2 = mtTypeInfo.SwiftFieldsQueue.Pop();

                    //var parentField = mtTypeInfo.SwiftFieldsQueue.Peek();
                    //parentField.SwiftFields.Add(dequeuedField2);
                }
                else
                {
                    var tagDName = tag.Descendants().Where(x => x.HasAttributes && x.Attribute(XName.Get("source")).Value == "Name").FirstOrDefault();
                    var fieldText = tagDName.Value;
                    var tagDescription = tag.Descendants().Where(x => x.HasAttributes && x.Attribute(XName.Get("source")).Value == "Definition").FirstOrDefault();
                    var fieldDescription = tagDescription?.Value;

                    GetMTFieldInfo(tagType, xsd, mtTypeInfo, fieldText, fieldDescription);
                    //throw new Exception("No Sequence");
                }

                var dequeuedField = mtTypeInfo.SwiftFieldsQueue.Pop();
                mtTypeInfo.SwiftFields.Add(dequeuedField);
            }
        }

        public static void GetTags(XNode nodesContainer, string namespaceName, XDocument xsd, MTTypeInfo mtTypeInfo)
        {
            var tags = (nodesContainer as XElement).Descendants((XName.Get("element", namespaceName))).ToList();
            foreach (var tag in tags)
            {
                SwiftField swiftField = new SwiftField();
                swiftField.SwiftFields = new List<SwiftField>();

                var tagName = tag.Attribute(XName.Get("name")).Value;
                var tagType = tag.Attribute(XName.Get("type")).Value;

                var minOccurs = tag.Attribute(XName.Get("minOccurs"));
                var maxOccurs = tag.Attribute(XName.Get("maxOccurs"));

                swiftField.MinOccurs = minOccurs?.Value;
                swiftField.MaxOccurs = maxOccurs?.Value;



                var tagFullName = tag.Descendants().Where(x => x.HasAttributes && x.Attribute(XName.Get("source"))?.Value == "Name").FirstOrDefault();

                if (tagName.StartsWith("Seq")) //sequence
                {
                    var annotation = tag.FirstNode; //xs:annotation
                    //documentation - holds thee sequence name
                    var documentation = (annotation as XElement).Element(XName.Get("documentation", namespaceName));
                    var sequenceText = documentation.Value;

                    swiftField.Sequence = tagName;
                    swiftField.SequenceName = sequenceText;

                    mtTypeInfo.SwiftFieldsQueue.Push(swiftField);

                    GetMTSequenceInfo(tagType, xsd, mtTypeInfo);

                    var dequeuedField = mtTypeInfo.SwiftFieldsQueue.Pop();

                    var parentField = mtTypeInfo.SwiftFieldsQueue.Peek();
                    parentField.SwiftFields.Add(dequeuedField);
                }
                else if (tagName.StartsWith("Loop")) //sequence
                {
                    //var annotation = tag.FirstNode; //xs:annotation
                    ////documentation - holds thee sequence name
                    //var documentation = (annotation as XElement).Element(XName.Get("documentation", namespaceName));
                    //var sequenceText = documentation.Value;

                    swiftField.Sequence = tagName;
                    swiftField.SequenceName = "Loop";

                    mtTypeInfo.SwiftFieldsQueue.Push(swiftField);

                    GetMTSequenceInfo(tagType, xsd, mtTypeInfo);

                    var dequeuedField = mtTypeInfo.SwiftFieldsQueue.Pop();

                    var parentField = mtTypeInfo.SwiftFieldsQueue.Peek();
                    parentField.SwiftFields.Add(dequeuedField);
                }
                else
                {
                    //field description
                    var tagDName = tag.Descendants().Where(x => x.HasAttributes && x.Attribute(XName.Get("source")) != null && x.Attribute(XName.Get("source")).Value == "Name").FirstOrDefault();
                    if (tagDName != null)
                    {
                        var fieldText = tagDName.Value;
                        var tagDescription = tag.Descendants().Where(x => x.HasAttributes && x.Attribute(XName.Get("source")) != null && x.Attribute(XName.Get("source")).Value == "Definition").FirstOrDefault();
                        var fieldDescription = tagDescription?.Value;

                        if (tagName.StartsWith("F", StringComparison.CurrentCulture) && tagName.EndsWith("a", StringComparison.CurrentCulture))
                        {
                            swiftField.Sequence = tagName;
                            swiftField.SequenceName = "Choice";

                            mtTypeInfo.SwiftFieldsQueue.Push(swiftField);

                            GetMTFieldInfo(tagType, xsd, mtTypeInfo, fieldText, fieldDescription);

                            var dequeuedField = mtTypeInfo.SwiftFieldsQueue.Pop();

                            var parentField = mtTypeInfo.SwiftFieldsQueue.Peek();
                            parentField.SwiftFields.Add(dequeuedField);
                        }
                        else
                        {
                            GetMTFieldInfo(tagType, xsd, mtTypeInfo, fieldText, fieldDescription);
                        }
                    }
                    else
                    {
                        string todo = "wtf";
                    }
                }
            }
        }

        public static void GetMTSequenceInfo(string type, XDocument xsd, MTTypeInfo mtTypeInfo)
        {
            var messageComplexType = GetComplexType(type, xsd);
            var namespaceName = xsd.Document.Root.GetNamespaceOfPrefix("xs").NamespaceName;
            var namespaceInfoName = xsd.Document.Root.GetNamespaceOfPrefix("info").NamespaceName;
            var complexContent = messageComplexType.FirstNode;
            var extensionBase = (complexContent as XElement).FirstNode;
            var extensionBaseValue = (extensionBase as XElement).Attribute(XName.Get("base")).Value;
            GetTags(complexContent, namespaceName, xsd, mtTypeInfo);

        }

        public static void GetISO15022FieldInfo(string type, XDocument xsd, MTTypeInfo mtTypeInfo, SwiftField swiftField)
        {
            SwiftField swiftSequence = mtTypeInfo.SwiftFieldsQueue.Peek();

            var messageComplexType = GetComplexType(type, xsd);
            var namespaceName = xsd.Document.Root.GetNamespaceOfPrefix("xs").NamespaceName;
            var namespaceInfoName = xsd.Document.Root.GetNamespaceOfPrefix("info").NamespaceName;

            var complexContent = messageComplexType.FirstNode;
            var extensionBase = (complexContent as XElement).FirstNode;

            var extensionBaseValue = (extensionBase as XElement).Attribute(XName.Get("base")).Value;

            var choice = (extensionBase as XElement).Element(XName.Get("choice", namespaceName));
            var fields = (choice as XElement).Descendants(XName.Get("element", namespaceName)).ToList();

            foreach (var field in fields)
            {

                var tagName = field.Attribute(XName.Get("name")).Value;
                var tagType = field.Attribute(XName.Get("type")).Value;

                var annotation = field.FirstNode; //xs:annotation

                var tagElement = field.Descendants(XName.Get("Tag", namespaceInfoName)).ToList();
                var tag = tagElement.Attributes(XName.Get("value")).First();

                var minOccurs = field.Attribute(XName.Get("minOccurs"));
                var maxOccurs = field.Attribute(XName.Get("maxOccurs"));

                swiftField.MinOccurs = minOccurs?.Value;
                swiftField.MaxOccurs = maxOccurs?.Value;

                var tagText = tag.Value; // 16R for sequence

                var documentationName = (annotation as XElement).Descendants().Where(x => x.Attribute(XName.Get("source")) != null && x.Attribute(XName.Get("source")).Value == "Name").First();
                var documentationNameText = documentationName?.Value;
                //var documentationDefinition = (annotation as XElement).Descendants().Where(x => x.Attribute(XName.Get("source")).Value == "Definition").First();

                swiftField.Field += $"{tagText},";


                ///////////////////GetMTFieldInfo(tagType, xsd, mtTypeInfo);

            }

            swiftField.Field = swiftField.Field.TrimEnd(',');

        }

        public static void GetMTFieldInfo(string type, XDocument xsd, MTTypeInfo mtTypeInfo, string fieldText, string fieldDescription)
        {
            SwiftField swiftSequence = mtTypeInfo.SwiftFieldsQueue.Peek();

            var messageComplexType = GetComplexType(type, xsd);
            var namespaceName = xsd.Document.Root.GetNamespaceOfPrefix("xs").NamespaceName;
            var namespaceInfoName = xsd.Document.Root.GetNamespaceOfPrefix("info").NamespaceName;

            if (messageComplexType != null)
            {
                var complexContent = messageComplexType.FirstNode;
                var extensionBase = (complexContent as XElement).FirstNode;

                var extensionBaseValue = (extensionBase as XElement).Attribute(XName.Get("base")).Value;

                // these fields have value mapped to tag (certain values are for certain tag options)
                if (extensionBaseValue == "ISO15022Field")
                {
                    var choice = (extensionBase as XElement).Element(XName.Get("choice", namespaceName));
                    var fields = (choice as XElement).Descendants(XName.Get("element", namespaceName)).ToList();

                    foreach (var field in fields)
                    {
                        SwiftField swiftField = new SwiftField();
                        swiftField.SwiftFields = new List<SwiftField>();

                        var tagName = field.Attribute(XName.Get("name")).Value;
                        var tagType = field.Attribute(XName.Get("type")).Value;

                        var minOccurs = field.Attribute(XName.Get("minOccurs"));
                        var maxOccurs = field.Attribute(XName.Get("maxOccurs"));

                        swiftField.MinOccurs = minOccurs?.Value;
                        swiftField.MaxOccurs = maxOccurs?.Value;

                        var annotation = field.FirstNode; //xs:annotation

                        var documentationName = (annotation as XElement).Descendants().Where(x => x.Attribute(XName.Get("source")).Value == "Name").First();
                        var documentationNameText = documentationName?.Value;
                        var documentationDefinition = (annotation as XElement).Descendants().Where(x => x.Attribute(XName.Get("source")).Value == "Definition").First();
                        var documentationDefinitionText = documentationDefinition?.Value;


                        swiftField.Field = string.Empty;
                        swiftField.FieldName = tagName;
                        swiftField.FieldDescription = fieldText;
                        swiftSequence.SwiftFields.Add(swiftField);

                        GetISO15022FieldInfo(tagType, xsd, mtTypeInfo, swiftField);

                    }
                }
                else if (extensionBaseValue == "MTField")
                {
                    var choice = (extensionBase as XElement).Element(XName.Get("choice", namespaceName));
                    var fields = (choice as XElement).Descendants(XName.Get("element", namespaceName)).ToList();

                    foreach (var field in fields)
                    {
                        SwiftField swiftField = new SwiftField();
                        swiftField.SwiftFields = new List<SwiftField>();

                        var tagName = field.Attribute(XName.Get("name")).Value;
                        var tagType = field.Attribute(XName.Get("type")).Value;

                        var minOccurs = field.Attribute(XName.Get("minOccurs"));
                        var maxOccurs = field.Attribute(XName.Get("maxOccurs"));

                        swiftField.MinOccurs = minOccurs?.Value;
                        swiftField.MaxOccurs = maxOccurs?.Value;

                        //tagName F16R, tagType F16R_GENL_Type
                        if (tagName == "F16R" || tagName == "F16S")
                        {
                            var sequenceInfo = GetSimpleType(tagType, xsd);
                            var restriction = sequenceInfo.FirstNode;
                            var enumeration = (restriction as XElement).FirstNode;

                            var sequence = (enumeration as XElement).Attribute(XName.Get("value"));
                            var sequenceText = sequence.Value;


                            swiftSequence.SequenceValue = sequenceText;
                        }
                        else
                        {
                            var tagTypeInfo = GetSimpleType(tagType, xsd);

                            //F32B_Type is a complex type (MT 210)
                            if (tagTypeInfo == null)
                            {
                                tagTypeInfo = GetComplexType(tagType, xsd);
                            }

                            if (tagTypeInfo == null)
                                throw new Exception("Invalid Type");

                            var tagannotationInfo = (tagTypeInfo as XElement).Element(XName.Get("annotation", namespaceName));
                            var tagrestrictionInfo = (tagTypeInfo as XElement).Element(XName.Get("restriction", namespaceName));

                            if (tagannotationInfo != null)
                            {

                            }

                            //var restriction = tagTypeInfo.FirstNode;

                            if (tagrestrictionInfo != null)
                            {
                                var baseRestriction = (tagrestrictionInfo as XElement).Attribute(XName.Get("base"));

                                if (baseRestriction != null)
                                {
                                    var restrictionInfoType = GetSimpleType(baseRestriction.Value, xsd);
                                    var restrictionInfoTypeElement = (restrictionInfoType as XElement);

                                    var annotationInfo = (restrictionInfoType as XElement).Element(XName.Get("annotation", namespaceName));
                                    var restrictionInfo = (restrictionInfoType as XElement).Element(XName.Get("restriction", namespaceName));


                                    var annotationInfoAppInfo = (annotationInfo as XElement).FirstNode;
                                    var restrictionInfoAppInfo = (annotationInfo as XElement).FirstNode;

                                    var metaType = (annotationInfoAppInfo as XElement).Element(XName.Get("MetaType", xsd.Document.Root.GetNamespaceOfPrefix("info").NamespaceName)).Attribute(XName.Get("value"));
                                    var finFormat = (annotationInfoAppInfo as XElement).Element(XName.Get("FinFormat", xsd.Document.Root.GetNamespaceOfPrefix("info").NamespaceName)).Attribute(XName.Get("value"));

                                    var minLength = (restrictionInfo as XElement).Element(XName.Get("minLength", xsd.Document.Root.GetNamespaceOfPrefix("xs").NamespaceName))?.Attribute(XName.Get("value"));
                                    var maxLength = (restrictionInfo as XElement).Element(XName.Get("maxLength", xsd.Document.Root.GetNamespaceOfPrefix("xs").NamespaceName))?.Attribute(XName.Get("value"));
                                    var pattern = (restrictionInfo as XElement).Element(XName.Get("pattern", xsd.Document.Root.GetNamespaceOfPrefix("xs").NamespaceName))?.Attribute(XName.Get("value"));


                                    swiftField.MetaType = metaType?.Value;
                                    swiftField.FinFormat = finFormat?.Value;
                                    swiftField.MinLength = minLength?.Value; ;
                                    swiftField.MaxLength = maxLength?.Value;
                                    swiftField.Pattern = pattern?.Value;

                                    //var enumeration = (restriction as XElement).FirstNode;
                                }
                                else
                                {
                                    throw new Exception("baseRestriction is null");
                                }

                            }
                            else
                            {
                                var complexContentDefinition = (tagTypeInfo as XElement).Element(XName.Get("complexContent", namespaceName));
                                if (complexContentDefinition != null)
                                {
                                    var newSimpleTypeInfo = GetSimpleType(tagType, xsd);

                                    // todo: here we need to add fields (fields not tags)
                                    GetMTFieldInfo(tagType, xsd, mtTypeInfo, fieldText, fieldDescription);

                                }
                                else
                                {
                                    throw new Exception("complexContentDefinition is null");
                                }
                            }
                            //var zz = 1;
                        }

                        var tagElement = field.Descendants(XName.Get("Tag", namespaceInfoName)).ToList();
                        var tag = tagElement.Attributes(XName.Get("value")).First();
                        var tagText = tag.Value; // 16R for sequence
                        var tagDocumentation = field.Descendants(XName.Get("documentation", namespaceName)).ToList();

                        swiftField.Field = tagText;
                        swiftField.FieldName = fieldText;
                        swiftField.FieldDescription = fieldDescription;
                        swiftSequence.SwiftFields.Add(swiftField);
                    }
                }
                else if (extensionBaseValue == "FieldOption")
                {
                    var fields = (extensionBase as XElement).Descendants(XName.Get("element", namespaceName)).ToList();

                    //GetTags(complexContent, namespaceName, xsd, mtTypeInfo);



                    /*
                    foreach (var field in fields)
                    {
                        SwiftField swiftField = new SwiftField();
                        swiftField.SwiftFields = new List<SwiftField>();

                        var tagName = field.Attribute(XName.Get("name")).Value;
                        var tagType = field.Attribute(XName.Get("type")).Value;

                        var annotation = field.FirstNode; //xs:annotatio

                        var tagElement = field.Descendants(XName.Get("Tag", namespaceInfoName)).ToList();

                        if (tagElement != null && tagElement.Count > 0)
                        {
                            var tag = tagElement.Attributes(XName.Get("value")).First();

                            var tagText = tag.Value; // 16R for sequence

                            var documentationName = (annotation as XElement).Descendants().Where(x => x.Attribute(XName.Get("source")) != null && x.Attribute(XName.Get("source")).Value == "Name").First();
                            var documentationNameText = documentationName?.Value;
                            //var documentationDefinition = (annotation as XElement).Descendants().Where(x => x.Attribute(XName.Get("source")).Value == "Definition").First();

                            swiftField.Field = tagText;
                            swiftField.FieldName = fieldText;
                            swiftField.FieldDescription = fieldDescription;
                            swiftSequence.SwiftFields.Add(swiftField);
                        }
                        else
                        {
                            var newTagFiledName = (field as XElement).Descendants().Where(x => x.Attribute(XName.Get("source")) != null && x.Attribute(XName.Get("source")).Value == "Name").First();

                            if (newTagFiledName != null)
                            {
                                var tagText = newTagFiledName.Value;

                                var documentationName = (annotation as XElement).Descendants().Where(x => x.Attribute(XName.Get("source")) != null && x.Attribute(XName.Get("source")).Value == "Name").First();
                                var documentationNameText = documentationName?.Value;
                                //var documentationDefinition = (annotation as XElement).Descendants().Where(x => x.Attribute(XName.Get("source")).Value == "Definition").First();

                                swiftField.Field = tagText;
                                swiftField.FieldName = fieldText;
                                swiftField.FieldDescription = fieldDescription;
                                swiftSequence.SwiftFields.Add(swiftField);

                                var tagTypeInfo = GetSimpleType(tagType, xsd);

                                //currency type example                                                        

                            }
                        }                

                        ///////////////////GetMTFieldInfo(tagType, xsd, mtTypeInfo);
                    }
                    */


                }
                else if (extensionBaseValue == "ChoiceDataType")
                {
                    var fields = (extensionBase as XElement).Descendants(XName.Get("element", namespaceName)).ToList();
                }
                else if (extensionBaseValue == "ComplexDataType")
                {
                    var fields = (extensionBase as XElement).Descendants(XName.Get("element", namespaceName)).ToList();
                }
                else
                {
                    var choice = (extensionBase as XElement).Element(XName.Get("choice", namespaceName));
                    var fields = (choice as XElement).Descendants(XName.Get("element", namespaceName)).ToList();

                    foreach (var field in fields)
                    {
                        SwiftField swiftField = new SwiftField();
                        swiftField.SwiftFields = new List<SwiftField>();

                        var tagName = field.Attribute(XName.Get("name")).Value;
                        var tagType = field.Attribute(XName.Get("type")).Value;

                        var annotation = field.FirstNode; //xs:annotation

                        var tagElement = field.Descendants(XName.Get("Tag", namespaceInfoName)).ToList();
                        var tag = tagElement.Attributes(XName.Get("value")).First();


                        var tagText = tag.Value; // 16R for sequence

                        var documentationName = (annotation as XElement).Descendants().Where(x => x.Attribute(XName.Get("source")) != null && x.Attribute(XName.Get("source")).Value == "Name").First();
                        var documentationNameText = documentationName?.Value;
                        //var documentationDefinition = (annotation as XElement).Descendants().Where(x => x.Attribute(XName.Get("source")).Value == "Definition").First();

                        swiftField.Field = tagText;
                        swiftField.FieldName = fieldText;
                        swiftField.FieldDescription = fieldDescription;
                        swiftSequence.SwiftFields.Add(swiftField);

                        ///////////////////GetMTFieldInfo(tagType, xsd, mtTypeInfo);

                    }
                }
            }
            else
            {
                var tagTypeInfo = GetSimpleType(type, xsd);
                var simpleContent = tagTypeInfo.FirstNode;

                var tagannotationInfo = (tagTypeInfo as XElement).Element(XName.Get("annotation", namespaceName));
                var tagrestrictionInfo = (tagTypeInfo as XElement).Element(XName.Get("restriction", namespaceName));

                SwiftField swiftField = new SwiftField();
                //-//swiftField.Field = tagText;
                swiftField.FieldName = fieldText;
                swiftField.FieldDescription = fieldDescription;

                if (tagannotationInfo != null)
                {
                    var test = (tagannotationInfo as XElement);

                    var annotationInfo = (test as XElement).Element(XName.Get("appinfo", namespaceName));

                    var annotationInfoAppInfo = (annotationInfo as XElement).FirstNode;
                    var restrictionInfoAppInfo = (annotationInfo as XElement).FirstNode;

                    //var metaType = (annotationInfoAppInfo as XElement).Element(XName.Get("MetaType", xsd.Document.Root.GetNamespaceOfPrefix("info").NamespaceName));
                    var metaType = (annotationInfo as XElement).Element(XName.Get("MetaType", xsd.Document.Root.GetNamespaceOfPrefix("info").NamespaceName));

                    var metaTypeV = metaType.Attribute(XName.Get("value"));


                    var finFormat = (annotationInfo as XElement).Element(XName.Get("FinFormat", xsd.Document.Root.GetNamespaceOfPrefix("info").NamespaceName)).Attribute(XName.Get("value"));

                    swiftField.MetaType = metaTypeV?.Value;
                    swiftField.FinFormat = finFormat?.Value;
                }

                if (tagrestrictionInfo != null)
                {
                    var baseRestriction = (tagrestrictionInfo as XElement).Attribute(XName.Get("base"));

                    if (baseRestriction != null)
                    {
                        if (baseRestriction.Value == "xs:string")
                        {
                            var minLength = (tagrestrictionInfo as XElement).Element(XName.Get("minLength", xsd.Document.Root.GetNamespaceOfPrefix("xs").NamespaceName))?.Attribute(XName.Get("value"));
                            var maxLength = (tagrestrictionInfo as XElement).Element(XName.Get("maxLength", xsd.Document.Root.GetNamespaceOfPrefix("xs").NamespaceName))?.Attribute(XName.Get("value"));
                            var pattern = (tagrestrictionInfo as XElement).Element(XName.Get("pattern", xsd.Document.Root.GetNamespaceOfPrefix("xs").NamespaceName))?.Attribute(XName.Get("value"));
                            swiftField.MinLength = minLength?.Value;
                            swiftField.MaxLength = maxLength?.Value;
                            swiftField.Pattern = pattern?.Value;
                        }
                        else
                        {
                            var restrictionInfoType = GetSimpleType(baseRestriction.Value, xsd);
                            var restrictionInfoTypeElement = (restrictionInfoType as XElement);

                            var annotationInfo = (restrictionInfoType as XElement).Element(XName.Get("annotation", namespaceName));
                            var restrictionInfo = (restrictionInfoType as XElement).Element(XName.Get("restriction", namespaceName));


                            var annotationInfoAppInfo = (annotationInfo as XElement).FirstNode;
                            var restrictionInfoAppInfo = (annotationInfo as XElement).FirstNode;

                            var metaType = (annotationInfoAppInfo as XElement).Element(XName.Get("MetaType", xsd.Document.Root.GetNamespaceOfPrefix("info").NamespaceName)).Attribute(XName.Get("value"));
                            var finFormat = (annotationInfoAppInfo as XElement).Element(XName.Get("FinFormat", xsd.Document.Root.GetNamespaceOfPrefix("info").NamespaceName)).Attribute(XName.Get("value"));

                            var minLength = (restrictionInfo as XElement).Element(XName.Get("minLength", xsd.Document.Root.GetNamespaceOfPrefix("xs").NamespaceName))?.Attribute(XName.Get("value"));
                            var maxLength = (restrictionInfo as XElement).Element(XName.Get("maxLength", xsd.Document.Root.GetNamespaceOfPrefix("xs").NamespaceName))?.Attribute(XName.Get("value"));
                            var pattern = (restrictionInfo as XElement).Element(XName.Get("pattern", xsd.Document.Root.GetNamespaceOfPrefix("xs").NamespaceName))?.Attribute(XName.Get("value"));

                            if (metaType != null)
                                swiftField.MetaType = metaType?.Value;
                            if (finFormat != null)
                                swiftField.FinFormat = finFormat?.Value;

                            swiftField.MinLength = minLength?.Value;
                            swiftField.MaxLength = maxLength?.Value;
                            swiftField.Pattern = pattern?.Value;
                        }


                        //var enumeration = (restriction as XElement).FirstNode;
                    }
                    else
                    {
                        throw new Exception("baseRestriction is null");
                    }

                }
                else
                {
                    var complexContentDefinition = (tagTypeInfo as XElement).Element(XName.Get("complexContent", namespaceName));
                    if (complexContentDefinition != null)
                    {
                        throw new Exception("complexContentDefinition is null");

                    }
                    else
                    {
                        throw new Exception("complexContentDefinition is null");
                    }
                }

                swiftSequence.SwiftFields.Add(swiftField);

                //var zz = 1;


                //var tagElement = field.Descendants(XName.Get("Tag", namespaceInfoName)).ToList();
                //var tag = tagElement.Attributes(XName.Get("value")).First();
                //var tagText = tag.Value; // 16R for sequence
                //var tagDocumentation = field.Descendants(XName.Get("documentation", namespaceName)).ToList();

                //swiftField.Field = tagText;
                //swiftField.FieldName = fieldText;
                //swiftField.FieldDescription = fieldDescription;
                //swiftSequence.SwiftFields.Add(swiftField);







                //--------------------------------------------------------------------------------------------

                //--------------------------------------------------------------------------------------------

                //--------------------------------------------------------------------------------------------

                //--------------------------------------------------------------------------------------------
            }
        }

        private static void ValidationCallBack(object sender, ValidationEventArgs args)
        {
            return;
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

    public class MTTypeInfo
    {
        public int Year { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public List<SwiftField> SwiftFields { get; set; }

        [XmlIgnore]
        public Stack<SwiftField> SwiftFieldsQueue { get; set; }

        public MTTypeInfo() { }
    }

    public class SwiftField
    {
        public SwiftField() { }
        public string Sequence { get; set; }
        public string SequenceName { get; set; }
        public string SequenceValue { get; set; }
        public string SequenceDescription { get; set; }
        public string Field { get; set; }
        public string FieldName { get; set; }
        public string FieldDescription { get; set; }

        public string MinOccurs { get; set; }
        public string MaxOccurs { get; set; }

        public List<SwiftField> SwiftFields { get; set; }
        public string MetaType { get; set; }
        public string FinFormat { get; set; }
        public string MinLength { get; set; }
        public string MaxLength { get; set; }
        public string Pattern { get; set; }
    }

    public class ISO20022MTTypeInfo
    {
        public string Type { get; set; }

        public string Version { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }

        public ISO20022MTTypeInfo() { }
    }

}
