//------------------------------------------------------------------------------
//  由Zgf编写 - 致力于让通讯编码解码更简单
//  文档地址：https://gitee.com/zgf211998110/communicate-util.git
//  动态数组或动态列表，在改变数据长度后需要重新对长度的属性重新赋值
//  感谢您的下载和使用
//------------------------------------------------------------------------------
using CommunicateUtil.BytesDataAdapters.ArrayValueTypeAdapters;
using CommunicateUtil.BytesDataAdapters.BaseCommObjAdapters;
using CommunicateUtil.BytesDataAdapters.StringTypeAdapters;
using CommunicateUtil.BytesDataAdapters.ValueTypeAdapters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace CommunicateUtil
{
    [Flags]
    public enum EnumType
    {
        item1 = 0,
        item2,
        item3,
        item4,
        item5,
    }

    public class TestCommunicateObj: BaseCommunicateArrtObject
    {
        //根据协议数据长度获取动态长度的数据数组,即数组长度后紧跟数组数据内容
        [CommunicateArrtibute(OrderIndex = 1, AutoLengthType = typeof(short))]
        public List<float> FloatList
        {
            get;set;
        }

        //起始索引与上一属性结束索引不相同
        [CommunicateArrtibute(OrderIndex = 2, StartIndex = 20, AutoLengthType = typeof(byte))]
        public List<byte> ByteList
        {
            get; set;
        }

        //起始索引与上一属性结束索引不相同
        [CommunicateArrtibute(OrderIndex = 3, StartIndex = 30, AutoLengthType = typeof(byte))]
        public byte[] ByteArray
        {
            get; set;
        }

        //起始索引与上一属性结束索引相同
        [CommunicateArrtibute(OrderIndex = 4, AutoLengthType = typeof(byte))]
        public float[] FloatArray
        {
            get; set;
        }

        //起始索引与上一属性结束索引相同，数组长度已知为5
        [CommunicateArrtibute(OrderIndex = 5, ArrayLength = "5")]
        public short[] ShortArray
        {
            get; set;
        }

        //起始索引与上一属性结束索引相同，数组长度已知为5
        [CommunicateArrtibute(OrderIndex = 6, ArrayLength = "5")]
        public List<short> ShortList
        {
            get; set;
        }

        //起始索引与上一属性结束索引相同
        [CommunicateArrtibute(OrderIndex = 7,AutoLengthType = typeof(short))]
        public List<double> DoubleList
        {
            get; set;
        }

        //起始索引与上一属性结束索引相同
        [CommunicateArrtibute(OrderIndex = 8)]
        public short DoubleArrayLength
        {
            get; set;
        }

        //起始索引与上一属性结束索引相同
        [CommunicateArrtibute(OrderIndex = 9)]
        public int XXXX
        {
            get; set;
        }
        //起始索引与上一属性结束索引相同,使用前置属性作为数组长度
        [CommunicateArrtibute(OrderIndex = 10, ArrayLength = "DoubleArrayLength")]
        public double[] DoubleArray { get; set; }

        [CommunicateArrtibute(OrderIndex = 11)]
        public float FloatValue { get; set; }

        [CommunicateArrtibute(OrderIndex = 12)]
        public double DoubleValue
        {
            get; set;
        }
        [CommunicateArrtibute(OrderIndex = 13)]
        public short ShortValue { get; set; }
        [CommunicateArrtibute(OrderIndex = 14)]
        public byte ByteValue { get; set; }

        [CommunicateArrtibute(OrderIndex = 15)]
        public EnumType EnumValue { get; set; }

        [CommunicateArrtibute(OrderIndex = 16,ArrayLength = "11")]
        public string StringValue { get; set; }
    }
    internal class TestDemo
    {
        static void Main(string[] args)
        {
            TestCommunicateObj obj = new TestCommunicateObj();
            obj.FloatList = new List<float>() { 5.5f ,2.5f};
            obj.ByteList = new List<byte>() { 1, 2, 3, 4, 5 };
            obj.ByteArray = new byte[] { 1, 2, 3 };
            obj.FloatArray = new float[] { 2.5f, 5.5f };
            obj.ShortArray = new short[] { 1, 2, 3, 4, 111 };
            obj.ShortList = new List<short>() {52,666,22,12,33 };
            obj.DoubleList = new List<double>() { 5.5, 2.5 };
            obj.DoubleArrayLength = 2;
            obj.XXXX = 111;
            obj.DoubleArray = new double[] { 1.1, 2.2 };
            obj.FloatValue = 5.5f;
            obj.DoubleValue = 5.6;
            obj.ShortValue = 5;
            obj.ByteValue = 6;
            obj.StringValue = "张图帅";
            obj.EnumValue = EnumType.item3;
            var datas = obj.GetBytes();



            TestCommunicateObj obj_1 = new TestCommunicateObj();
            obj_1.GetSelf(datas.ToList());

            bool isEqual = datas.SequenceEqual(obj_1.GetBytes());

        }
    }

    internal class TestDemo_2
    {
        [ValidationMethod()]
        public static bool ValidateIsTrue(object value)
        {
            return true;
        }

        [ValidationMethod()]
        public static bool ValidateIsFalse(object value)
        {
            return false;
        }
        static void Main(string[] args)
        {
            // ************************解释器测试***********************
            //值类型及其相关数组集合的测试
            int lengh;
            
            EnumType enumType = EnumType.item5;
            var b = ValueTypeAdapter.GetBytes(out lengh,enumType, EndianType.Big,typeof(uint));
            var dsd = b.Select(r => { if (r == 4) return r = 6; return r; }).ToArray();
            var c = ValueTypeAdapter.GetValue<EnumType>(out lengh, dsd, EndianType.Big, typeof(uint));
            var d = ValueTypeAdapter.GetValue(out lengh, dsd, EndianType.Big, typeof(uint),typeof(EnumType));

            object obj = (byte)5;
            byte[] bytearray = ValueTypeAdapter.GetBytes(out lengh, obj, EndianType.Big);
            object obj_2 = ValueTypeAdapter.GetValue<byte>(out lengh, bytearray, EndianType.Big);

            obj = "张图帅" as string;
            bytearray = StringTypeAdapter.GetBytes(obj.ToString(), 8);
            obj_2 = StringTypeAdapter.GetString(bytearray, 8);
            obj = (short)55;
            bytearray = ValueTypeAdapter.GetBytes(out lengh, obj, EndianType.Big);
            obj_2 = ValueTypeAdapter.GetValue<short>(out lengh, bytearray, EndianType.Big);
            obj = (float)6.666;
            bytearray = ValueTypeAdapter.GetBytes(out lengh, obj, EndianType.Big);
            obj_2 = ValueTypeAdapter.GetValue<float>(out lengh, bytearray, EndianType.Big);

            obj = new EnumType[] { EnumType.item5, (EnumType)6 };
            bytearray = ArrayAdapter.GetBytes(out lengh, obj, EndianType.Big, typeof(short));
            obj_2 = ArrayAdapter.GetArrayObj(out lengh, bytearray, obj.GetType(), (obj as EnumType[]).Length, EndianType.Big, typeof(short));
            obj = new float[] { 1.6f, 6f };
            bytearray = ArrayAdapter.GetBytes(out lengh, obj, EndianType.Big);
            obj_2 = ArrayAdapter.GetArrayObj(out lengh, bytearray, obj.GetType(), (obj as float[]).Length, EndianType.Big);

            obj = new List<EnumType>() { EnumType.item5, (EnumType)6 };
            bytearray = ListAdapter.GetBytes(out lengh, obj, EndianType.Big, typeof(short));
            obj_2 = ListAdapter.GetListObj(out lengh, bytearray, obj.GetType(), (obj as List<EnumType>).Count, EndianType.Big, typeof(short));
            obj = new List<float>() { 1.6f, 6f };
            bytearray = ListAdapter.GetBytes(out lengh, obj, EndianType.Big);
            obj_2 = ListAdapter.GetListObj(out lengh, bytearray, obj.GetType(), (obj as List<float>).Count, EndianType.Big);

            //复杂对象测试basecommobj嵌套
            int objlengh = 0;
            TestCommArrObj commArrObj = new TestCommArrObj();
            commArrObj.enumList = new EnumType[] { EnumType.item5, EnumType.item4 };
            commArrObj.aa = 1;
            commArrObj.bb = 2;
            commArrObj.str = "张图超帅";
            commArrObj.EnumType = EnumType.item5;
            commArrObj.EnumType_1 = (EnumType)6;
            commArrObj.arrObj_1 = new TestCommArrObj_1[]
            { 
                new TestCommArrObj_1() { DoubleLengh = 5, Doubles = EnumType.item5 },
                new TestCommArrObj_1() { DoubleLengh = 3, Doubles = EnumType.item3 },
            };
            ValidationUtil.LoadValidationMethods();
            byte[] bytes = commArrObj.ClassGetBytesLogic(out objlengh);
            TestCommArrObj commArrObj_1 = new TestCommArrObj();
            commArrObj_1.ClassGetValueLogic(out objlengh, bytes.ToList());

        }
    }

    public class TestCommArrObj:BaseCommunicateArrtObject
    {
        [CommunicateArrtibute(OrderIndex = 0, EnumEndType = typeof(short),AutoLengthType = typeof(byte))]
        public EnumType[] enumList { get; set; }
        [CommunicateArrtibute(OrderIndex = 1)]
        public byte aa { get; set; }

        [ValidCheckArrtibute("CommunicateUtil", "ValidateIsFalse")]
        [CommunicateArrtibute(OrderIndex = 2)]
        public short bb { get; set; }

        [CommunicateArrtibute(OrderIndex = 2.1f,ArrayLength = "32")]
        public string str { get; set; }

        [CommunicateArrtibute(OrderIndex = 3)]
        public EnumType EnumType { get; set; }

        [CommunicateArrtibute(OrderIndex = 34)]
        public EnumType EnumType_1 { get; set; }
        [ValidCheckArrtibute("CommunicateUtil", "ValidateIsFalse")]
        [CommunicateArrtibute(OrderIndex = 35,AutoLengthType = typeof(byte))]
        public TestCommArrObj_1[] arrObj_1 { get; set; }

    }
    public class TestCommArrObj_1 : BaseCommunicateArrtObject
    {
        [CommunicateArrtibute(OrderIndex = 1)]
        public byte DoubleLengh { get; set; }
        [CommunicateArrtibute(OrderIndex = 2, EnumEndType = typeof(short))]
        public EnumType Doubles { get; set; }

    }
}
