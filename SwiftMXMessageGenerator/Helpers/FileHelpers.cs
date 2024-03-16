using Newtonsoft.Json;
using System.IO;
using System.Xml.Serialization;

namespace SWIFT_ISO2022MessageGenerator.Helpers
{
    public static class FileHelpers
    {
        public static string GetXML<T>(T t)
        {
            System.Xml.Serialization.XmlSerializer xmlSerializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
            using (System.IO.StringWriter stringWriter = new System.IO.StringWriter())
            {
                xmlSerializer.Serialize(stringWriter, t);
                return stringWriter.ToString();
            }
        }

        public static T LoadFromString<T>(string xmlString)
        {
            T instance;
            using (TextReader reader = new StringReader(xmlString))
            {
                var xmlSerializer = new XmlSerializer(typeof(T));
                instance = (T)xmlSerializer.Deserialize(reader);
            }
            return instance;
        }
        public static T GetDeserializedXMLDocumentFromString<T>(string xmlString)
        {
            T instance = LoadFromString<T>(xmlString);
            return instance;
        }

        public static T GetDeserializedXMLDocument<T>(T t, string file)
        {
            System.Xml.Serialization.XmlSerializer xmlSerializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
            using (var filecontents = System.IO.File.OpenRead(file))
            {
                var deserializedDocument = xmlSerializer.Deserialize(filecontents);
                return (T) deserializedDocument;
            }
        }

        public static void SaveXmlFile<T>(T t, string fileName, string filePath)
        {
            var outputLocation = $@"{filePath}\{fileName}.xml";
            System.Xml.Serialization.XmlSerializer xmlSerializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
            using (System.IO.FileStream file = System.IO.File.Create(outputLocation))
            {
                xmlSerializer.Serialize(file, t);
            }
        }

        public static void SaveJSONFile<T>(T t, string fileName, string filePath)
        {
            var outputLocation = $@"{filePath}\{fileName}.json";

            JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                NullValueHandling = NullValueHandling.Include
            };

            var allTypesStr = JsonConvert.SerializeObject(t, typeof(T), Newtonsoft.Json.Formatting.Indented, jsonSerializerSettings);
            using (System.IO.FileStream file = System.IO.File.Create(outputLocation))
            {
                using (var writer = new StreamWriter(file))
                {
                    writer.Write(allTypesStr);
                }
            }
        }
    }
}
