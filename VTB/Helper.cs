﻿using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace VTB
{
    public static class Helper
    {
        // https://github.com/sander-/working-with-digital-signatures
        // https://bitcoin.stackexchange.com/a/25039

        public static class Base58Encoding
        {
            public const int CheckSumSizeInBytes = 4;

            public static byte[] AddCheckSum(byte[] data)
            {
                byte[] checkSum = GetCheckSum(data);
                byte[] dataWithCheckSum = ArrayHelpers.ConcatArrays(data, checkSum);
                return dataWithCheckSum;
            }

            //Returns null if the checksum is invalid
            public static byte[] VerifyAndRemoveCheckSum(byte[] data)
            {
                byte[] result = ArrayHelpers.SubArray(data, 0, data.Length - CheckSumSizeInBytes);
                byte[] givenCheckSum = ArrayHelpers.SubArray(data, data.Length - CheckSumSizeInBytes);
                byte[] correctCheckSum = GetCheckSum(result);
                if (givenCheckSum.SequenceEqual(correctCheckSum))
                    return result;
                else
                    return null;
            }

            private const string Digits = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

            public static string Encode(byte[] data)
            {
                // Decode byte[] to BigInteger
                BigInteger intData = 0;
                for (int i = 0; i < data.Length; i++)
                {
                    intData = intData * 256 + data[i];
                }

                // Encode BigInteger to Base58 string
                string result = "";
                while (intData > 0)
                {
                    int remainder = (int)(intData % 58);
                    intData /= 58;
                    result = Digits[remainder] + result;
                }

                // Append `1` for each leading 0 byte
                for (int i = 0; i < data.Length && data[i] == 0; i++)
                {
                    result = '1' + result;
                }
                return result;
            }

            public static string EncodeWithCheckSum(byte[] data)
            {
                return Encode(AddCheckSum(data));
            }

            public static byte[] Decode(string s)
            {
                // Decode Base58 string to BigInteger 
                BigInteger intData = 0;
                for (int i = 0; i < s.Length; i++)
                {
                    int digit = Digits.IndexOf(s[i]); //Slow
                    if (digit < 0)
                        throw new FormatException(string.Format("Invalid Base58 character `{0}` at position {1}", s[i], i));
                    intData = intData * 58 + digit;
                }

                // Encode BigInteger to byte[]
                // Leading zero bytes get encoded as leading `1` characters
                int leadingZeroCount = s.TakeWhile(c => c == '1').Count();
                var leadingZeros = Enumerable.Repeat((byte)0, leadingZeroCount);
                var bytesWithoutLeadingZeros =
                    intData.ToByteArray()
                    .Reverse()// to big endian
                    .SkipWhile(b => b == 0);//strip sign byte
                var result = leadingZeros.Concat(bytesWithoutLeadingZeros).ToArray();
                return result;
            }

            // Throws `FormatException` if s is not a valid Base58 string, or the checksum is invalid
            public static byte[] DecodeWithCheckSum(string s)
            {
                var dataWithCheckSum = Decode(s);
                var dataWithoutCheckSum = VerifyAndRemoveCheckSum(dataWithCheckSum);
                if (dataWithoutCheckSum == null)
                    throw new FormatException("Base58 checksum is invalid");
                return dataWithoutCheckSum;
            }

            private static byte[] GetCheckSum(byte[] data)
            {
                SHA256 sha256 = new SHA256Managed();
                byte[] hash1 = sha256.ComputeHash(data);
                byte[] hash2 = sha256.ComputeHash(hash1);

                var result = new byte[CheckSumSizeInBytes];
                Buffer.BlockCopy(hash2, 0, result, 0, result.Length);

                return result;
            }
        }

        public class ArrayHelpers
        {
            public static T[] ConcatArrays<T>(params T[][] arrays)
            {
                var result = new T[arrays.Sum(arr => arr.Length)];
                int offset = 0;
                for (int i = 0; i < arrays.Length; i++)
                {
                    var arr = arrays[i];
                    Buffer.BlockCopy(arr, 0, result, offset, arr.Length);
                    offset += arr.Length;
                }
                return result;
            }

            public static T[] ConcatArrays<T>(T[] arr1, T[] arr2)
            {
                var result = new T[arr1.Length + arr2.Length];
                Buffer.BlockCopy(arr1, 0, result, 0, arr1.Length);
                Buffer.BlockCopy(arr2, 0, result, arr1.Length, arr2.Length);
                return result;
            }

            public static T[] SubArray<T>(T[] arr, int start, int length)
            {
                var result = new T[length];
                Buffer.BlockCopy(arr, start, result, 0, length);
                return result;
            }

            public static T[] SubArray<T>(T[] arr, int start)
            {
                return SubArray(arr, start, arr.Length - start);
            }
        }

        public static bool VerifySignature(string message, string publicKey, string signature)
        {
            var curve = SecNamedCurves.GetByName("secp256k1");
            var domain = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);

            var publicKeyBytes = Base58Encoding.Decode(publicKey);

            var q = curve.Curve.DecodePoint(publicKeyBytes);

            var keyParameters = new
                    Org.BouncyCastle.Crypto.Parameters.ECPublicKeyParameters(q,
                    domain);

            ISigner signer = SignerUtilities.GetSigner("SHA-256withECDSA");

            signer.Init(false, keyParameters);
            signer.BlockUpdate(Encoding.ASCII.GetBytes(message), 0, message.Length);

            var signatureBytes = Base58Encoding.Decode(signature);

            return signer.VerifySignature(signatureBytes);
        }

        public static string GetSignature(string privateKey, string message)
        {
            var curve = SecNamedCurves.GetByName("secp256k1");
            var domain = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);

            var keyParameters = new
                    ECPrivateKeyParameters(new Org.BouncyCastle.Math.BigInteger(privateKey),
                    domain);

            ISigner signer = SignerUtilities.GetSigner("SHA-256withECDSA");

            signer.Init(true, keyParameters);
            signer.BlockUpdate(Encoding.ASCII.GetBytes(message), 0, message.Length);
            var signature = signer.GenerateSignature();
            return Base58Encoding.Encode(signature);
        }

        public static string GetPublicKeyFromPrivateKeyEx(string privateKey)
        {
            var curve = SecNamedCurves.GetByName("secp256k1");
            var domain = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);

            var d = new Org.BouncyCastle.Math.BigInteger(privateKey);
            var q = domain.G.Multiply(d);

            var publicKey = new ECPublicKeyParameters(q, domain);
            return Base58Encoding.Encode(publicKey.Q.GetEncoded());
        }

        private static string GetPublicKeyFromPrivateKey(string privateKey)
        {
            var p = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFEFFFFFC2F", NumberStyles.HexNumber);
            var b = (BigInteger)7;
            var a = BigInteger.Zero;
            var Gx = BigInteger.Parse("79BE667EF9DCBBAC55A06295CE870B07029BFCDB2DCE28D959F2815B16F81798", NumberStyles.HexNumber);
            var Gy = BigInteger.Parse("483ADA7726A3C4655DA4FBFC0E1108A8FD17B448A68554199C47D08FFB10D4B8", NumberStyles.HexNumber);

            CurveFp curve256 = new CurveFp(p, a, b);
            Point generator256 = new Point(curve256, Gx, Gy);

            var secret = BigInteger.Parse(privateKey, NumberStyles.HexNumber);
            var pubkeyPoint = generator256 * secret;
            return pubkeyPoint.X.ToString("X") + pubkeyPoint.Y.ToString("X");
        }

        class Point
        {
            public static readonly Point INFINITY = new Point(null, default(BigInteger), default(BigInteger));
            public CurveFp Curve { get; private set; }
            public BigInteger X { get; private set; }
            public BigInteger Y { get; private set; }

            public Point(CurveFp curve, BigInteger x, BigInteger y)
            {
                this.Curve = curve;
                this.X = x;
                this.Y = y;
            }
            public Point Double()
            {
                if (this == INFINITY)
                    return INFINITY;

                BigInteger p = this.Curve.p;
                BigInteger a = this.Curve.a;
                BigInteger l = ((3 * this.X * this.X + a) * InverseMod(2 * this.Y, p)) % p;
                BigInteger x3 = (l * l - 2 * this.X) % p;
                BigInteger y3 = (l * (this.X - x3) - this.Y) % p;
                return new Point(this.Curve, x3, y3);
            }
            public override string ToString()
            {
                if (this == INFINITY)
                    return "infinity";
                return string.Format("({0},{1})", this.X, this.Y);
            }
            public static Point operator +(Point left, Point right)
            {
                if (right == INFINITY)
                    return left;
                if (left == INFINITY)
                    return right;
                if (left.X == right.X)
                {
                    if ((left.Y + right.Y) % left.Curve.p == 0)
                        return INFINITY;
                    else
                        return left.Double();
                }

                var p = left.Curve.p;
                var l = ((right.Y - left.Y) * InverseMod(right.X - left.X, p)) % p;
                var x3 = (l * l - left.X - right.X) % p;
                var y3 = (l * (left.X - x3) - left.Y) % p;
                return new Point(left.Curve, x3, y3);
            }
            public static Point operator *(Point left, BigInteger right)
            {
                var e = right;
                if (e == 0 || left == INFINITY)
                    return INFINITY;
                var e3 = 3 * e;
                var negativeLeft = new Point(left.Curve, left.X, -left.Y);
                var i = LeftmostBit(e3) / 2;
                var result = left;
                while (i > 1)
                {
                    result = result.Double();
                    if ((e3 & i) != 0 && (e & i) == 0)
                        result += left;
                    if ((e3 & i) == 0 && (e & i) != 0)
                        result += negativeLeft;
                    i /= 2;
                }
                return result;
            }

            private static BigInteger LeftmostBit(BigInteger x)
            {
                BigInteger result = 1;
                while (result <= x)
                    result = 2 * result;
                return result / 2;
            }
            private static BigInteger InverseMod(BigInteger a, BigInteger m)
            {
                while (a < 0) a += m;
                if (a < 0 || m <= a)
                    a = a % m;
                BigInteger c = a;
                BigInteger d = m;

                BigInteger uc = 1;
                BigInteger vc = 0;
                BigInteger ud = 0;
                BigInteger vd = 1;

                while (c != 0)
                {
                    BigInteger r;
                    //q, c, d = divmod( d, c ) + ( c, );
                    var q = BigInteger.DivRem(d, c, out r);
                    d = c;
                    c = r;

                    //uc, vc, ud, vd = ud - q*uc, vd - q*vc, uc, vc;
                    var uct = uc;
                    var vct = vc;
                    var udt = ud;
                    var vdt = vd;
                    uc = udt - q * uct;
                    vc = vdt - q * vct;
                    ud = uct;
                    vd = vct;
                }
                if (ud > 0) return ud;
                else return ud + m;
            }
        }

        class CurveFp
        {
            public BigInteger p { get; private set; }
            public BigInteger a { get; private set; }
            public BigInteger b { get; private set; }
            public CurveFp(BigInteger p, BigInteger a, BigInteger b)
            {
                this.p = p;
                this.a = a;
                this.b = b;
            }
        }
    }
}
