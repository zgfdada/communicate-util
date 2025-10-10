using CommunicateUtil.BytesDataAdapters.ValueTypeAdapters;
using CommunicateUtil;
using CommunicateUtil.BytesDataAdapters.StringTypeAdapters;

namespace TestProject1
{
    [Flags]
    public enum EnumType
    {
        item1 = 1,
        item2,
        item3,
        item4,
        item5,
    }
    public class _0_ValueTest
    {
        [Fact]
        //值类型数据适配器测试
        public void ValueTypeAdaptersTest()
        {
            //值类型数据适配器测试
            int lengh;

            //byte
            object obj = (byte)5;
            byte[] bytearray = ValueTypeAdapter.GetBytes(out lengh, obj, EndianType.Big_ABCD);
            object obj_2 = ValueTypeAdapter.GetValue<byte>(out lengh, bytearray, EndianType.Big_ABCD);
            Assert.Equal(obj, obj_2);

            //string
            obj = "张图帅" as string;
            bytearray = StringTypeAdapter.GetBytes(obj.ToString(), 9);
            obj_2 = StringTypeAdapter.GetString(bytearray, 9);
            Assert.Equal(obj, obj_2);

            //short
            obj = (short)55;
            bytearray = ValueTypeAdapter.GetBytes(out lengh, obj, EndianType.Big_ABCD);
            obj_2 = ValueTypeAdapter.GetValue<short>(out lengh, bytearray, EndianType.Big_ABCD);
            Assert.Equal(obj, obj_2);

            //float
            obj = (float)6.666;
            bytearray = ValueTypeAdapter.GetBytes(out lengh, obj, EndianType.Big_ABCD);
            obj_2 = ValueTypeAdapter.GetValue<float>(out lengh, bytearray, EndianType.Big_ABCD);
            Assert.Equal(obj, obj_2);

            //double
            obj = (double)6.666;
            bytearray = ValueTypeAdapter.GetBytes(out lengh, obj, EndianType.Big_ABCD);
            obj_2 = ValueTypeAdapter.GetValue<double>(out lengh, bytearray, EndianType.Big_ABCD);
            Assert.Equal(obj, obj_2);

            //int
            obj = (int)6.666;
            bytearray = ValueTypeAdapter.GetBytes(out lengh, obj, EndianType.Big_ABCD);
            obj_2 = ValueTypeAdapter.GetValue<int>(out lengh, bytearray, EndianType.Big_ABCD);
            Assert.Equal(obj, obj_2);

            //uint
            obj = (uint)54;
            bytearray = ValueTypeAdapter.GetBytes(out lengh, obj, EndianType.Big_ABCD);
            obj_2 = ValueTypeAdapter.GetValue<uint>(out lengh, bytearray, EndianType.Big_ABCD);
            Assert.Equal(obj, obj_2);

            //longint
            obj = (long)554;
            bytearray = ValueTypeAdapter.GetBytes(out lengh, obj, EndianType.Big_ABCD);
            obj_2 = ValueTypeAdapter.GetValue<long>(out lengh, bytearray, EndianType.Big_ABCD);
            Assert.Equal(obj, obj_2);

            //ulong
            obj = (ulong)223;
            bytearray = ValueTypeAdapter.GetBytes(out lengh, obj, EndianType.Big_ABCD);
            obj_2 = ValueTypeAdapter.GetValue<ulong>(out lengh, bytearray, EndianType.Big_ABCD);
            Assert.Equal(obj, obj_2);

            //enum
            obj = (EnumType)6;
            bytearray = ValueTypeAdapter.GetBytes(out lengh, obj, EndianType.Big_ABCD);
            obj_2 = ValueTypeAdapter.GetValue<EnumType>(out lengh, bytearray, EndianType.Big_ABCD);
            Assert.Equal(obj, obj_2);


            //double不同字节序测试
            foreach (EndianType endianType in Enum.GetValues(typeof(EndianType)))
            {
                obj = (double)3.657;
                bytearray = ValueTypeAdapter.GetBytes(out lengh, obj, endianType);
                obj_2 = ValueTypeAdapter.GetValue<double>(out lengh, bytearray, endianType);
                Assert.Equal(obj, obj_2);
            }

            //float不同字节序测试
            foreach (EndianType endianType in Enum.GetValues(typeof(EndianType)))
            {
                obj = (float)3.657;
                bytearray = ValueTypeAdapter.GetBytes(out lengh, obj, endianType);
                obj_2 = ValueTypeAdapter.GetValue<float>(out lengh, bytearray, endianType);
                Assert.Equal(obj, obj_2);
            }
            
        }
    }
}