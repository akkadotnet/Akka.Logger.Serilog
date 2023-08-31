// -----------------------------------------------------------------------
//   <copyright file="TestData.cs" company="Akka.NET Project">
//     Copyright (C) 2013-2023 Akka.NET project <https://github.com/akkadotnet/akka.net>
//   </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;

namespace Akka.Logger.Serilog.Tests.Generator
{
    internal static class TestData
    {
        public const bool Boolean = true;
        public const byte Byte = 42;
        public const sbyte SByte = -42;
        public const char Char = 'A';
        public const decimal Decimal = 3.1415M;
        public const double Double = 3.1415;
        public const float Float = 3.1415F;
        public const int Int = -42;
        public const uint UInt = 42;
        public const long Long = -42L;
        public const ulong ULong = 42;
        public const short Short = -42;
        public const ushort UShort = 42;
        public const string String = "Fourty Two";
        public const EnumInstance Enum = EnumInstance.One;
        
        public static readonly bool[] BooleanArray = { false, true, false };
        public static readonly byte[] ByteArray = { 1, 2, 3 };
        public static readonly sbyte[] SByteArray = { -1, 2, -3 };
        public static readonly char[] CharArray = { '1', '2', '3' };
        public static readonly decimal[] DecimalArray = { 1M, -2M, 3M };
        public static readonly double[] DoubleArray = { 1.0, -2.0, 3.0 };
        public static readonly float[] FloatArray = { 1F, -2F, 3F };
        public static readonly int[] IntArray = { 1, -2, 3 };
        public static readonly uint[] UIntArray = { 1, 2, 3 };
        public static readonly long[] LongArray = { 1L, -2L, 3L };
        public static readonly ulong[] ULongArray = { 1L, 2L, 3L };
        public static readonly short[] ShortArray = { 1, -2, 3 };
        public static readonly ushort[] UShortArray = { 1, 2, 3 };
        public static readonly string[] StringArray = { "One", "Two", "Three" };
        public static readonly EnumInstance[] EnumArray = { EnumInstance.One, EnumInstance.Two, EnumInstance.Three };

        public static readonly List<bool> BooleanList = new List<bool>(BooleanArray);
        public static readonly List<byte> ByteList = new List<byte>(ByteArray);
        public static readonly List<sbyte> SByteList = new List<sbyte>(SByteArray);
        public static readonly List<char> CharList = new List<char>(CharArray);
        public static readonly List<decimal> DecimalList = new List<decimal>(DecimalArray);
        public static readonly List<double> DoubleList = new List<double>(DoubleArray);
        public static readonly List<float> FloatList = new List<float>(FloatArray);
        public static readonly List<int> IntList = new List<int>(IntArray);
        public static readonly List<uint> UIntList = new List<uint>(UIntArray);
        public static readonly List<long> LongList = new List<long>(LongArray);
        public static readonly List<ulong> ULongList = new List<ulong>(ULongArray);
        public static readonly List<short> ShortList = new List<short>(ShortArray);
        public static readonly List<ushort> UShortList = new List<ushort>(UShortArray);
        public static readonly List<string> StringList = new List<string>(StringArray);
        public static readonly List<EnumInstance> EnumList = new List<EnumInstance>(EnumArray);

        public static readonly Dictionary<string, object> PrimitiveDictionary = new Dictionary<string, object>
        {
            [nameof(Boolean)] = Boolean,
            [nameof(Byte)] = Byte,
            [nameof(SByte)] = SByte,
            [nameof(Char)] = Char,
            [nameof(Decimal)] = Decimal,
            [nameof(Double)] = Double,
            [nameof(Float)] = Float,
            [nameof(Int)] = Int,
            [nameof(UInt)] = UInt,
            [nameof(Long)] = Long,
            [nameof(ULong)] = ULong,
            [nameof(Short)] = Short,
            [nameof(UShort)] = UShort,
            [nameof(String)] = String,
            [nameof(Enum)] = Enum,
        };

        public static readonly Dictionary<string, object> ArrayDictionary = new Dictionary<string, object>
        {
            [nameof(BooleanArray)] = BooleanArray,
            [nameof(ByteArray)] = ByteArray,
            [nameof(SByteArray)] = SByteArray,
            [nameof(CharArray)] = CharArray,
            [nameof(DecimalArray)] = DecimalArray,
            [nameof(DoubleArray)] = DoubleArray,
            [nameof(FloatArray)] = FloatArray,
            [nameof(IntArray)] = IntArray,
            [nameof(UIntArray)] = UIntArray,
            [nameof(LongArray)] = LongArray,
            [nameof(ULongArray)] = ULongArray,
            [nameof(ShortArray)] = ShortArray,
            [nameof(UShortArray)] = UShortArray,
            [nameof(StringArray)] = StringArray,
            [nameof(EnumArray)] = EnumArray,
        };

        public static readonly Dictionary<string, object> ListDictionary = new Dictionary<string, object>
        {
            [nameof(BooleanList)] = BooleanList,
            [nameof(ByteList)] = ByteList,
            [nameof(SByteList)] = SByteList,
            [nameof(CharList)] = CharList,
            [nameof(DecimalList)] = DecimalList,
            [nameof(DoubleList)] = DoubleList,
            [nameof(FloatList)] = FloatList,
            [nameof(IntList)] = IntList,
            [nameof(UIntList)] = UIntList,
            [nameof(LongList)] = LongList,
            [nameof(ULongList)] = ULongList,
            [nameof(ShortList)] = ShortList,
            [nameof(UShortList)] = UShortList,
            [nameof(StringList)] = StringList,
            [nameof(EnumList)] = EnumList,
        };

        public static readonly PlainClassInstance PlainClassInstance = new PlainClassInstance
        {
            Boolean = Boolean,
            Byte = Byte,
            Sbyte = SByte,
            Char = Char,
            
            Decimal = Decimal,
            Double = Double,
            Float = Float,
            Int = Int,
            
            UInt = UInt,
            Long = Long,
            ULong = ULong,
            Short = Short,
            
            UShort = UShort,
            String = String,
            Enum = Enum
        };

        public static readonly string[] MessageFormats =
        {
            $"{nameof(Boolean)}: {{Boolean}}", 
            $"{nameof(Byte)}: {{Byte}}", 
            $"{nameof(SByte)}: {{SByte}}",
            $"{nameof(Char)}: {{Char}}",
            
            $"{nameof(Decimal)}: {{Decimal}}",
            $"{nameof(Double)}: {{Double}}",
            $"{nameof(Float)}: {{Float}}",
            $"{nameof(Int)}: {{Int}}",
            
            $"{nameof(UInt)}: {{UInt}}",
            $"{nameof(Long)}: {{Long}}",
            $"{nameof(ULong)}: {{ULong}}",
            $"{nameof(Short)}: {{Short}}",
            
            $"{nameof(UShort)}: {{UShort}}", 
            $"{nameof(String)}: {{String}}",
            $"{nameof(Enum)}: {{Enum}}",
            
            
            $"{nameof(BooleanArray)}: {{BooleanArray}}",
            $"{nameof(ByteArray)}: {{ByteArray}}",
            $"{nameof(SByteArray)}: {{SByteArray}}",
            $"{nameof(CharArray)}: {{CharArray}}",
            
            $"{nameof(DecimalArray)}: {{DecimalArray}}",
            $"{nameof(DoubleArray)}: {{DoubleArray}}",
            $"{nameof(FloatArray)}: {{FloatArray}}",
            $"{nameof(IntArray)}: {{IntArray}}",
            
            $"{nameof(UIntArray)}: {{UIntArray}}",
            $"{nameof(LongArray)}: {{LongArray}}",
            $"{nameof(ULongArray)}: {{ULongArray}}",
            $"{nameof(ShortArray)}: {{ShortArray}}",
            
            $"{nameof(UShortArray)}: {{UShortArray}}",
            $"{nameof(StringArray)}: {{StringArray}}",
            $"{nameof(EnumArray)}: {{EnumArray}}",
            
            
            $"{nameof(BooleanList)}: {{BooleanList}}",
            $"{nameof(ByteList)}: {{ByteList}}",
            $"{nameof(SByteList)}: {{SByteList}}",
            $"{nameof(CharList)}: {{CharList}}",
            
            $"{nameof(DecimalList)}: {{DecimalList}}",
            $"{nameof(DoubleList)}: {{DoubleList}}",
            $"{nameof(FloatList)}: {{FloatList}}",
            $"{nameof(IntList)}: {{IntList}}",
            
            $"{nameof(UIntList)}: {{UIntList}}",
            $"{nameof(LongList)}: {{LongList}}",
            $"{nameof(ULongList)}: {{ULongList}}",
            $"{nameof(ShortList)}: {{ShortList}}",
            
            $"{nameof(UShortList)}: {{UShortList}}",
            $"{nameof(StringList)}: {{StringList}}",
            $"{nameof(EnumList)}: {{EnumList}}",

            $"{nameof(PrimitiveDictionary)}: {{PrimitiveDictionary}}",
            $"{nameof(ArrayDictionary)}: {{ArrayDictionary}}",
            $"{nameof(ListDictionary)}: {{ListDictionary}}",
            
            $"{nameof(PlainClassInstance)}: {{PlainClassInstance}}",
        };
        
        public static readonly object[][] Args =
        { 
            new object[]{Boolean}, 
            new object[]{Byte}, 
            new object[]{SByte}, 
            new object[]{Char},
            
            new object[]{Decimal}, 
            new object[]{Double}, 
            new object[]{Float}, 
            new object[]{Int},
            
            new object[]{UInt}, 
            new object[]{Long}, 
            new object[]{ULong}, 
            new object[]{Short},
            
            new object[]{UShort}, 
            new object[]{String},
            new object[]{Enum},
            
            
            new object[]{BooleanArray}, 
            new object[]{ByteArray}, 
            new object[]{SByteArray}, 
            new object[]{CharArray},
            
            new object[]{DecimalArray},
            new object[]{DoubleArray},
            new object[]{FloatArray},
            new object[]{IntArray},
            
            new object[]{UIntArray},
            new object[]{LongArray},
            new object[]{ULongArray},
            new object[]{ShortArray},
            
            new object[]{UShortArray},
            new object[]{StringArray},
            new object[]{EnumArray},

            
            new object[]{BooleanList},
            new object[]{ByteList},
            new object[]{SByteList},
            new object[]{CharList},
            
            new object[]{DecimalList},
            new object[]{DoubleList},
            new object[]{FloatList},
            new object[]{IntList},
            
            new object[]{UIntList},
            new object[]{LongList},
            new object[]{ULongList},
            new object[]{ShortList},
            
            new object[]{UShortList},
            new object[]{StringList},
            new object[]{EnumList},
            
            
            new object[]{PrimitiveDictionary},
            new object[]{ArrayDictionary},
            new object[]{ListDictionary},
            new object[]{PlainClassInstance},
        };
    }

    internal enum EnumInstance
    {
        One,
        Two,
        Three
    }

    // ReSharper disable UnusedAutoPropertyAccessor.Global
    internal class PlainClassInstance
    {
        public bool Boolean { get; set; }
        public byte Byte { get; set; }
        public sbyte Sbyte { get; set; }
        public char Char { get; set; }
        public decimal Decimal { get; set; }
        public double Double { get; set; }
        public float Float { get; set; }
        public int Int { get; set; }
        public uint UInt { get; set; }
        public long Long { get; set; }
        public ulong ULong { get; set; }
        public short Short { get; set; }
        public ushort UShort { get; set; }
        public string String { get; set; }
        public EnumInstance Enum { get; set; }
    }
}

