//------------------------------------------------------------------------------
//  由Zgf编写 - 致力于让通讯编码解码更简单
//  文档地址：https://github.com/ztg920917/BytesConverter
//  动态数组或动态列表，在改变数据长度后需要重新对长度的属性重新赋值
//  感谢您的下载和使用
//------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommunicateUtil
{
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
        #region 各种值类型数据
        [CommunicateArrtibute(OrderIndex = 1, StartIndex = "0", EndianType = EndianType.Little)]
        public int IntValue_Little { get; set; }

        //若与上一个属性的字节流是连续的，可以将StartIndex设置为设置为"上一个属性名.EndIndex"
        //也可以不要后缀EndIndex，有"."会自动计算上一个的结束字节索引位置
        [CommunicateArrtibute(OrderIndex = 2, StartIndex = "IntValue_Little.EndIndex", EndianType = EndianType.Big)]
        public int IntValue_Big{ get; set; }

        //若与上一个属性的字节流是不连续的，可以将StartIndex设置为设置为该起始字节的索引位置，
        //中间字节填充0x00
        [CommunicateArrtibute(OrderIndex = 3, StartIndex = "15")]
        public float FloatValue { get; set; }

        [CommunicateArrtibute(OrderIndex = 4, StartIndex = "FloatValue.")]
        public double DoubleValue { get; set; }

        [CommunicateArrtibute(OrderIndex = 5, StartIndex = "DoubleValue.EndIndex")]
        public short ShortValue { get; set; }

        [CommunicateArrtibute(OrderIndex = 6, StartIndex = "ShortValue.EndIndex")]
        public EnumType EnumValue { get; set; }
        #endregion

        #region 各种值的数组数据
        //拥有明确的数据长度
        [CommunicateArrtibute(OrderIndex = 7, StartIndex = "EnumValue.EndIndex", ArrayLength = "3")]
        public byte[] ByteValues { get; set; }

        //根据协议获取动态的数据长度
        [CommunicateArrtibute(OrderIndex = 8, StartIndex = "ByteValues.EndIndex")]
        public byte ArrarLengh { get; set; }

        //根据协议数据长度获取动态的数据数组
        [CommunicateArrtibute(OrderIndex = 9, StartIndex = "ArrarLengh.EndIndex", ArrayLength = "ArrarLengh")]
        public float[] FloatArray { get; set; }
        #endregion

        #region 各种值的列表数据
        //拥有明确的数据长度
        [CommunicateArrtibute(OrderIndex = 10, StartIndex = "FloatArray.EndIndex", ArrayLength = "3")]
        public byte[] ByteList { get; set; }

        //根据协议获取动态的数据长度
        [CommunicateArrtibute(OrderIndex = 11, StartIndex = "ByteList.EndIndex")]
        public byte ListLengh { get; set; }

        private List<float> _floatList = new List<float>();
        //根据协议数据长度获取动态的数据数组
        [CommunicateArrtibute(OrderIndex = 12, StartIndex = "ListLengh.EndIndex", ArrayLength = "ListLengh")]
        public List<float> FloatList
        {
            get;set;
        }
        #endregion
    }
    internal class TestDemo
    {
        static void Main(string[] args)
        {
            TestCommunicateObj obj = new TestCommunicateObj();
            obj.IntValue_Little = 1;
            obj.IntValue_Big = 2;
            obj.FloatValue = 3.3f;
            obj.DoubleValue = 4.4d;
            obj.ShortValue = 5;
            obj.EnumValue = EnumType.item4;
            obj.ByteValues = new byte[] { 1, 2, 3 };
            obj.ArrarLengh = 4;
            obj.FloatArray = new float[] { 1.1f, 2.2f, 3.3f, 4.4f };
            obj.ByteList = new byte[] { 1, 2, 3 };
            obj.ListLengh = 1;
            obj.FloatList = new List<float>() { 5.5f };
            var datas = obj.GetBytes();
            TestCommunicateObj obj_1 = new TestCommunicateObj();
            obj_1.GetSelf(datas.ToList());
            obj_1.FloatList.Add(2.5f);
            //动态数组或动态列表，若在获取数据后，又修改了数据长度，需要重新获取数据长度
            obj_1.ListLengh = (byte)obj_1.FloatList.Count();
            var datas_1 = obj_1.GetBytes();
            var isEqual = datas.SequenceEqual(datas_1);
        }
    }
}
