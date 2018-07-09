﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NeoSharp.Core.Cryptography;
using NeoSharp.Core.Types;

namespace NeoSharp.Core.Extensions
{
    public static class ByteArrayExtensions
    {
        // TODO: How to inject this?

        private static ICrypto _crypto = new BouncyCastleCrypto();

        /// <summary>
        /// Generate SHA256 hash
        /// </summary>
        /// <param name="value">Value</param>
        /// <returns>Return SHA256 hash</returns>
        public static byte[] Sha256(this IEnumerable<byte> value)
        {
            return _crypto.Sha256(value.ToArray());
        }

        public static UInt160 ToScriptHash(this byte[] script)
        {
            return new UInt160(_crypto.Hash160(script));
        }

        /// <summary>
        /// Generate SHA256 hash
        /// </summary>
        /// <param name="value">Value</param>
        /// <param name="offset">Offset</param>
        /// <param name="count">Count</param>
        /// <returns>Return SHA256 hash</returns>
        public static byte[] Sha256(this byte[] value, int offset, int count)
        {
            return _crypto.Sha256(value, offset, count);
        }

        /// <summary>
        /// Bytarray XOR
        /// </summary>
        /// <param name="x">X</param>
        /// <param name="y">Y</param>
        /// <returns>Return XOR bytearray</returns>
        public static byte[] XOR(this byte[] x, byte[] y)
        {
            if (y == null) throw new ArgumentNullException(nameof(y));
            if (x.Length != y.Length) throw new ArgumentException(nameof(y));

            var result = new byte[x.Length];
            for (var i = 0; i < x.Length; i++)
            {
                result[i] = (byte)(x[i] ^ y[i]);
            }

            return result;
        }

        public static string ToHexString(this IEnumerable<byte> value, bool append0x = false)
        {
            var sb = new StringBuilder();

            foreach (var b in value)
                sb.AppendFormat("{0:x2}", b);

            if (append0x)
            {
                if (sb.Length > 0) return "0x" + sb.ToString();
            }

            return sb.ToString();
        }

        public static T AsSerializable<T>(this byte[] value, int start = 0) where T : ISerializable, new()
        {
            using (var ms = new MemoryStream(value, start, value.Length - start, false))
            using (var reader = new BinaryReader(ms, Encoding.UTF8))
            {
                return reader.ReadSerializable<T>();
            }
        }

        public static ISerializable AsSerializable(this byte[] value, Type type)
        {
            if (!typeof(ISerializable).GetTypeInfo().IsAssignableFrom(type))
                throw new InvalidCastException();
            var serializable = (ISerializable)Activator.CreateInstance(type);
            using (var ms = new MemoryStream(value, false))
            using (var reader = new BinaryReader(ms, Encoding.UTF8))
            {
                serializable.Deserialize(reader);
            }
            return serializable;
        }

        public static T[] AsSerializableArray<T>(this byte[] value, int max = 0x10000000) where T : ISerializable, new()
        {
            using (var ms = new MemoryStream(value, false))
            using (var reader = new BinaryReader(ms, Encoding.UTF8))
            {
                return reader.ReadSerializableArray<T>(max);
            }
        }
    }
}
