﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SwiftMXMessageGenerator.Helpers
{
    public class AssemblyHelper
    {
        public AssemblyHelper() { }

        public static Type GetNVLPTypes(Assembly assembly)
        {
            var types = from t in assembly.GetTypes()
                        where t.IsClass && (
                           t.Name == "BusinessMessageEnvelopeV01"
                           || t.Name.StartsWith("BusinessMessageEnvelopeV")
                           )
                        select t;

            return types.FirstOrDefault();
        }

        public static Type GetBAHEADERTypes(Assembly assembly)
        {
            var types = from t in assembly.GetTypes()
                        where t.IsClass && (
                           t.Name == "ApplicationHeader"
                            || t.Name == "BusinessApplicationHeaderV01"
                            || t.Name == "BusinessApplicationHeaderV02"
                            || t.Name == "BusinessApplicationHeaderV03"
                           )
                        select t;

            return types.FirstOrDefault();
        }

        public static Type GetDocumentTypes(Assembly assembly)
        {
            var types = from t in assembly.GetTypes()
                        where t.IsClass && (
                           t.Name == "Document"
                           )
                        select t;

            return types.FirstOrDefault();
        }

        public static IEnumerable<Type> GetAllDocumentTypes(Assembly assembly)
        {
            var types = from t in assembly.GetTypes()
                        where t.IsClass && (
                        1 == 2
                        || t.Name == "Document"
                        || t.Name == "BusinessApplicationHeaderV01"
                        || t.Name == "BusinessApplicationHeaderV02"
                        || t.Name == "BusinessApplicationHeaderV03"
                        || t.Name == "BusinessMessageEnvelopeV01"
                        || t.Name == "ApplicationHeader"
                           )
                        select t;

            return types;
        }
    }
}