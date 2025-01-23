using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CommunicateUtil.BytesDataAdapters.ValueTypeAdapters
{
    /// <summary>
    /// 枚举适配器
    /// </summary>
    public class EnumAdapter 
    {
        private T ConvertFromBytes<T>(out int lengh,byte[] bytes, EndianType endianType, Type endValueType = null) 
        {
            if (endValueType == null)
                endValueType = typeof(byte);
            var bufferValue = BytesConverter.BytesToValue(bytes, endianType, endValueType);
            lengh = Marshal.SizeOf(endValueType);
            return (T)Enum.ToObject(typeof(T), bufferValue);
        }
        private object ConvertFromBytes(out int lengh, byte[] bytes, EndianType endianType, Type endValueType = null,Type enumType = null)
        {
            if (endValueType == null)
                endValueType = typeof(byte);
            var bufferValue = BytesConverter.BytesToValue(bytes, endianType, endValueType);
            lengh = Marshal.SizeOf(endValueType);
            return Enum.ToObject(enumType, bufferValue);
        }

        private byte[] ConvertToBytes<T>(out int lengh, T value, EndianType endianType, Type endValueType = null)
        {
            if (endValueType == null)
                endValueType = typeof(byte);
            var bufferValue = Convert.ChangeType(value,endValueType);
            lengh = Marshal.SizeOf(endValueType);

            return BytesConverter.ValueToBytes(bufferValue, endianType);
        }

        public static T GetEnumValue<T>(out int lengh, byte[] bytes, EndianType endianType, Type endValueType = null) 
        {
            return new EnumAdapter().ConvertFromBytes<T>(out lengh,bytes, endianType,endValueType);
        }

        public static byte[] GetBytes<T>(out int lengh, T value, EndianType endianType, Type endValueType = null)
        {
            return new EnumAdapter().ConvertToBytes(out lengh, value, endianType, endValueType);
        }

        public static object GetEnumValue(out int lengh, byte[] bytes, EndianType endianType, Type endValueType = null,Type enumType = null)
        {
            return new EnumAdapter().ConvertFromBytes(out lengh, bytes, endianType, endValueType, enumType);
        }
    }
}
