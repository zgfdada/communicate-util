using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CommunicateUtil.BytesDataAdapters.ValueTypeAdapters
{
    /// <summary>
    /// 值类型适配器
    /// </summary>
    public class ValueTypeAdapter
    {

        public static T GetValue<T>(out int lengh, byte[] bytes, EndianType endianType,Type endType = null)
        {
            if (typeof(T).IsEnum)
                lengh = Marshal.SizeOf(endType == null ? typeof(byte) : endType);
            else
                lengh = Marshal.SizeOf(typeof(T));
            var data = bytes.Take(lengh).ToArray();
            if (typeof(T).IsEnum)
                return EnumAdapter.GetEnumValue<T>(out lengh, data, endianType, endType);
            else
            {
                lengh = Marshal.SizeOf(typeof(T));
                return BytesConverter.BytesToValue<T>(data, endianType);
            }
        }
        public static object GetValue(out int lengh, byte[] bytes, EndianType endianType, Type endType,Type enumType = null)
        {
            if (enumType != null && enumType.IsEnum)
                lengh = Marshal.SizeOf(endType == null ? typeof(byte) : endType);
            else
                lengh = Marshal.SizeOf(endType);
            var data = bytes.Take(lengh).ToArray();
            if (enumType!=null && enumType.IsEnum)
                return EnumAdapter.GetEnumValue(out lengh, data, endianType, endType,enumType);
            else
            {
                lengh = Marshal.SizeOf(endType);
                return BytesConverter.BytesToValue(data, endianType, endType);
            }
        }

        public static byte[] GetBytes<T>(out int lengh, T value, EndianType endianType, Type endType = null)
        {
            if (value.GetType().IsEnum)
                return EnumAdapter.GetBytes(out lengh, value,endianType,endType);
            else
            {
                lengh = Marshal.SizeOf(value);
                return BytesConverter.ValueToBytes(value, endianType);
            }
        }
    }
}
