using CommunicateUtil.BytesDataAdapters.ArrayValueTypeAdapters;
using CommunicateUtil;
using System.Collections;

namespace TestProject1;

public class _1_ValueArrayTest
{
    [Fact]
    /// <summary>
    /// 值数组类型数据适配器测试
    /// </summary>
    public void ValueArrayAdaptersTest()
    {
        int lengh;
        object obj;
        object obj_2;
        byte[] bytearray;
        obj = new EnumType[] { EnumType.item5, (EnumType)6 };
        bytearray = ArrayAdapter.GetBytes(out lengh, obj, EndianType.Big_ABCD, typeof(short));
        obj_2 = ArrayAdapter.GetArrayObj(out lengh, bytearray, obj.GetType(), (obj as EnumType[]).Length, EndianType.Big_ABCD, typeof(short));
        Assert.Equal(obj, obj_2);

        obj = new float[] { 1.6f, 6f };
        bytearray = ArrayAdapter.GetBytes(out lengh, obj, EndianType.Big_ABCD);
        obj_2 = ArrayAdapter.GetArrayObj(out lengh, bytearray, obj.GetType(), (obj as float[]).Length, EndianType.Big_ABCD);
        Assert.Equal(obj, obj_2);
    }
}
