﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PeanutButter.DuckTyping.AutoConversion.Converters;
using PeanutButter.DuckTyping.Extensions;

namespace PeanutButter.DuckTyping.AutoConversion
{
    internal static class ConverterLocator
    {
        private static IConverter[] Converters =>
            _converters ?? (_converters = FindConverters());

        private static IConverter[] _converters;
        private static IConverter[] FindConverters()
        {
            var converterTypes = FindConverterTypes();
            return converterTypes.Select(TryConstruct)
                .Where(c => c != null)
                .Union(MakeStringConverters())
                .ToArray();
        }

        private static IConverter[] MakeStringConverters()
        {
            var types = FindTypesWhichCanTryParseStrings();
            var genericType = typeof(GenericStringConverter<>);
            var converters = new List<IConverter>();
            foreach (var type in types)
            {
                var specific = genericType.MakeGenericType(type);
                var instance = (IConverter)Activator.CreateInstance(specific);
                converters.Add(instance);
            }
            return converters.ToArray();
        }

        private static Type[] FindTypesWhichCanTryParseStrings()
        {
            return AllLoadedTypes.Where(HasValidTryParseMethod).ToArray();
        }

        private static bool HasValidTryParseMethod(Type arg)
        {
            return arg.GetMethods(BindingFlags.Static | BindingFlags.Public)
                                .Any(mi => mi.IsTryParseMethod());
        }


        public static IConverter GetConverter(Type t1, Type t2)
        {
            return Converters.FirstOrDefault(c => CanConvert(c, t1, t2));
        }

        private static bool CanConvert(IConverter converter, Type t1, Type t2)
        {
            var conversionTypes = new[] { converter.T1, converter.T2 };
            return conversionTypes.Contains(t1) && conversionTypes.Contains(t2);
        }

        private static IConverter TryConstruct(Type arg)
        {
            try
            {
                if (arg.IsGenericType)
                    return null;
                return (IConverter)Activator.CreateInstance(arg);
            }
            catch
            {
                return null;
            }
        }

        private static readonly object _allTypesLock = new object();
        private static Type[] _allLoadedTypes;
        private static Type[] AllLoadedTypes
        {
            get
            {
                lock (_allTypesLock)
                {
                    return _allLoadedTypes ?? (_allLoadedTypes = FindAllLoadedTypes());
                }
            }
        }

        private static Type[] FindAllLoadedTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                        .Select(a =>
                        {
                            try
                            {
                                return a.GetTypes();
                            }
                            catch { return new Type[0]; }
                        })
                        .SelectMany(a => a)
                        .ToArray();
        }

        private static Type[] FindConverterTypes()
        {
            var baseType = typeof(IConverter<,>);
            return AllLoadedTypes
                        .Where(t => t.GetAllImplementedInterfaces()
                            .Any(i => i.IsGenericType &&
                                      i.GetGenericTypeDefinition() == baseType))
                        .ToArray();
        }
    }
}