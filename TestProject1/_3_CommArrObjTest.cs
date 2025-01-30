using CommunicateUtil;
using CommunicateUtil.BytesDataAdapters.BaseCommObjAdapters;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProject1
{
    public class _3_CommArrObjTest
    {

        public class TestCommArrObj : BaseCommunicateArrtObject
        {
            //CommunicateArrtibute内的定义请到该类内部去查看，这里第一次使用时会简单介绍
            //OrderIndex - 该属性在协议字节流中的顺序
            //EnumEndType - 该枚举属性在协议中最终的数据类型
            //ArrayLength - 该属性在协议中的数组或集合的长度在协议中所使用的数据类型(长度后紧跟数据内容的情况)
            [CommunicateArrtibute(OrderIndex = 0, EnumEndType = typeof(short), AutoLengthType = typeof(byte))]
            public EnumType[] enumList { get; set; }
            [CommunicateArrtibute(OrderIndex = 1)]
            public byte aa { get; set; }

            [CommunicateArrtibute(OrderIndex = 2)]
            public short bb { get; set; }
            //ArrayLength - 数组或集合的固定长度(字符串字节长度也使用该特性类属性)
            [CommunicateArrtibute(OrderIndex = 2.1f, ArrayLength = "32")]
            public string str { get; set; }

            [CommunicateArrtibute(OrderIndex = 3)]
            public EnumType EnumType { get; set; }

            [CommunicateArrtibute(OrderIndex = 34)]
            public EnumType EnumType_1 { get; set; }
        }
        [Fact]
        /// <summary>
        /// 简单数据对象适配器测试
        /// </summary>
        public void SimpleCommArrObjAdaptersTest()
        {
            TestCommArrObj testCommArrObj = new TestCommArrObj();
            testCommArrObj.enumList = new EnumType[] { EnumType.item5, EnumType.item4 };
            testCommArrObj.aa = 1;
            testCommArrObj.bb = 2;
            testCommArrObj.str = "张图超帅";
            testCommArrObj.EnumType = EnumType.item5;
            testCommArrObj.EnumType_1 = (EnumType)6;

            byte[] bytes = testCommArrObj.ClassGetBytesLogic(out int objlengh);

            TestCommArrObj commArrObj_1 = new TestCommArrObj();
            commArrObj_1.ClassGetValueLogic(out objlengh, bytes.ToList());

            // 使用 FluentAssertions 直接比较
            testCommArrObj.Should().BeEquivalentTo(commArrObj_1, 
                options => options.Excluding(x => x.GetBytesIndexDefine).Excluding(x => x.GetSelfIndexDefine));
        }
    }
}
