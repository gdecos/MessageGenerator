using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace MessageGenerator.Helpers
{
    public static class CustomSerializer
    {
        public static string Serialize<T>(this T value)
        {
            if (value == null)
                return string.Empty;

            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = false,
                Indent = true,
                NewLineOnAttributes = false,
                Encoding = Encoding.UTF8
            };

            try
            {
                var xmlserializer = new XmlSerializer(typeof(T));
                var stringWriter = new StringWriter();
                using var writer = XmlWriter.Create(stringWriter, settings);
                xmlserializer.Serialize(writer, value);
                return stringWriter.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred", ex);
            }
        }
    }
}