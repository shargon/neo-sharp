﻿using System;
using System.Numerics;
using NeoSharp.BinarySerialization;
using NeoSharp.Core.Converters;
using NeoSharp.Core.Extensions;

namespace NeoSharp.Core.Cryptography
{
    [BinaryTypeSerializer(typeof(ECPointBinarySerializer))]
    public class ECPoint : IComparable<ECPoint>
    {
        /// <summary>
        /// Infinity
        /// </summary>
        public readonly static ECPoint Infinity = new ECPoint();

        /// <summary>
        /// Encoded data
        /// </summary>
        public readonly byte[] EncodedData;
        /// <summary>
        /// Decoded data
        /// </summary>
        public readonly byte[] DecodedData;
        /// <summary>
        /// Is infinite
        /// </summary>
        public readonly bool IsInfinity;

        /// <summary>
        /// X,Y
        /// </summary>
        public readonly BigInteger X, Y;

        /// <summary>
        /// Infinite constructor
        /// </summary>
        private ECPoint()
        {
            IsInfinity = true;

            DecodedData = EncodedData = new byte[] { 0x00 };

            X = BigInteger.Zero;
            Y = BigInteger.Zero;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="point">Point</param>
        public ECPoint(byte[] point)
        {
            IsInfinity = false;

            EncodedData = ICrypto.Default.DecodePublicKey(point, true, out X, out Y);
            DecodedData = ICrypto.Default.DecodePublicKey(point, false, out X, out Y);
        }

        /// <summary>
        /// Compare ECPoint
        /// </summary>
        /// <param name="other">ECPoint to compare</param>
        /// <returns>Return compare value</returns>
        public int CompareTo(ECPoint other)
        {
            if (other == this) return 0;

            int result = X.CompareTo(other.X);
            if (result != 0) return result;

            return Y.CompareTo(other.Y);
        }

        /// <summary>
        /// String representation
        /// </summary>
        public override string ToString()
        {
            return EncodedData.ToHexString();
        }
        /// <summary>
        /// String representation
        /// </summary>
        /// <param name="append0x">Append 0x</param>
        public string ToString(bool append0x)
        {
            return EncodedData.ToHexString(append0x);
        }
    }
}