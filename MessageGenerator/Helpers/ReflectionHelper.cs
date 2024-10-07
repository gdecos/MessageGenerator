using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace MessageGenerator.Helpers
{
    public static class ReflectionHelper
    {
        private static string RootNameSpace = string.Empty;

        private static string DeclaringTypeName = string.Empty;
        private static string ReturnTypeName = string.Empty;
        private static string ReturnTypeNameFull = string.Empty;
        private static string ReturnParamName = string.Empty;
        private static List<PropertyInfo> ReturnParams;
        private static string currentTypeName;
        private static string currentTypeFullName;
        private static string currentTypeElementName;
        private static string currentPropertyName;
        private static string currentPropertyFullName;
        private static string currentPropertyDeclaringTypeName;
        private static string currentPropertyDeclaringTypeFullName;

        public static T GetDocument<T>()
        {
            T DocumentObj = ReflectionHelper.Create<T>();
            return DocumentObj;
        }

        public static T Create<T>()
        {
            var type = typeof(T);

            var namespaceAttempt = type.GetCustomAttributesData().Where(w => w.AttributeType == typeof(System.Xml.Serialization.XmlRootAttribute)).FirstOrDefault();

            if (namespaceAttempt != null)
            {
                var nsArg = namespaceAttempt.NamedArguments.Where(w => w.MemberName == "Namespace").FirstOrDefault();
                RootNameSpace = nsArg.TypedValue.ToString()?.Trim('"');
            }

            MethodInfo[] methodInfo = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

            DeclaringTypeName = methodInfo[0].DeclaringType.Name;
            ReturnTypeName = methodInfo[0].ReturnType.Name;
            ReturnTypeNameFull = methodInfo[0].ReturnType.FullName;
            ReturnParamName = type.GetProperties()[0].Name;
            ReturnParams = type.GetProperties()?.ToList();

            Console.WriteLine("\t {0}", type.FullName);
            Console.WriteLine("\t {0}", new String('_', 100));

            foreach (var prop in type.GetProperties())
            {
                var prop1 = prop.Name;
                var prop2 = prop.PropertyType.Name;
                var prop3 = prop.PropertyType.FullName;
                Console.WriteLine("\t\t {0}\r\n\t\t {1}\r\n\t\t {2}", prop1, prop2, prop3);
            }

            Console.WriteLine("\t{0}", new String('_', 100));

            Console.WriteLine("\t {0}\r\n\t {1}\r\n\t {2}\r\n\t {3} - [{4}]", type.FullName,  DeclaringTypeName, ReturnParamName, ReturnTypeName, ReturnTypeNameFull);

            Console.WriteLine("\t{0}", new String('-', 100));

            return (T)Create(type);
        }

        public static object Create(Type type)
        {
            return Create(type, string.Empty);
        }

        public static object Create(Type type, string objectElementName)
        {
            string x = type.FullName;
            if (type.BaseType.Name == "Array")
            {
                var theType = type.GetElementType();

                if (!theType.IsArray)
                {
                    var objArr = Array.CreateInstance(type.GetElementType(), 1);

                    for (int i = 0; i < objArr.Length; i++)
                    {
                        var obj = Create(type.GetElementType());

                        var properties = type.GetProperties();
                        var item = properties.Where(p => p.Name == "Item").FirstOrDefault();
                        var itemElementName = properties.Where(p => p.Name == "ItemElementName").FirstOrDefault();
                        if (item != null && itemElementName != null)
                        {
                            var b = true;
                        }

                        foreach (var property in type.GetProperties())
                        {
                            var propertyType = property.PropertyType;

                            if (propertyType.IsClass && string.IsNullOrEmpty(propertyType.Namespace)
                                || (!propertyType.Namespace.Equals("System") && !propertyType.Namespace.StartsWith("System.")))
                            {
                                var child = Create(propertyType);
                                property.SetValue(obj, child);
                            }
                            else
                            {
                                var whyhere = "?";
                                if (property.PropertyType.FullName == "System.Int64")
                                {
                                    if (property.CanWrite)
                                        property.SetValue(obj, new Int64());
                                }
                                else if (property.PropertyType.FullName == "System.Int32")
                                {
                                    if (property.CanWrite)
                                        property.SetValue(obj, new Int32());
                                }
                                else
                                {
                                    if (property.CanWrite)
                                        throw new Exception($"Unhandled Type: {property.PropertyType}");
                                }


                            }
                        }
                        objArr.SetValue(obj, i);
                    }
                    return objArr;
                }
                else
                {
                    //our type is an Array of Arrays!!!!
                    //***********************************************************************

                    var theMainType = Create(type.GetElementType().GetElementType());

                    var outerArray = Array.CreateInstance(type.GetElementType(), 1);


                    var objArr = Array.CreateInstance(type.GetElementType(), 1);

                    var innerArray = Array.CreateInstance(theMainType.GetType(), 1);
                    innerArray.SetValue(theMainType, 0);

                    outerArray.SetValue(innerArray, 0);

                    /* REDA.v002_001_04 */
                    /*
                     * BUG the definition in this file should be :
                     *  [System.Xml.Serialization.XmlArrayItemAttribute("PricValtnDtls", typeof(PriceValuation4[]), IsNullable=false)]
                     *  
                     */

                    //PriceValuation4[][] xc = new PriceValuation4[1][];
                    //xc[0] = new PriceValuation4[1];
                    //xc[0][0] = new PriceValuation4
                    //{
                    //    Id = "123"
                    //};                   
                    //outerArray.SetValue(xc, 0);

                    return objArr;
                    //return outerArray;
                }
            }
            else
            {
                if (type.IsAbstract)
                {
                    return null;
                }

                if (type.Name == "String")
                {
                    var retVal = Convert.ChangeType("Text_" + objectElementName, TypeCode.String);
                    return retVal.ToString();
                }

                if (type.Name == "XmlElement")
                {
                    XmlDocument xmlDocSPack = new XmlDocument();
                    XmlNode xmldocNode = xmlDocSPack.CreateXmlDeclaration("1.0", "", null);
                    xmlDocSPack.AppendChild(xmldocNode);

                    XmlElement mainDocNode = xmlDocSPack.CreateElement(System.Xml.XmlConvert.EncodeName("XmlLinkedNode"), namespaceURI: RootNameSpace);
                    //mainDocNode.SetAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
                    //mainDocNode.SetAttribute("xmlns", RootNameSpace);

                    var newAttr = xmlDocSPack.CreateAttribute("Attribute1");
                    newAttr.InnerText = "val for Attribute Here";
                    mainDocNode.Attributes.Append(newAttr);

                    xmlDocSPack.AppendChild(mainDocNode);

                    return mainDocNode;
                }

                if (!type.IsClass && !type.IsEnum & !type.IsValueType)
                    throw new Exception("Not a class");

                var obj = Activator.CreateInstance(type);

                var properties = type.GetProperties();

                if (properties == null) return obj;

                var item = properties.Where(p => p.Name == "Item").FirstOrDefault();
                var itemElementName = properties.Where(p => p.Name == "ItemElementName").FirstOrDefault();

                if (item != null && itemElementName == null)
                {
                    //get rest of properties
                    var otherproperties2 = properties.Where(p => p.Name != "Item" && p.Name != "ItemElementName").ToArray();
                    if (otherproperties2.Length > 0)
                        SetProperties(otherproperties2, obj, objectElementName);

                    Attribute[] attribs2 = Attribute.GetCustomAttributes(item, typeof(System.Xml.Serialization.XmlElementAttribute));
                    //var enumValues2 = System.Enum.GetValues(item.PropertyType);

                    if (attribs2.Length == 1)
                    {

                        int option = 1;
                        var att_item = ((System.Xml.Serialization.XmlElementAttribute)attribs2[option - 1]);

                        if (att_item.Type != null)
                            item.SetValue(obj, Create(att_item.Type, att_item.ElementName));

                        if (item.PropertyType != null)
                        {
                            item.SetValue(obj, Create(item.PropertyType));
                        }
                    }

                    if (attribs2.Length > 1)
                    {
                        Random r = new Random();
                        int option = r.Next(1, attribs2.Length + 1);
                        //option = 2;
                        var att_item = ((System.Xml.Serialization.XmlElementAttribute)attribs2[option - 1]);

                        item.SetValue(obj, Create(att_item.Type, att_item.ElementName));
                    }
                }
                else if (item != null && itemElementName != null)
                {

                    var restOfProperties = properties.Where(p => p.Name != "Item" & p.Name != "ItemElementName").ToArray();
                    if (restOfProperties.Length > 0)
                    {
                        SetProperties(restOfProperties, obj, objectElementName);
                    }

                    Attribute[] attribs = Attribute.GetCustomAttributes(item, typeof(System.Xml.Serialization.XmlElementAttribute));
                    var enumValues = System.Enum.GetValues(itemElementName.PropertyType);

                    if (attribs.Length > 0)
                    {
                        Random r = new Random();
                        int option = r.Next(1, attribs.Length + 1);

                        var att_item = ((System.Xml.Serialization.XmlElementAttribute)attribs[option - 1]);

                        foreach (object enumValue in enumValues)
                        {
                            if (enumValue.ToString() == att_item.ElementName)
                            {
                                var dd = Convert.ChangeType(enumValue, itemElementName.PropertyType);
                                itemElementName.SetValue(obj, dd);
                                break;
                            }
                        }

                        /* $$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$ */
                        /* $$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$ */

                        if (att_item.Type.FullName == "System.String")
                        {
                            currentPropertyFullName = obj.GetType().FullName;
                            currentPropertyName = obj.GetType().Name;
                            item.SetValue(obj, Convert.ChangeType("TEXT - " + obj.GetType().FullName.Split('.').Last(), item.PropertyType));
                        }
                        else if (att_item.Type.FullName == "System.DateTime")
                        {
                            if (att_item.DataType == "date")
                            {
                                item.SetValue(obj, System.DateTime.Now.Date);
                            }
                            else
                            {
                                item.SetValue(obj, System.DateTime.Now.ToUniversalTime());
                            }
                        }
                        else if (att_item.Type.FullName == "System.Decimal")
                        {
                            item.SetValue(obj, new Decimal(1.0));
                        }
                        else if (att_item.Type.FullName == "System.Boolean")
                        {
                            item.SetValue(obj, true);
                        }
                        // OCT 2020
                        else if (att_item.Type.FullName == "System.Byte[]")
                        {
                            item.SetValue(obj, Encoding.ASCII.GetBytes("OCT 2020 BYTE[] DATA"));
                        }
                        else if (att_item.Type.FullName == "System.Xml.XmlElement")
                        {
                            XmlDocument xmlDocSPack = new XmlDocument();
                            XmlNode xmldocNode = xmlDocSPack.CreateXmlDeclaration("1.0", "", null);
                            xmlDocSPack.AppendChild(xmldocNode);

                            XmlElement mainDocNode = xmlDocSPack.CreateElement(System.Xml.XmlConvert.EncodeName(item.Name), namespaceURI: RootNameSpace);
                            mainDocNode.SetAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
                            //mainDocNode.SetAttribute("xmlns", RootNameSpace);

                            var newAttr = xmlDocSPack.CreateAttribute("Attribute1");
                            newAttr.InnerText = "Attribute Value";
                            mainDocNode.Attributes.Append(newAttr);


                            xmlDocSPack.AppendChild(mainDocNode);


                            //CHECK THIS

                            item.SetValue(obj, mainDocNode);
                        }
                        else if (att_item.Type.FullName.StartsWith("System."))
                        {
                            throw new Exception("Unhandled Type");
                        }
                        else
                        {
                            currentTypeName = att_item.Type.Name;
                            currentTypeFullName = att_item.Type.FullName;
                            currentTypeElementName = att_item.ElementName;

                            item.SetValue(obj, Create(att_item.Type, att_item.ElementName));
                        }

                        /* $$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$ */
                        /* $$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$ */
                    }
                }
                else
                {
                    var items = properties.Where(p => p.Name == "Items").FirstOrDefault();
                    var itemsElementName = properties.Where(p => p.Name == "ItemsElementName").FirstOrDefault();

                    if (items != null && itemsElementName != null)
                    {
                        var restOfProperties = properties.Where(p => p.Name != "Items" & p.Name != "ItemsElementName").ToArray();
                        if (restOfProperties.Length > 0)
                        {
                            SetProperties(restOfProperties, obj, objectElementName);
                        }

                        Attribute[] attribs = Attribute.GetCustomAttributes(items, typeof(System.Xml.Serialization.XmlElementAttribute));

                        //var itemsArray = Array.CreateInstance(items.PropertyType, attribs.Length);
                        //var itemsElementNameArray = Array.CreateInstance(itemsElementName.PropertyType, attribs.Length);

                        var enumValues = System.Enum.GetValues(itemsElementName.PropertyType.GetElementType());

                        if (attribs.Length > 0)
                        {
                            int currentIndex = 0;

                            var objList = Array.CreateInstance(items.PropertyType.GetElementType(), attribs.Length);
                            var objListEnums = Array.CreateInstance(itemsElementName.PropertyType.GetElementType(), attribs.Length);

                            for (int i = 0; i < attribs.Length; i++)
                            {
                                var att_item = ((System.Xml.Serialization.XmlElementAttribute)attribs[i]);

                                //items list could be a string
                                if (att_item.Type.ToString() == "System.String")
                                {
                                    currentPropertyFullName = obj.GetType().FullName;
                                    currentPropertyName = obj.GetType().Name;
                                    var itemObj = Convert.ChangeType("TEXT - " + att_item.ElementName, att_item.Type);
                                    objList.SetValue(itemObj, i);
                                }
                                // OCT 2020
                                else if (att_item.Type.ToString() == "System.Boolean")
                                {
                                    var rnd = new Random().NextDouble() > 0.5;

                                    var itemObj = Convert.ChangeType(rnd, att_item.Type);
                                    objList.SetValue(itemObj, i);
                                }
                                else
                                {
                                    if (att_item.Type.ToString().StartsWith("System."))
                                    {
                                        throw new Exception(string.Format("UNHANDLED ITEM ARRAY SYSTEM TYPE - Type is: {0}", att_item.Type?.FullName));
                                    }
                                    var itemObj = Create(att_item.Type, att_item.ElementName);
                                    objList.SetValue(itemObj, i);
                                }

                                foreach (object enumValue in enumValues)
                                {
                                    if (enumValue.ToString() == att_item.ElementName)
                                    {
                                        var dd = Convert.ChangeType(enumValue, itemsElementName.PropertyType.GetElementType());
                                        objListEnums.SetValue(dd, i);
                                        break;
                                    }
                                }
                                currentIndex++;
                            }
                            items.SetValue(obj, objList);
                            itemsElementName.SetValue(obj, objListEnums);
                        }
                    }
                    else
                    {
                        SetProperties(properties, obj, objectElementName);
                    }
                }
                return obj;
            }
        }

        private static void SetProperties(System.Reflection.PropertyInfo[] properties, object obj, string objectElementName)
        {
            if (obj != null)
            {
                if (
                    obj.GetType().FullName.Contains("CtrlSum") || properties.Any(a => a.Name.Contains("CtrlSum"))
                )
                {
                    var here123 = 1234;

                }
            }

            foreach (var property in properties)
            {
                currentPropertyName = property.Name;
                currentPropertyDeclaringTypeName = property.DeclaringType.Name;
                currentPropertyDeclaringTypeFullName = property.DeclaringType.FullName;

                var propertyType = property.PropertyType;

                //-		$exception	{"Type: System.Nullable`1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]] Not handled"}	System.Exception
                if (propertyType.FullName.Contains("System.Nullable`1"))
                {
                    var child = Create(propertyType);

                    var nullableObject = Activator.CreateInstance(propertyType);
                    var nullableObjectCreated = Create(propertyType, propertyType.FullName);

                    var nullableType = Nullable.GetUnderlyingType(propertyType);
                    var val = Create(nullableType);

                    //var o = Convert.ChangeType(child, child.GetType()); - NULL
                    //property.SetValue(obj, child);
                    property.SetValue(obj, val);
                }
                else if (propertyType.FullName.Contains("System.Collections.ObjectModel.Collection`1"))
                {
                    var child = Create(propertyType);
                    //DYNAMICALLY SETTING THE ABOVE
                    var o = Convert.ChangeType(child, child.GetType());

                    var p = o.GetType().GetProperty("Item");

                    var objCollection = Activator.CreateInstance(o.GetType());

                    var propertiesCollection = o.GetType().GetProperties();

                    if (propertiesCollection == null) return ;

                    foreach (var itemsCollectionProperty in propertiesCollection)
                    {
                        if (itemsCollectionProperty.CanWrite)
                        {
                            var newInnerChild = Create(itemsCollectionProperty.PropertyType);
                            //objCollection.GetType().GetProperty("Item").SetValue(objCollection, newInnerChild, new object[] { 0 });
                            //objCollection.GetType().GetProperty("Item").SetValue(objCollection, newInnerChild);
                            // objCollection.GetType().GetMethod("set_Item").Invoke(objCollection, new object[] { 0, newInnerChild });
                            //objCollection.GetType().GetMethod("set_Item").Invoke(objCollection, new object[] { 0, newInnerChild });
                            //(objCollection as ICollection<object>).Add(newInnerChild);

                            objCollection.GetType().GetMethod("Add").Invoke(objCollection, new object[] { newInnerChild });
                        }
                    }
                    if (property.CanWrite)
                        property.SetValue(obj, objCollection);
                }

                /* ********************************************************************************************************************** */
                /* ********************************************************************************************************************** */
                /* ********************************************************************************************************************** */
                else if (propertyType.IsClass
                    && string.IsNullOrEmpty(propertyType.Namespace)
                    || (!propertyType.Namespace.Equals("System")
                        && !propertyType.Namespace.StartsWith("System.")))
                {

                    if (propertyType.IsEnum)
                    {
                        var e = Enum.GetValues(propertyType);
                        //
                        if (property.CanWrite)
                            property.SetValue(obj, e.GetValue(new Random().Next(0, e.Length))); //e.Length - 1
                        else
                        {
//                           property.SetValue(obj, e.GetValue(new Random().Next(0, e.Length))); //e.Length - 1
                        }
                    }
                    else
                    {
                        var child = Create(propertyType, property.Name);

                        //DYNAMICALLY SETTING THE ABOVE
                        var o = Convert.ChangeType(child, child.GetType());

                        var p = o.GetType().GetProperty("Item");
                        var ie = o.GetType().GetProperty("ItemElementName");
                        if (p != null && ie != null)
                        {
                            if (p.GetValue(o) != null && ie.GetValue(o) != null)
                            {
                                //DO Nothing. The item and itelementname have already be set to the correct types
                            }
                            else
                            {
                                //Somehow the item and itemelementname properties have not been set
                                //**************************************************************************/
                                Attribute[] attribs = Attribute.GetCustomAttributes(p, typeof(System.Xml.Serialization.XmlElementAttribute));
                                var enumValues = System.Enum.GetValues(ie.PropertyType);
                                if (attribs.Length > 0)
                                {
                                    Random r = new Random();
                                    int option = r.Next(1, attribs.Length + 1);

                                    var att_item = ((System.Xml.Serialization.XmlElementAttribute)attribs[option - 1]);

                                    foreach (object enumValue in enumValues)
                                    {
                                        if (enumValue.ToString() == att_item.ElementName)
                                        {
                                            var dd = Convert.ChangeType(enumValue, ie.PropertyType);
                                            ie.SetValue(o, dd);
                                            break;
                                        }
                                    }

                                    if (att_item.Type.IsClass && string.IsNullOrEmpty(att_item.Type.Namespace)
                                    || (!att_item.Type.Namespace.Equals("System") && !att_item.Type.Namespace.StartsWith("System.")))
                                    {
                                        currentTypeName = att_item.Type.Name;
                                        currentTypeFullName = att_item.Type.FullName;
                                        currentTypeElementName = att_item.ElementName;

                                        p.SetValue(o, Create(att_item.Type, att_item.ElementName));
                                    }
                                }
                            }
                            //****************************************************************************/
                        }
                        else if (p != null)
                        {
                            // OCT 2020

                            if (p.PropertyType.Name == "String")
                            {
                                currentPropertyFullName = o.GetType().FullName;
                                currentPropertyName = o.GetType().Name;
                                p.SetValue(o, Convert.ChangeType("TEXT - " + o.GetType().FullName.Split('.').Last(), p.PropertyType), null);
                            }
                            else
                            {
                                //here it is an item but has no itemelementname
                                Attribute[] attribs = Attribute.GetCustomAttributes(p, typeof(System.Xml.Serialization.XmlElementAttribute));

                                int totalOptions = attribs.Length;

                                Random r = new Random();
                                int option = r.Next(1, totalOptions + 1);
                                int idx = 0;

                                //option = 1;
                                foreach (var att in attribs)
                                {
                                    idx++;
                                    if (idx != option) continue;

                                    var att_item = ((System.Xml.Serialization.XmlElementAttribute)att);

                                    if (att_item.Type.FullName == "System.String")
                                    {
                                        currentPropertyFullName = o.GetType().FullName;
                                        currentPropertyName = o.GetType().Name;
                                        p.SetValue(o, Convert.ChangeType("TEXT - " + o.GetType().FullName.Split('.').Last(), p.PropertyType), null);
                                    }
                                    else if (att_item.Type.FullName == "System.DateTime")
                                    {
                                        //if (att_item.ElementName == "DtTm")
                                        if (att_item.DataType == "date")
                                        {
                                            p.SetValue(o, System.DateTime.Now.Date);
                                        }
                                        else
                                        {
                                            p.SetValue(o, System.DateTime.Now.ToUniversalTime());
                                        }
                                    }
                                    else if (att_item.Type.FullName == "System.Decimal")
                                    {
                                        p.SetValue(o, new Decimal(1.0));
                                    }
                                    else if (att_item.Type.FullName == "System.Boolean")
                                    {
                                        p.SetValue(o, true);
                                    }
                                    else if (att_item.Type.FullName == "System.Xml.XmlElement")
                                    {
                                        XmlDocument xmlDocSPack = new XmlDocument();
                                        XmlNode xmldocNode = xmlDocSPack.CreateXmlDeclaration("1.0", "", null);
                                        xmlDocSPack.AppendChild(xmldocNode);

                                        XmlElement mainDocNode = xmlDocSPack.CreateElement(System.Xml.XmlConvert.EncodeName(property.Name), namespaceURI: RootNameSpace);
                                        mainDocNode.SetAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
                                        //mainDocNode.SetAttribute("xmlns", RootNameSpace);

                                        var newAttr = xmlDocSPack.CreateAttribute("Attribute1");
                                        newAttr.InnerText = "value 1";
                                        mainDocNode.Attributes.Append(newAttr);


                                        xmlDocSPack.AppendChild(mainDocNode);


                                        //CHECK THIS

                                        p.SetValue(o, mainDocNode);
                                    }
                                    // OCT 2020
                                    else if (att_item.Type.FullName == "System.Byte[]")
                                    {
                                        //p.SetValue(o, Encoding.ASCII.GetBytes("OCT 2020 #2 BYTE[] DATA"));
                                        p.SetValue(o, new byte[] { 0 });
                                    }
                                    else if (att_item.Type.FullName.StartsWith("System."))
                                    {
                                        throw new Exception("Unhandled Type");
                                    }
                                    else
                                    {
                                        currentTypeName = att_item.Type.Name;
                                        currentTypeFullName = att_item.Type.FullName;
                                        currentTypeElementName = att_item.ElementName;
                                        p.SetValue(o, Create(att_item.Type, att_item.ElementName), null);
                                    }
                                }
                            }

                        }

                        property.SetValue(obj, child);
                    }
                }
                else
                {
                    if (propertyType.IsEnum)
                    {
                        var enumerator = true;
                    }
                    switch (propertyType.FullName)
                    {
                        case "System.String":
                            var child = new String('*', 3) + " " + property.Name + " " + property.DeclaringType.Name + " " + new String('*', 3);
                            
                            if (property.Name == "NbOfTxs" ||   property.Name.Contains("NbOfTxs") || property.DeclaringType.Name.Contains("NbOfTxs")
                                || property.Name.Contains("NumberOfTransactions") || property.DeclaringType.Name.Contains("NumberOfTransactions"))
                            {
                                Attribute[] testAttribs = Attribute.GetCustomAttributes(property, typeof(System.Xml.Serialization.XmlElementAttribute));
                            }

                            if (property.CanWrite)
                                property.SetValue(obj, child);
                            break;
                        case "System.String[]":
                            if (property.CanWrite)
                                property.SetValue(obj, new string[] {
                                new String('*', 3) + " " + property.Name + " " + property.DeclaringType.Name + " " + new String('*', 3) + " 1",
                                new String('*', 3) + " " + property.Name + " " + property.DeclaringType.Name + " " + new String('*', 3) + " 2"});
                            break;
                        case "System.Object[]":

                            if (property.Name == "Items")
                            {
                                //DYNAMICALLY SETTING THE ABOVE
                                Attribute[] attribs = Attribute.GetCustomAttributes(property, typeof(System.Xml.Serialization.XmlElementAttribute));

                                int totalOptions = attribs.Length;
                                List<object> objList = new List<object>();

                                foreach (var att in attribs)
                                {
                                    var att_item = ((System.Xml.Serialization.XmlElementAttribute)att);

                                    if (att_item.Type.FullName == "System.String")
                                    {
                                        objList.Add(new String('*', 3) + " " + property.Name + " " + property.DeclaringType.Name + " " + new String('*', 3) + " 1");
                                    }
                                    else if (att_item.Type.FullName == "System.DateTime")
                                    {
                                        objList.Add(System.DateTime.Now.ToUniversalTime());
                                    }
                                    else if (att_item.Type.FullName == "System.Boolean")
                                    {
                                        objList.Add(true);
                                    }
                                    else if (att_item.Type.FullName == "System.Decimal")
                                    {
                                        objList.Add(new decimal(1.2));
                                    }
                                    else if (att_item.Type.FullName == "System.Byte[]")
                                    {
                                        objList.Add(Encoding.ASCII.GetBytes("BYTE DATA HERE"));
                                    }
                                    else if (att_item.Type.FullName.StartsWith("System."))
                                    {
                                        throw new Exception("here 3");
                                    }
                                    else
                                    {
                                        objList.Add(Create(att_item.Type, att_item.ElementName));
                                    }
                                }

                                property.SetValue(obj, objList.ToArray());
                            }
                            else
                                property.SetValue(obj, null);
                            break;
                        case "System.Object":

                            if (property.Name == "Item")
                            {

                                //DYNAMICALLY SETTING THE ABOVE
                                Attribute[] attribs = Attribute.GetCustomAttributes(property, typeof(System.Xml.Serialization.XmlElementAttribute));

                                int totalOptions = attribs.Length;

                                Random r = new Random();
                                int option = r.Next(1, totalOptions + 1);

                                option = 1;
                                int idx = 0;
                                foreach (var att in attribs)
                                {
                                    idx++;
                                    if (idx != option) continue;

                                    var att_item = ((System.Xml.Serialization.XmlElementAttribute)att);

                                    if (att_item.Type.FullName == "System.String")
                                    {
                                        currentPropertyFullName = obj.GetType().FullName;
                                        currentPropertyName = obj.GetType().Name;
                                        property.SetValue(obj, Convert.ChangeType("TEXT - " + obj.GetType().FullName.Split('.').Last(), property.PropertyType), null);
                                    }
                                    else if (att_item.Type.FullName == "System.DateTime")
                                    {
                                        if (att_item.DataType == "date")
                                        {
                                            property.SetValue(obj, System.DateTime.Now.Date);
                                        }
                                        else
                                        {
                                            property.SetValue(obj, System.DateTime.Now.ToUniversalTime());
                                        }
                                    }
                                    else if (att_item.Type.FullName == "System.Decimal")
                                    {
                                        property.SetValue(obj, new Decimal(123.0));

                                        var ItemElementName = obj.GetType().GetProperty("ItemElementName");
                                        if (ItemElementName != null)
                                        {
                                            var enumValues = System.Enum.GetValues(ItemElementName.PropertyType);

                                            foreach (object enumValue in enumValues)
                                            {
                                                if (enumValue.ToString() == att_item.ElementName)
                                                {
                                                    var dd = Convert.ChangeType(enumValue, ItemElementName.PropertyType);
                                                    ItemElementName.SetValue(obj, dd);
                                                }

                                                if (enumValue.ToString() == "Unit")
                                                {
                                                    var dd = Convert.ChangeType(enumValue, ItemElementName.PropertyType);
                                                    ItemElementName.SetValue(obj, dd);
                                                }
                                            }
                                        }
                                    }
                                    else if (att_item.Type.FullName == "System.Boolean")
                                    {
                                        property.SetValue(obj, true);

                                        var ItemElementName = obj.GetType().GetProperty("ItemElementName");
                                        if (ItemElementName != null)
                                        {
                                            var enumValues = System.Enum.GetValues(ItemElementName.PropertyType);

                                            foreach (object enumValue in enumValues)
                                            {
                                                if (enumValue.ToString() == att_item.ElementName)
                                                {
                                                    var dd = Convert.ChangeType(enumValue, ItemElementName.PropertyType);
                                                    ItemElementName.SetValue(obj, dd);
                                                }
                                            }
                                        }

                                    }
                                    else if (att_item.Type.FullName == "System.Xml.XmlElement")
                                    {
                                        XmlDocument xmlDocSPack1 = new XmlDocument();
                                        XmlNode xmldocNode1 = xmlDocSPack1.CreateXmlDeclaration("1.0", "", null);
                                        xmlDocSPack1.AppendChild(xmldocNode1);

                                        XmlElement mainDocNode1 = xmlDocSPack1.CreateElement(System.Xml.XmlConvert.EncodeName(property.Name), namespaceURI: RootNameSpace);
                                        mainDocNode1.SetAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
                                        //mainDocNode1.SetAttribute("xmlns", RootNameSpace);

                                        var newAttr1 = xmlDocSPack1.CreateAttribute("Attribute1");
                                        newAttr1.InnerText = "SomeValue";
                                        mainDocNode1.Attributes.Append(newAttr1);


                                        xmlDocSPack1.AppendChild(mainDocNode1);


                                        //CHECK THIS

                                        property.SetValue(obj, mainDocNode1);
                                    }
                                    else if (att_item.Type.FullName == "System.Decimal[]")
                                    {
                                        property.SetValue(obj, new Decimal[] { 1, 2, 3 });
                                    }
                                    else if (att_item.Type.FullName.StartsWith("System."))
                                    {
                                        throw new Exception("Unhandled Type");
                                    }
                                    else
                                    {
                                        property.SetValue(obj, Create(att_item.Type, att_item.ElementName), null);
                                    }
                                }
                            }
                            else
                                property.SetValue(obj, null);
                            break;
                        case "System.Xml.XmlElement":

                            XmlDocument xmlDocSPack = new XmlDocument();
                            XmlNode xmldocNode = xmlDocSPack.CreateXmlDeclaration("1.0", "", null);
                            xmlDocSPack.AppendChild(xmldocNode);

                            XNamespace docNamespace = RootNameSpace;

                            //CHECK THIS

                            var sampleHeader = @"D:\Swift Messaging\_ouput\ISO-20022\xml-samples\NVLP-Head";
                            sampleHeader = @"D:\Swift Messaging\_ouput\ISO-20022\xml-samples\NVLP-Head\OramaTech.Swift.Iso20022.Head.v001_001_04.BusinessApplicationHeader8-7acdf286-92f6-4f32-b62f-ee82b39210d8_20241006_105635_PM.xml";
                           
                            var sampleMessage = @"D:\Swift Messaging\_ouput\ISO-20022\xml-samples\NVLP-Msg";
                            sampleMessage = @"D:\Swift Messaging\_ouput\ISO-20022\xml-samples\NVLP-Msg\ACH PAIN File_OK.xml";

                            if (
                                property.Name == "Hdr"
                                || (property.Name == "Any" && objectElementName == "Hdr")
                                )
                            {
                                /*
                                XNamespace headNS = "urn:iso:std:iso:20022:tech:xsd:head.001.001.03";
                                XElement root = new XElement(docNamespace + "AppHdr",
                                    new XAttribute("xmlns", headNS),
                                    new XAttribute(XNamespace.Xmlns + "Hdr", headNS),
                                    new XElement(headNS + "Child",
                                        new XElement(docNamespace + "DifferentChild", "other content")
                                    ),
                                    new XElement(headNS + "Child2", "c2 content"),
                                    new XElement(docNamespace + "Child3", "c3 content")
                                );
                                var reader = root.CreateReader();
                                reader.MoveToContent();
                                xmlDocSPack.LoadXml(reader.ReadOuterXml());
                                */

                                //AppHdr
                                //urn:iso:std:iso:20022:tech:xsd:head.001.001.03
                                XmlElement mainDocNode = xmlDocSPack.CreateElement(System.Xml.XmlConvert.EncodeName("AppHdr"), namespaceURI: RootNameSpace);
                                //mainDocNode.SetAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
                                /////////////////////////mainDocNode.SetAttribute("xmlns", RootNameSpace);
                                //mainDocNode.SetAttribute("xmlns", "urn:iso:std:iso:20022:tech:xsd:head.001.001.03");

                                //var newAttr = xmlDocSPack.CreateAttribute("Attribute1");
                                //newAttr.InnerText = "New Attribute Value";
                                //mainDocNode.Attributes.Append(newAttr);

                                var sampleHdrXmlDoc = new XmlDocument();
                                 sampleHdrXmlDoc.Load(sampleHeader);

                                // error diff contect
                                //mainDocNode.AppendChild(sampleHdrXmlDoc.DocumentElement);
                                // creates AppHrd/AppHdr nested
                                //mainDocNode.AppendChild(xmlDocSPack.ImportNode(sampleHdrXmlDoc.DocumentElement, true));
                                // creates correct
                                xmlDocSPack.AppendChild(xmlDocSPack.ImportNode(sampleHdrXmlDoc.DocumentElement, true));

                                //xmlDocSPack.AppendChild(mainDocNode);

                                //old code working - same naemspaces generated
                                //xmlDocSPack = new XmlDocument();
                                //xmlDocSPack.Load(sampleHeader);

                            }
                            else if (
                                property.Name == "Doc"
                                || (property.Name == "Any" && objectElementName == "Doc")
                                )
                            {
                                //Document
                                //urn:swift:xsd:pain.998.001.03
                                XmlElement mainDocNode = xmlDocSPack.CreateElement(System.Xml.XmlConvert.EncodeName("Document"), namespaceURI: RootNameSpace);
                                //mainDocNode.SetAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
                                /////////////////////////mainDocNode.SetAttribute("xmlns", RootNameSpace);
                                //mainDocNode.SetAttribute("xmlns", "urn:swift:xsd:pain.998.001.03");

                                //var newAttr = xmlDocSPack.CreateAttribute("Attribute1");
                                //newAttr.InnerText = "New Attribute Value";
                                //mainDocNode.Attributes.Append(newAttr);

                                var sampleDocMessageXmlDoc = new XmlDocument();
                                sampleDocMessageXmlDoc.Load(sampleMessage);

                                xmlDocSPack.AppendChild(xmlDocSPack.ImportNode(sampleDocMessageXmlDoc.DocumentElement, true));

                                //worked
                                //xmlDocSPack.AppendChild(mainDocNode);

                                //xmlDocSPack = new XmlDocument();
                                //xmlDocSPack.Load(sampleMessage);
                            }
                            else
                            {
                                var item = "NOT HDR OR Doc";
                                XmlElement mainDocNode = xmlDocSPack.CreateElement(System.Xml.XmlConvert.EncodeName($"Envelope_{property.Name}"), namespaceURI: RootNameSpace);

                                var innerObj = xmlDocSPack.CreateElement(System.Xml.XmlConvert.EncodeName("Custom_AddInfo"), namespaceURI: RootNameSpace);
                                var newAttr = xmlDocSPack.CreateAttribute("CustomAttributePropertyName");
                                newAttr.InnerText = $"{property.Name}-{objectElementName}";
                                innerObj.Attributes.Append(newAttr);

                                mainDocNode.AppendChild(innerObj);

                                xmlDocSPack.AppendChild(mainDocNode);
                            }

                            property.SetValue(obj, xmlDocSPack.DocumentElement);
                            //property.SetValue(obj, null);


                            break;
                        case "System.DateTime":

                            Attribute[] attribs1 = Attribute.GetCustomAttributes(property, typeof(System.Xml.Serialization.XmlElementAttribute));
                            bool found = false;
                            foreach (var att in attribs1)
                            {
                                var att_item = ((System.Xml.Serialization.XmlElementAttribute)att);

                                if (att_item.DataType == "date")
                                {
                                    found = true;
                                    property.SetValue(obj, System.DateTime.Now.Date);
                                }
                                else
                                {
                                    found = true;
                                    property.SetValue(obj, DateTime.Now.ToUniversalTime());
                                }

                            }
                            if (!found)
                            {
                                if (property.CanWrite)
                                {
                                    property.SetValue(obj, DateTime.Now.ToUniversalTime());
                                }
                                else
                                {
                                    var isReadonly = true;
                                }
                            }

                            break;
                        case "System.DateTime[]":

                            Attribute[] attribs1a = Attribute.GetCustomAttributes(property, typeof(System.Xml.Serialization.XmlElementAttribute));
                            bool founda = false;
                            foreach (var att in attribs1a)
                            {
                                var att_item = ((System.Xml.Serialization.XmlElementAttribute)att);

                                if (att_item.DataType == "date")
                                {
                                    founda = true;
                                    property.SetValue(obj, new System.DateTime[] { System.DateTime.Now.Date, System.DateTime.Now.Date.AddDays(1) });
                                }
                                else
                                {
                                    founda = true;
                                    property.SetValue(obj, new System.DateTime[] { DateTime.Now.ToUniversalTime(), DateTime.Now.AddDays(1).ToUniversalTime() });
                                }

                            }
                            if (!founda)
                                property.SetValue(obj, new System.DateTime[] { DateTime.Now.ToUniversalTime(), DateTime.Now.AddDays(1).ToUniversalTime() });

                            break;
                        case "System.Boolean":
                            if (property.CanWrite)
                                property.SetValue(obj, true);

                            //DYNAMICALLY SETTING THE ABOVE
                            Attribute[] attribsb = Attribute.GetCustomAttributes(property, typeof(System.Xml.Serialization.XmlElementAttribute));
                            int totalOptionsb = attribsb.Length;
                            if (totalOptionsb > 0)
                            {
                                int here = 1;
                            }

                            break;
                        case "System.Int16":
                            if (property.CanWrite)
                                property.SetValue(obj, (Int16)1);
                            break;
                        case "System.Int32":
                            if (property.CanWrite)
                                property.SetValue(obj, (Int32)2);
                            break;
                        case "System.Int64":
                            if (property.CanWrite)
                                property.SetValue(obj, (Int64)3);
                            break;
                        case "System.Decimal":
                            if (property.CanWrite)
                                property.SetValue(obj, new decimal(1.5));
                            break;
                        case "System.Decimal[]":
                            if (property.CanWrite)
                                property.SetValue(obj, new decimal[] { new decimal(1.0), new decimal(2.0) });
                            break;
                        case "System.Byte[]":
                            if (property.CanWrite)
                                property.SetValue(obj, Encoding.ASCII.GetBytes("BYTE DATA HERE"));
                            break;
                        case "System.Byte[][]":

                            byte[][] scores = new byte[5][];
                            for (int b = 0; b < scores.Length; b++)
                            {
                                scores[b] = new byte[4];
                            }

                            property.SetValue(obj, scores);
                            break;
                        // OCT 2020
                        case "System.Boolean[]":
                            if (property.CanWrite)
                                property.SetValue(obj, new bool[] { true, false });
                            break;
                        case "System.DayOfWeek":
                            if (property.CanWrite)
                                property.SetValue(obj, System.DayOfWeek.Monday);
                            break;
                        case "System.DateTimeKind":
                            if (property.CanWrite)
                                property.SetValue(obj, System.DateTimeKind.Utc);
                            break;
                        case "System.DateTimeOffset":
                            if (property.CanWrite)
                            {
                                DateTime time1 = DateTime.UtcNow;
                                DateTimeOffset time2 = time1;
                                property.SetValue(obj, time2);
                            }
                            break;
                        case "System.TimeSpan":
                            if (property.CanWrite)
                                property.SetValue(obj, DateTime.UtcNow);
                            break;
                        case "System.Byte":
                            if (property.CanWrite)
                                property.SetValue(obj, byte.MaxValue);
                            break;
                        case "System.SByte":
                            if (property.CanWrite)
                                property.SetValue(obj, sbyte.MaxValue);
                            break;
                        //FIX
                        case "System.Single":
                            if (property.CanWrite)
                                property.SetValue(obj, Single.MaxValue);
                            break;
                        case "System.Xml.XmlElement[]":

                            XmlDocument xmlElementDoc1 = new XmlDocument();
                            XmlNode xmlElementDocNode1 = xmlElementDoc1.CreateXmlDeclaration("1.0", "", null);
                            xmlElementDoc1.AppendChild(xmlElementDocNode1);

                            var elem1 = xmlElementDoc1.CreateElement(System.Xml.XmlConvert.EncodeName(property.Name + "_1"), namespaceURI: RootNameSpace);
                            var newAttr1forElem = xmlElementDoc1.CreateAttribute("Attribute1");
                            newAttr1forElem.InnerText = "New Attribute Value 1";
                            elem1.Attributes.Append(newAttr1forElem);

                            var elem2 = xmlElementDoc1.CreateElement(System.Xml.XmlConvert.EncodeName(property.Name + "_2"), namespaceURI: RootNameSpace);
                            newAttr1forElem = xmlElementDoc1.CreateAttribute("Attribute2");
                            newAttr1forElem.InnerText = "New Attribute Value 2";
                            elem2.Attributes.Append(newAttr1forElem);

                            var xmlElementArray = new System.Xml.XmlElement[]
                            {
                                elem1,
                                elem2
                            };

                            if (property.CanWrite)
                                property.SetValue(obj, xmlElementArray);

                            //throw new NotImplementedException("test");
                            break;
                        default:
                            throw new Exception(string.Format("Type: {0} Not handled", propertyType.FullName));
                    }
                }
            }
        }
    }
}
