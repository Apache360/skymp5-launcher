﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Yandex.Metrica.Aides
{
    public static class Standard
    {
        public static TItem FromJsonStandard<TItem>(
          this string json,
          DataContractJsonSerializerSettings settings = null)
        {
            Type type = typeof(TItem);
            DataContractJsonSerializerSettings settings1 = settings;
            if (settings1 == null)
                settings1 = new DataContractJsonSerializerSettings()
                {
                    UseSimpleDictionaryFormat = true,
                    KnownTypes = (IEnumerable<Type>)new List<Type>()
          {
            typeof (Dictionary<string, object>)
          }
                };
            using (MemoryStream memoryStream = new MemoryStream(Encoding.Unicode.GetBytes(json)))
                return (TItem)new DataContractJsonSerializer(type, settings1).ReadObject((Stream)memoryStream);
        }

        public static string ToJsonStandard<TItem>(
          this TItem item,
          DataContractJsonSerializerSettings settings = null)
        {
            Type type = typeof(TItem);
            DataContractJsonSerializerSettings settings1 = settings;
            if (settings1 == null)
                settings1 = new DataContractJsonSerializerSettings()
                {
                    UseSimpleDictionaryFormat = true,
                    KnownTypes = (IEnumerable<Type>)new List<Type>()
          {
            typeof (Dictionary<string, object>)
          }
                };
            using (MemoryStream memoryStream = new MemoryStream())
            {
                new DataContractJsonSerializer(type, settings1).WriteObject((Stream)memoryStream, (object)item);
                memoryStream.Position = 0L;
                byte[] array = memoryStream.ToArray();
                return Encoding.Unicode.GetString(array, 0, array.Length);
            }
        }
    }
}