using CommunicateUtil.BytesDataAdapters.ArrayValueTypeAdapters;
using CommunicateUtil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProject1
{
    public class _2_ValueListTest
    {
        [Fact]
        /// <summary>
        /// 值列表类型数据适配器测试
        /// </summary>
        public void ValueListAdaptersTest()
        {
            int lengh;
            object obj;
            object obj_2;
            byte[] bytearray;
            obj = new List<EnumType>() { EnumType.item5, (EnumType)6 };
            bytearray = ListAdapter.GetBytes(out lengh, obj, EndianType.Big_ABCD, typeof(short));
            obj_2 = ListAdapter.GetListObj(out lengh, bytearray, obj.GetType(), (obj as List<EnumType>).Count, EndianType.Big_ABCD, typeof(short));
            Assert.Equal(obj, obj_2);

            obj = new List<float>() { 1.6f, 6f };
            bytearray = ListAdapter.GetBytes(out lengh, obj, EndianType.Big_ABCD);
            obj_2 = ListAdapter.GetListObj(out lengh, bytearray, obj.GetType(), (obj as List<float>).Count, EndianType.Big_ABCD);
            Assert.Equal(obj, obj_2);

        }
    }
}
