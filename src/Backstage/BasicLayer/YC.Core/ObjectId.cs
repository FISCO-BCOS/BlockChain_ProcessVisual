/* Copyright 2010-2017 MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Threading;

namespace YC.Core
{
    /// <summary>
    /// Represents an ObjectId (see also BsonObjectId).
    /// </summary>
#if NET45
    [Serializable]
#endif
    public struct ObjectId : IComparable<ObjectId>, IEquatable<ObjectId>, IConvertible
    {
        // private static fields
        private static readonly ObjectId __emptyInstance = default(ObjectId);
        private static readonly int __staticMachine = (GetMachineHash() + GetAppDomainId()) & 0x00ffffff;
        private static readonly short __staticPid = GetPid();
        private static int __staticIncrement = (new Random()).Next();

        // private fields
        private readonly int _a;
        private readonly int _b;
        private readonly int _c;

        // constructors
        /// <summary>
        /// Initializes a new instance of the ObjectId class.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        public ObjectId(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }
            if (bytes.Length != 12)
            {
                throw new ArgumentException("Byte array must be 12 bytes long", "bytes");
            }

            FromByteArray(bytes, 0, out _a, out _b, out _c);
        }

        /// <summary>
        /// Initializes a new instance of the ObjectId class.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="index">The index into the byte array where the ObjectId starts.</param>
        internal ObjectId(byte[] bytes, int index)
        {
            FromByteArray(bytes, index, out _a, out _b, out _c);
        }

        /// <summary>
        /// Initializes a new instance of the ObjectId class.
        /// </summary>
        /// <param name="timestamp">The timestamp (expressed as a DateTime).</param>
        /// <param name="machine">The machine hash.</param>
        /// <param name="pid">The PID.</param>
        /// <param name="increment">The increment.</param>
        public ObjectId(DateTime timestamp, int machine, short pid, int increment)
            : this(GetTimestampFromDateTime(timestamp), machine, pid, increment)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ObjectId class.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="machine">The machine hash.</param>
        /// <param name="pid">The PID.</param>
        /// <param name="increment">The increment.</param>
        public ObjectId(int timestamp, int machine, short pid, int increment)
        {
            if ((machine & 0xff000000) != 0)
            {
                throw new ArgumentOutOfRangeException("machine", "The machine value must be between 0 and 16777215 (it must fit in 3 bytes).");
            }
            if ((increment & 0xff000000) != 0)
            {
                throw new ArgumentOutOfRangeException("increment", "The increment value must be between 0 and 16777215 (it must fit in 3 bytes).");
            }

            _a = timestamp;
            _b = (machine << 8) | (((int)pid >> 8) & 0xff);
            _c = ((int)pid << 24) | increment;
        }

        /// <summary>
        /// Initializes a new instance of the ObjectId class.
        /// </summary>
        /// <param name="value">The value.</param>
        public ObjectId(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            var bytes = BsonUtils.ParseHexString(value);
            FromByteArray(bytes, 0, out _a, out _b, out _c);
        }

        // public static properties
        /// <summary>
        /// Gets an instance of ObjectId where the value is empty.
        /// </summary>
        public static ObjectId Empty
        {
            get { return __emptyInstance; }
        }

        // public properties
        /// <summary>
        /// Gets the timestamp.
        /// </summary>
        public int Timestamp
        {
            get { return _a; }
        }

        /// <summary>
        /// Gets the machine.
        /// </summary>
        public int Machine
        {
            get { return (_b >> 8) & 0xffffff; }
        }

        /// <summary>
        /// Gets the PID.
        /// </summary>
        public short Pid
        {
            get { return (short)(((_b << 8) & 0xff00) | ((_c >> 24) & 0x00ff)); }
        }

        /// <summary>
        /// Gets the increment.
        /// </summary>
        public int Increment
        {
            get { return _c & 0xffffff; }
        }

        /// <summary>
        /// Gets the creation time (derived from the timestamp).
        /// </summary>
        public DateTime CreationTime
        {
            get { return BsonConstants.UnixEpoch.AddSeconds(Timestamp); }
        }

        // public operators
        /// <summary>
        /// Compares two ObjectIds.
        /// </summary>
        /// <param name="lhs">The first ObjectId.</param>
        /// <param name="rhs">The other ObjectId</param>
        /// <returns>True if the first ObjectId is less than the second ObjectId.</returns>
        public static bool operator <(ObjectId lhs, ObjectId rhs)
        {
            return lhs.CompareTo(rhs) < 0;
        }

        /// <summary>
        /// Compares two ObjectIds.
        /// </summary>
        /// <param name="lhs">The first ObjectId.</param>
        /// <param name="rhs">The other ObjectId</param>
        /// <returns>True if the first ObjectId is less than or equal to the second ObjectId.</returns>
        public static bool operator <=(ObjectId lhs, ObjectId rhs)
        {
            return lhs.CompareTo(rhs) <= 0;
        }

        /// <summary>
        /// Compares two ObjectIds.
        /// </summary>
        /// <param name="lhs">The first ObjectId.</param>
        /// <param name="rhs">The other ObjectId.</param>
        /// <returns>True if the two ObjectIds are equal.</returns>
        public static bool operator ==(ObjectId lhs, ObjectId rhs)
        {
            return lhs.Equals(rhs);
        }

        /// <summary>
        /// Compares two ObjectIds.
        /// </summary>
        /// <param name="lhs">The first ObjectId.</param>
        /// <param name="rhs">The other ObjectId.</param>
        /// <returns>True if the two ObjectIds are not equal.</returns>
        public static bool operator !=(ObjectId lhs, ObjectId rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>
        /// Compares two ObjectIds.
        /// </summary>
        /// <param name="lhs">The first ObjectId.</param>
        /// <param name="rhs">The other ObjectId</param>
        /// <returns>True if the first ObjectId is greather than or equal to the second ObjectId.</returns>
        public static bool operator >=(ObjectId lhs, ObjectId rhs)
        {
            return lhs.CompareTo(rhs) >= 0;
        }

        /// <summary>
        /// Compares two ObjectIds.
        /// </summary>
        /// <param name="lhs">The first ObjectId.</param>
        /// <param name="rhs">The other ObjectId</param>
        /// <returns>True if the first ObjectId is greather than the second ObjectId.</returns>
        public static bool operator >(ObjectId lhs, ObjectId rhs)
        {
            return lhs.CompareTo(rhs) > 0;
        }

        // public static methods
        /// <summary>
        /// Generates a new ObjectId with a unique value.
        /// </summary>
        /// <returns>An ObjectId.</returns>
        public static ObjectId GenerateNewId()
        {
            return GenerateNewId(GetTimestampFromDateTime(DateTime.UtcNow));
        }

        public static string Get()
        {
            return GenerateNewId().ToString();
        }

        /// <summary>
        /// Generates a new ObjectId with a unique value (with the timestamp component based on a given DateTime).
        /// </summary>
        /// <param name="timestamp">The timestamp component (expressed as a DateTime).</param>
        /// <returns>An ObjectId.</returns>
        public static ObjectId GenerateNewId(DateTime timestamp)
        {
            return GenerateNewId(GetTimestampFromDateTime(timestamp));
        }

        /// <summary>
        /// Generates a new ObjectId with a unique value (with the given timestamp).
        /// </summary>
        /// <param name="timestamp">The timestamp component.</param>
        /// <returns>An ObjectId.</returns>
        public static ObjectId GenerateNewId(int timestamp)
        {
            int increment = Interlocked.Increment(ref __staticIncrement) & 0x00ffffff; // only use low order 3 bytes
            return new ObjectId(timestamp, __staticMachine, __staticPid, increment);
        }

        /// <summary>
        /// Packs the components of an ObjectId into a byte array.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="machine">The machine hash.</param>
        /// <param name="pid">The PID.</param>
        /// <param name="increment">The increment.</param>
        /// <returns>A byte array.</returns>
        public static byte[] Pack(int timestamp, int machine, short pid, int increment)
        {
            if ((machine & 0xff000000) != 0)
            {
                throw new ArgumentOutOfRangeException("machine", "The machine value must be between 0 and 16777215 (it must fit in 3 bytes).");
            }
            if ((increment & 0xff000000) != 0)
            {
                throw new ArgumentOutOfRangeException("increment", "The increment value must be between 0 and 16777215 (it must fit in 3 bytes).");
            }

            byte[] bytes = new byte[12];
            bytes[0] = (byte)(timestamp >> 24);
            bytes[1] = (byte)(timestamp >> 16);
            bytes[2] = (byte)(timestamp >> 8);
            bytes[3] = (byte)(timestamp);
            bytes[4] = (byte)(machine >> 16);
            bytes[5] = (byte)(machine >> 8);
            bytes[6] = (byte)(machine);
            bytes[7] = (byte)(pid >> 8);
            bytes[8] = (byte)(pid);
            bytes[9] = (byte)(increment >> 16);
            bytes[10] = (byte)(increment >> 8);
            bytes[11] = (byte)(increment);
            return bytes;
        }

        /// <summary>
        /// Parses a string and creates a new ObjectId.
        /// </summary>
        /// <param name="s">The string value.</param>
        /// <returns>A ObjectId.</returns>
        public static ObjectId Parse(string s)
        {
            if (s == null)
            {
                throw new ArgumentNullException("s");
            }

            ObjectId objectId;
            if (TryParse(s, out objectId))
            {
                return objectId;
            }
            else
            {
                var message = string.Format("'{0}' is not a valid 24 digit hex string.", s);
                throw new FormatException(message);
            }
        }

        /// <summary>
        /// Tries to parse a string and create a new ObjectId.
        /// </summary>
        /// <param name="s">The string value.</param>
        /// <param name="objectId">The new ObjectId.</param>
        /// <returns>True if the string was parsed successfully.</returns>
        public static bool TryParse(string s, out ObjectId objectId)
        {
            // don't throw ArgumentNullException if s is null
            if (s != null && s.Length == 24)
            {
                byte[] bytes;
                if (BsonUtils.TryParseHexString(s, out bytes))
                {
                    objectId = new ObjectId(bytes);
                    return true;
                }
            }

            objectId = default(ObjectId);
            return false;
        }

        /// <summary>
        /// Unpacks a byte array into the components of an ObjectId.
        /// </summary>
        /// <param name="bytes">A byte array.</param>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="machine">The machine hash.</param>
        /// <param name="pid">The PID.</param>
        /// <param name="increment">The increment.</param>
        public static void Unpack(byte[] bytes, out int timestamp, out int machine, out short pid, out int increment)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }
            if (bytes.Length != 12)
            {
                throw new ArgumentOutOfRangeException("bytes", "Byte array must be 12 bytes long.");
            }

            timestamp = (bytes[0] << 24) + (bytes[1] << 16) + (bytes[2] << 8) + bytes[3];
            machine = (bytes[4] << 16) + (bytes[5] << 8) + bytes[6];
            pid = (short)((bytes[7] << 8) + bytes[8]);
            increment = (bytes[9] << 16) + (bytes[10] << 8) + bytes[11];
        }

        // private static methods
        private static int GetAppDomainId()
        {
#if NETSTANDARD1_5 || NETSTANDARD1_6
            return 1;
#else
            return AppDomain.CurrentDomain.Id;
#endif
        }

        /// <summary>
        /// Gets the current process id.  This method exists because of how CAS operates on the call stack, checking
        /// for permissions before executing the method.  Hence, if we inlined this call, the calling method would not execute
        /// before throwing an exception requiring the try/catch at an even higher level that we don't necessarily control.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int GetCurrentProcessId()
        {
            return Process.GetCurrentProcess().Id;
        }

        private static int GetMachineHash()
        {
            // use instead of Dns.HostName so it will work offline
            var machineName = GetMachineName();
            return 0x00ffffff & machineName.GetHashCode(); // use first 3 bytes of hash
        }

        private static string GetMachineName()
        {
            return Environment.MachineName;
        }

        private static short GetPid()
        {
            try
            {
                return (short)GetCurrentProcessId(); // use low order two bytes only
            }
            catch (SecurityException)
            {
                return 0;
            }
        }

        private static int GetTimestampFromDateTime(DateTime timestamp)
        {
            var secondsSinceEpoch = (long)Math.Floor((BsonUtils.ToUniversalTime(timestamp) - BsonConstants.UnixEpoch).TotalSeconds);
            if (secondsSinceEpoch < int.MinValue || secondsSinceEpoch > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException("timestamp");
            }
            return (int)secondsSinceEpoch;
        }

        private static void FromByteArray(byte[] bytes, int offset, out int a, out int b, out int c)
        {
            a = (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
            b = (bytes[offset + 4] << 24) | (bytes[offset + 5] << 16) | (bytes[offset + 6] << 8) | bytes[offset + 7];
            c = (bytes[offset + 8] << 24) | (bytes[offset + 9] << 16) | (bytes[offset + 10] << 8) | bytes[offset + 11];
        }

        // public methods
        /// <summary>
        /// Compares this ObjectId to another ObjectId.
        /// </summary>
        /// <param name="other">The other ObjectId.</param>
        /// <returns>A 32-bit signed integer that indicates whether this ObjectId is less than, equal to, or greather than the other.</returns>
        public int CompareTo(ObjectId other)
        {
            int result = ((uint)_a).CompareTo((uint)other._a);
            if (result != 0) { return result; }
            result = ((uint)_b).CompareTo((uint)other._b);
            if (result != 0) { return result; }
            return ((uint)_c).CompareTo((uint)other._c);
        }

        /// <summary>
        /// Compares this ObjectId to another ObjectId.
        /// </summary>
        /// <param name="rhs">The other ObjectId.</param>
        /// <returns>True if the two ObjectIds are equal.</returns>
        public bool Equals(ObjectId rhs)
        {
            return
                _a == rhs._a &&
                _b == rhs._b &&
                _c == rhs._c;
        }

        /// <summary>
        /// Compares this ObjectId to another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if the other object is an ObjectId and equal to this one.</returns>
        public override bool Equals(object obj)
        {
            if (obj is ObjectId)
            {
                return Equals((ObjectId)obj);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            int hash = 17;
            hash = 37 * hash + _a.GetHashCode();
            hash = 37 * hash + _b.GetHashCode();
            hash = 37 * hash + _c.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Converts the ObjectId to a byte array.
        /// </summary>
        /// <returns>A byte array.</returns>
        public byte[] ToByteArray()
        {
            var bytes = new byte[12];
            ToByteArray(bytes, 0);
            return bytes;
        }

        /// <summary>
        /// Converts the ObjectId to a byte array.
        /// </summary>
        /// <param name="destination">The destination.</param>
        /// <param name="offset">The offset.</param>
        public void ToByteArray(byte[] destination, int offset)
        {
            if (destination == null)
            {
                throw new ArgumentNullException("destination");
            }
            if (offset + 12 > destination.Length)
            {
                throw new ArgumentException("Not enough room in destination buffer.", "offset");
            }

            destination[offset + 0] = (byte)(_a >> 24);
            destination[offset + 1] = (byte)(_a >> 16);
            destination[offset + 2] = (byte)(_a >> 8);
            destination[offset + 3] = (byte)(_a);
            destination[offset + 4] = (byte)(_b >> 24);
            destination[offset + 5] = (byte)(_b >> 16);
            destination[offset + 6] = (byte)(_b >> 8);
            destination[offset + 7] = (byte)(_b);
            destination[offset + 8] = (byte)(_c >> 24);
            destination[offset + 9] = (byte)(_c >> 16);
            destination[offset + 10] = (byte)(_c >> 8);
            destination[offset + 11] = (byte)(_c);
        }

        /// <summary>
        /// Returns a string representation of the value.
        /// </summary>
        /// <returns>A string representation of the value.</returns>
        public override string ToString()
        {
            var c = new char[24];
            c[0] = BsonUtils.ToHexChar((_a >> 28) & 0x0f);
            c[1] = BsonUtils.ToHexChar((_a >> 24) & 0x0f);
            c[2] = BsonUtils.ToHexChar((_a >> 20) & 0x0f);
            c[3] = BsonUtils.ToHexChar((_a >> 16) & 0x0f);
            c[4] = BsonUtils.ToHexChar((_a >> 12) & 0x0f);
            c[5] = BsonUtils.ToHexChar((_a >> 8) & 0x0f);
            c[6] = BsonUtils.ToHexChar((_a >> 4) & 0x0f);
            c[7] = BsonUtils.ToHexChar(_a & 0x0f);
            c[8] = BsonUtils.ToHexChar((_b >> 28) & 0x0f);
            c[9] = BsonUtils.ToHexChar((_b >> 24) & 0x0f);
            c[10] = BsonUtils.ToHexChar((_b >> 20) & 0x0f);
            c[11] = BsonUtils.ToHexChar((_b >> 16) & 0x0f);
            c[12] = BsonUtils.ToHexChar((_b >> 12) & 0x0f);
            c[13] = BsonUtils.ToHexChar((_b >> 8) & 0x0f);
            c[14] = BsonUtils.ToHexChar((_b >> 4) & 0x0f);
            c[15] = BsonUtils.ToHexChar(_b & 0x0f);
            c[16] = BsonUtils.ToHexChar((_c >> 28) & 0x0f);
            c[17] = BsonUtils.ToHexChar((_c >> 24) & 0x0f);
            c[18] = BsonUtils.ToHexChar((_c >> 20) & 0x0f);
            c[19] = BsonUtils.ToHexChar((_c >> 16) & 0x0f);
            c[20] = BsonUtils.ToHexChar((_c >> 12) & 0x0f);
            c[21] = BsonUtils.ToHexChar((_c >> 8) & 0x0f);
            c[22] = BsonUtils.ToHexChar((_c >> 4) & 0x0f);
            c[23] = BsonUtils.ToHexChar(_c & 0x0f);
            return new string(c);
        }

        // explicit IConvertible implementation
        TypeCode IConvertible.GetTypeCode()
        {
            return TypeCode.Object;
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        byte IConvertible.ToByte(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        double IConvertible.ToDouble(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        short IConvertible.ToInt16(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        int IConvertible.ToInt32(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        long IConvertible.ToInt64(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        float IConvertible.ToSingle(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        string IConvertible.ToString(IFormatProvider provider)
        {
            return ToString();
        }

        object IConvertible.ToType(Type conversionType, IFormatProvider provider)
        {
            //switch (Type.GetTypeCode(conversionType))
            //{
            //    case TypeCode.String:
            //        return ((IConvertible)this).ToString(provider);
            //    case TypeCode.Object:
            //        if (conversionType == typeof(object) || conversionType == typeof(ObjectId))
            //        {
            //            return this;
            //        }
            //        if (conversionType == typeof(BsonObjectId))
            //        {
            //            return new BsonObjectId(this);
            //        }
            //        if (conversionType == typeof(BsonString))
            //        {
            //            return new BsonString(((IConvertible)this).ToString(provider));
            //        }
            //        break;
            //}

            throw new InvalidCastException();
        }

        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }
    }


    /// <summary>
    /// A static class containing BSON constants.
    /// </summary>
    public static class BsonConstants
    {
        // private static fields
        private static readonly long __dateTimeMaxValueMillisecondsSinceEpoch;
        private static readonly long __dateTimeMinValueMillisecondsSinceEpoch;
        private static readonly DateTime __unixEpoch;

        // static constructor
        static BsonConstants()
        {
            // unixEpoch has to be initialized first
            __unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            __dateTimeMaxValueMillisecondsSinceEpoch = (DateTime.MaxValue - __unixEpoch).Ticks / 10000;
            __dateTimeMinValueMillisecondsSinceEpoch = (DateTime.MinValue - __unixEpoch).Ticks / 10000;
        }

        // public static properties
        /// <summary>
        /// Gets the number of milliseconds since the Unix epoch for DateTime.MaxValue.
        /// </summary>
        public static long DateTimeMaxValueMillisecondsSinceEpoch
        {
            get { return __dateTimeMaxValueMillisecondsSinceEpoch; }
        }

        /// <summary>
        /// Gets the number of milliseconds since the Unix epoch for DateTime.MinValue.
        /// </summary>
        public static long DateTimeMinValueMillisecondsSinceEpoch
        {
            get { return __dateTimeMinValueMillisecondsSinceEpoch; }
        }

        /// <summary>
        /// Gets the Unix Epoch for BSON DateTimes (1970-01-01).
        /// </summary>
        public static DateTime UnixEpoch { get { return __unixEpoch; } }
    }

    /// <summary>
    /// A static class containing BSON utility methods.
    /// </summary>
    public static class BsonUtils
    {
        // public static methods
        /// <summary>
        /// Gets a friendly class name suitable for use in error messages.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>A friendly class name.</returns>
        public static string GetFriendlyTypeName(Type type)
        {
            var typeInfo = type.GetTypeInfo();
            if (!typeInfo.IsGenericType)
            {
                return type.Name;
            }

            var sb = new StringBuilder();
            sb.AppendFormat("{0}<", Regex.Replace(type.Name, @"\`\d+$", ""));
            foreach (var typeParameter in type.GetTypeInfo().GetGenericArguments())
            {
                sb.AppendFormat("{0}, ", GetFriendlyTypeName(typeParameter));
            }
            sb.Remove(sb.Length - 2, 2);
            sb.Append(">");
            return sb.ToString();
        }

        /// <summary>
        /// Parses a hex string into its equivalent byte array.
        /// </summary>
        /// <param name="s">The hex string to parse.</param>
        /// <returns>The byte equivalent of the hex string.</returns>
        public static byte[] ParseHexString(string s)
        {
            if (s == null)
            {
                //throw new ArgumentNullException(nameof(s));
            }

            byte[] bytes;
            if (!TryParseHexString(s, out bytes))
            {
                throw new FormatException("String should contain only hexadecimal digits.");
            }

            return bytes;
        }

        /// <summary>
        /// Converts from number of milliseconds since Unix epoch to DateTime.
        /// </summary>
        /// <param name="millisecondsSinceEpoch">The number of milliseconds since Unix epoch.</param>
        /// <returns>A DateTime.</returns>
        public static DateTime ToDateTimeFromMillisecondsSinceEpoch(long millisecondsSinceEpoch)
        {
            if (millisecondsSinceEpoch < BsonConstants.DateTimeMinValueMillisecondsSinceEpoch ||
                millisecondsSinceEpoch > BsonConstants.DateTimeMaxValueMillisecondsSinceEpoch)
            {
                var message = string.Format(
                    "The value {0} for the BsonDateTime MillisecondsSinceEpoch is outside the" +
                    "range that can be converted to a .NET DateTime.",
                    millisecondsSinceEpoch);
                throw new ArgumentOutOfRangeException("millisecondsSinceEpoch", message);
            }

            // MaxValue has to be handled specially to avoid rounding errors
            if (millisecondsSinceEpoch == BsonConstants.DateTimeMaxValueMillisecondsSinceEpoch)
            {
                return DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
            }
            else
            {
                return BsonConstants.UnixEpoch.AddTicks(millisecondsSinceEpoch * 10000);
            }
        }

        /// <summary>
        /// Converts a value to a hex character.
        /// </summary>
        /// <param name="value">The value (assumed to be between 0 and 15).</param>
        /// <returns>The hex character.</returns>
        public static char ToHexChar(int value)
        {
            return (char)(value + (value < 10 ? '0' : 'a' - 10));
        }

        /// <summary>
        /// Converts a byte array to a hex string.
        /// </summary>
        /// <param name="bytes">The byte array.</param>
        /// <returns>A hex string.</returns>
        public static string ToHexString(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }

            var length = bytes.Length;
            var c = new char[length * 2];

            for (int i = 0, j = 0; i < length; i++)
            {
                var b = bytes[i];
                c[j++] = ToHexChar(b >> 4);
                c[j++] = ToHexChar(b & 0x0f);
            }

            return new string(c);
        }

        /// <summary>
        /// Converts a DateTime to local time (with special handling for MinValue and MaxValue).
        /// </summary>
        /// <param name="dateTime">A DateTime.</param>
        /// <returns>The DateTime in local time.</returns>
        public static DateTime ToLocalTime(DateTime dateTime)
        {
            if (dateTime == DateTime.MinValue)
            {
                return DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Local);
            }
            else if (dateTime == DateTime.MaxValue)
            {
                return DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Local);
            }
            else
            {
                return dateTime.ToLocalTime();
            }
        }

        /// <summary>
        /// Converts a DateTime to number of milliseconds since Unix epoch.
        /// </summary>
        /// <param name="dateTime">A DateTime.</param>
        /// <returns>Number of seconds since Unix epoch.</returns>
        public static long ToMillisecondsSinceEpoch(DateTime dateTime)
        {
            var utcDateTime = ToUniversalTime(dateTime);
            return (utcDateTime - BsonConstants.UnixEpoch).Ticks / 10000;
        }

        /// <summary>
        /// Converts a DateTime to UTC (with special handling for MinValue and MaxValue).
        /// </summary>
        /// <param name="dateTime">A DateTime.</param>
        /// <returns>The DateTime in UTC.</returns>
        public static DateTime ToUniversalTime(DateTime dateTime)
        {
            if (dateTime == DateTime.MinValue)
            {
                return DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
            }
            else if (dateTime == DateTime.MaxValue)
            {
                return DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
            }
            else
            {
                return dateTime.ToUniversalTime();
            }
        }

        /// <summary>
        /// Tries to parse a hex string to a byte array.
        /// </summary>
        /// <param name="s">The hex string.</param>
        /// <param name="bytes">A byte array.</param>
        /// <returns>True if the hex string was successfully parsed.</returns>
        public static bool TryParseHexString(string s, out byte[] bytes)
        {
            bytes = null;

            if (s == null)
            {
                return false;
            }

            var buffer = new byte[(s.Length + 1) / 2];

            var i = 0;
            var j = 0;

            if ((s.Length % 2) == 1)
            {
                // if s has an odd length assume an implied leading "0"
                int y;
                if (!TryParseHexChar(s[i++], out y))
                {
                    return false;
                }
                buffer[j++] = (byte)y;
            }

            while (i < s.Length)
            {
                int x, y;
                if (!TryParseHexChar(s[i++], out x))
                {
                    return false;
                }
                if (!TryParseHexChar(s[i++], out y))
                {
                    return false;
                }
                buffer[j++] = (byte)((x << 4) | y);
            }

            bytes = buffer;
            return true;
        }

        // private static methods
        private static bool TryParseHexChar(char c, out int value)
        {
            if (c >= '0' && c <= '9')
            {
                value = c - '0';
                return true;
            }

            if (c >= 'a' && c <= 'f')
            {
                value = 10 + (c - 'a');
                return true;
            }

            if (c >= 'A' && c <= 'F')
            {
                value = 10 + (c - 'A');
                return true;
            }

            value = 0;
            return false;
        }
    }
}
