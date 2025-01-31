using CommunicateUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProject1
{
    public class _5_CommArrObjTest
    {
        //复杂通讯对象适配器测试-动态属性有效性测试
        [Fact]
        public void ComplexCommArrObjAdaptersTest()
        {
            TestCommArrObj aa = new TestCommArrObj();
            TestCommArrObj_1 bb = new TestCommArrObj_1();
            TestCommArrObj_2 cc = new TestCommArrObj_2();
            cc.Bytes = new byte[] { 1, 2 };
            bb.testCommArrObj = cc;
            aa.aa = bb;
            aa.arrObj_1 = new TestCommArrObj_1[] { bb, bb };
            aa.arrObj_2 = new List<TestCommArrObj_1>() { bb, bb };
            byte[] bytes = aa.GetBytes();
            TestCommArrObj dd = new TestCommArrObj();
            dd.GetSelf(bytes.ToList());
            Assert.Equal(bytes, dd.GetBytes());
        }

        public class TestCommArrObj : BaseCommunicateArrtObject
        {
            //BaseCommunicateArrtObject嵌套BaseCommunicateArrtObject
            [CommunicateArrtibute(OrderIndex = 1)]
            public TestCommArrObj_1 aa { get; set; }

            //BaseCommunicateArrtObject嵌套BaseCommunicateArrtObject[]
            [ValidCheckArrtibute("TestProject1", "ValidFalseFun")]
            [CommunicateArrtibute(OrderIndex = 35, AutoLengthType = typeof(byte))]
            public TestCommArrObj_1[] arrObj_1 { get; set; }

            //BaseCommunicateArrtObject嵌套List<BaseCommunicateArrtObject>
            [CommunicateArrtibute(OrderIndex = 36, AutoLengthType = typeof(byte))]
            public List<TestCommArrObj_1> arrObj_2 { get; set; }

        }
        public class TestCommArrObj_1 : BaseCommunicateArrtObject
        {
            [CommunicateArrtibute(OrderIndex = 1)]
            public TestCommArrObj_2 testCommArrObj { get; set; }

        }
        public class TestCommArrObj_2 
            : BaseCommunicateArrtObject
        {
            [CommunicateArrtibute(OrderIndex = 1, AutoLengthType = typeof(byte))]
            public byte[] Bytes { get; set; }

            [ValidationMethod]
            public static bool ValidFalseFun(object obj)
            {
                return false;
            }
        }
    }
}
