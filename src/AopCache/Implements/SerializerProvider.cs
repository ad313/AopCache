using AopCache.Abstractions;
using System;
using System.Linq;
using System.Text.Json;

namespace AopCache.Implements
{
    public class SerializerProvider : ISerializerProvider
    {
        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="data"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public string Serialize(object data, Type type = null)
        {
            return data == null
                ? null
                : type == null
                    ? JsonSerializer.Serialize(data)
                    : JsonSerializer.Serialize(data, type);
        }

        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="data"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public byte[] SerializeBytes(object data, Type type = null)
        {
            return data == null ? null :
                type == null ? JsonSerializer.SerializeToUtf8Bytes(data) :
                JsonSerializer.SerializeToUtf8Bytes(data, type);
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
        public T Deserialize<T>(string json)
        {
            return string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<T>(json);
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="json"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public object Deserialize(string json, Type type)
        {
            return string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize(json, type);
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public T Deserialize<T>(byte[] bytes)
        {
            return bytes == null || !bytes.Any() ? default : JsonSerializer.Deserialize<T>(bytes);
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public object Deserialize(byte[] bytes, Type type)
        {
            return bytes == null || !bytes.Any() ? default : JsonSerializer.Deserialize(bytes, type);
        }

        /// <summary>
        /// 克隆对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public T Clone<T>(T data)
        {
            if (data == null)
                return default;

            return Deserialize<T>(SerializeBytes(data));
        }

        /// <summary>
        /// 克隆对象
        /// </summary>
        /// <param name="data"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public object Clone(object data, Type type)
        {
            if (data == null)
                return default;

            return Deserialize(SerializeBytes(data, type), type);
        }
    }
}