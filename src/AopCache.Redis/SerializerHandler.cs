using System;
using MessagePack;
using MessagePack.Resolvers;
using Newtonsoft.Json;

namespace AopCache.Redis
{
    public partial class SerializerHandler
    {
        public static string ToString(object value, Type type)
        {
            return JsonConvert.SerializeObject(value, Formatting.None);
        }

        public static object StringToObject(string value, Type type)
        {
            return value == null ? null : JsonConvert.DeserializeObject(value, type);
        }

        public static byte[] ToBytes(object value, Type type)
        {
            return MessagePackSerializer.Serialize(value, ContractlessStandardResolver.Options);
        }

        public static object BytesToObject(byte[] value, Type type)
        {
            if (value == null)
                return null;

            return MessagePackSerializer.Deserialize(type, value, ContractlessStandardResolver.Options);
        }

        public static T JsonClone<T>(T value)
        {
            var json = ToString(value, null);
            return JsonConvert.DeserializeObject<T>(json);
        }

        public static T BytesClone<T>(T value)
        {
            var bytes = ToBytes(value, null);
            return MessagePackSerializer.Deserialize<T>(bytes, ContractlessStandardResolver.Options);
        }
    }
}