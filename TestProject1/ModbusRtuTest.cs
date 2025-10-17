using FluentAssertions;
using HslCommunication;
using HslCommunication.ModBus;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProject1
{
    public class ModbusRtuTest
    {

        [Fact]
        public void TestReadHoldingRegisters()
        {
            // 1. 创建Modbus RTU主站对象
            ModbusRtu master = new ModbusRtu();

            // 2. 配置串行参数
            master.SerialPortInni("COM22", 9600); // 端口号、波特率
            master.Station = 1;

            try
            {
                // 3. 连接（打开端口）
                OperateResult connect = master.Open();
                if (!connect.IsSuccess)
                {
                    Console.WriteLine($"连接失败：{connect.Message}");
                    return;
                }

                // 4. 读取保持寄存器（地址0开始的4个）
                var readResult = master.Read("0", 4).Content;
                var data = Data.GetObj(readResult);
                var DataC = Data_CommunicateUtil.GetSelf<Data_CommunicateUtil>(readResult);
                // 使用 FluentAssertions 直接比较
                data.Should().BeEquivalentTo(DataC,
                    options => options.Excluding(x => x.GetBytesIndexDefine).Excluding(x => x.GetSelfIndexDefine));

                var data_bytes = data.GetBytes();
                var DataC_bytes = DataC.GetBytes();
                Assert.Equal(data_bytes, DataC_bytes);

            }
            finally
            {
                // 关闭连接
                master.Close();
            }
        }
    }

    /// <summary>
    /// 数据体数据结构 - 普通做法
    /// </summary>
    public class Data
    {
        public byte Data_1 { get; set; }
        public short Data_2 { get; set; }
        public float Data_3 { get; set; }

        public byte[] GetBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.Add(Data_1);
            bytes.AddRange(BitConverter.GetBytes(Data_2).Reverse());
            bytes.AddRange(BitConverter.GetBytes(Data_3).Reverse());
            return bytes.ToArray();
        }

        public static Data GetObj(byte[] bytes)
        {
            Data data = new Data();
            data.Data_1 = bytes[0];
            data.Data_2 = BitConverter.ToInt16(bytes.Skip(1).Take(2).Reverse().ToArray());
            data.Data_3 = BitConverter.ToSingle(bytes.Skip(3).Take(4).Reverse().ToArray());
            return data;
        }
    }


    public class Data_CommunicateUtil : CommunicateUtil.BaseCommunicateArrtObject
    {
        [CommunicateUtil.CommunicateArrtibute(OrderIndex = 1)]
        public byte Data_1 { get; set; }
        [CommunicateUtil.CommunicateArrtibute(OrderIndex = 2)]
        public short Data_2 { get; set; }
        [CommunicateUtil.CommunicateArrtibute(OrderIndex = 3)]
        public float Data_3 { get; set; }
    }
}