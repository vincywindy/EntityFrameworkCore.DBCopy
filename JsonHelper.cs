using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EntityFrameworkCore
{
    internal static class JsonHelper
    {
        private static JsonSerializerSettings setting;
        static JsonHelper()
        {
            setting = new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore
            };
        }
        internal static void AddJson<T>(string filename, IList<T> list)
        {
            var json = JsonConvert.SerializeObject(list, Formatting.None, setting);
            File.AppendAllText(filename, json);
        }
        internal static IEnumerable<object> GetJson(string filename, Type type)
        {
            var serializer = new JsonSerializer();

            using (var stringReader = File.OpenText(filename))
            using (var jsonReader = new JsonTextReader(stringReader))
            {
                jsonReader.SupportMultipleContent = true;

                while (jsonReader.Read())
                {

                    var json = (IList)serializer.Deserialize(jsonReader, type);
                    foreach (var j in json)
                    {
                        yield return j;
                    }


                }
            }
        }
    }
}
