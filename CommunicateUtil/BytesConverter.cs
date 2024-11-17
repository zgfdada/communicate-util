//------------------------------------------------------------------------------
//  由Zgf编写 - 致力于让通讯编码解码更简单
//  文档地址：https://gitee.com/zgf211998110/communicate-util.git
//  感谢您的下载和使用
//------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CommunicateUtil
{
    /// <summary>
    /// 大小端类型
    /// </summary>
    public enum EndianType
    {
        /// <summary>
        /// 小端模式，即DCBA
        /// </summary>
        Little,

        /// <summary>
        /// 大端模式。即ABCD
        /// </summary>
        Big,

        /// <summary>
        /// 以交换小端格式。即CDAB
        /// </summary>
        LittleSwap,

        /// <summary>
        /// 以交换大端，即：BADC
        /// </summary>
        BigSwap
    }
    /// <summary>
    /// 字节流转换器(致力于让通讯编码解码更简单)
    /// </summary>
    public static class BytesConverter
    {
        #region 字节顺序处理
        /// <summary>
        /// 将小端类型bytes数组根据需要转换成对应的大小端类型bytes数组
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="endianType"></param>
        /// <returns></returns>
        private static byte[] LittleToBytes(byte[] bytes, EndianType endianType)
        {
            byte[] result = new byte[bytes.Length];
            switch (endianType)
            {
                case EndianType.Little:
                    result = bytes;
                    break;
                case EndianType.Big:
                    result = bytes.Reverse().ToArray();
                    break;
                case EndianType.LittleSwap:
                    for (int i = 0; i < bytes.Length; i += 2)
                    {
                        result[i] = bytes[i + 1];
                        result[i + 1] = bytes[i];
                    }
                    break;
                case EndianType.BigSwap:
                    byte[] bufferbytes = bytes.Reverse().ToArray();
                    for (int i = 0; i < bytes.Length; i += 2)
                    {
                        result[i] = bufferbytes[i + 1];
                        result[i + 1] = bufferbytes[i];
                    }
                    break;


            }
            return result;
        }

        /// <summary>
        /// 将bytes数组根据不同大小端模式转换成小端类型bytes数组
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="endianType"></param>
        /// <returns></returns>
        private static byte[] BytesToLittle(byte[] bytes, EndianType endianType)
        {
            return LittleToBytes(bytes, endianType);
        }

        #endregion

        #region 值类型与Byte[]转换
        /// <summary>
        /// 将值类型数据根据不同大小端模式转换成bytes数组
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="endianType"></param>
        /// <returns></returns>
        public static byte[] ValueToBytes<T>(T value, EndianType endianType)
        {
            byte[] sourceBytes = new byte[Marshal.SizeOf(value)];
            GCHandle handle = GCHandle.Alloc(value, GCHandleType.Pinned);
            try
            {
                Marshal.Copy(handle.AddrOfPinnedObject(), sourceBytes, 0, sourceBytes.Length);
            }
            finally
            {
                handle.Free();
            }
            return LittleToBytes(sourceBytes, endianType);
        }

        /// <summary>
        /// 将bytes数组根据不同大小端模式转换成值类型数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bytes"></param>
        /// <param name="endianType"></param>
        /// <returns></returns>
        public static T BytesToValue<T>(byte[] bytes, EndianType endianType)
        {
            byte[] targetBytes = BytesToLittle(bytes, endianType);
            T result = default(T);
            GCHandle handle = GCHandle.Alloc(result, GCHandleType.Pinned);
            try
            {
                Marshal.Copy(targetBytes, 0, handle.AddrOfPinnedObject(), targetBytes.Length);
                result = (T)handle.Target;
            }
            finally
            {
                handle.Free();
            }
            return result;
        }

        /// <summary>
        /// 将bytes数组根据不同大小端模式转换成值类型数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bytes"></param>
        /// <param name="endianType"></param>
        /// <returns></returns>
        public static object BytesToValue(byte[] bytes, EndianType endianType, Type valueType)
        {
            byte[] targetBytes = BytesToLittle(bytes, endianType);
            object result = Activator.CreateInstance(valueType);
            GCHandle handle = GCHandle.Alloc(result, GCHandleType.Pinned);
            try
            {
                Marshal.Copy(targetBytes, 0, handle.AddrOfPinnedObject(), targetBytes.Length);
                result = handle.Target;
            }
            finally
            {
                handle.Free();
            }
            return result;
        }

        #endregion

        #region 协议字段或属性解码
        /// <summary>
        /// bytes转成值对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bytes"></param>
        /// <param name="startIndex"></param>
        /// <param name="endianType"></param>
        /// <returns></returns>
        private static T GetT<T>(List<byte> bytes, int startIndex, EndianType endianType)
        {
            int dataLengh = Marshal.SizeOf<T>();
            return BytesConverter.BytesToValue<T>(
                    bytes.Skip(startIndex).Take(dataLengh).ToArray(),
                    endianType
                    );
        }

        /// <summary>
        /// bytes转成值对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bytes"></param>
        /// <param name="startIndex"></param>
        /// <param name="endianType"></param>
        /// <returns></returns>
        private static object GetT(List<byte> bytes, int startIndex, EndianType endianType, Type valueType)
        {
            int dataLengh = Marshal.SizeOf(valueType);
            return BytesConverter.BytesToValue(
                    bytes.Skip(startIndex).Take(dataLengh).ToArray(),
                    endianType,
                    valueType
                    );
        }

        /// <summary>
        /// bytes转成值对象
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="startIndex"></param>
        /// <param name="endianType"></param>
        /// <param name="valueType"></param>
        /// <returns></returns>
        private static object GetValueType(List<byte> bytes, int startIndex, EndianType endianType, Type valueType)
        {
            object value = null;
            if (valueType.IsValueType)
            {
                if (valueType.IsEnum)
                    value = (int)GetT<byte>(bytes, startIndex, endianType);
                else
                {
                    value = GetT(bytes, startIndex, endianType, valueType);
                }
            }
            //if (valueType.IsEnum)
            //{
            //    value = (int)GetT<byte>(bytes, startIndex, endianType);
            //}
            //else if (valueType == typeof(byte))
            //{
            //    value = GetT<byte>(bytes, startIndex, endianType);
            //}
            //else if (valueType == typeof(int))
            //{
            //    value = GetT<int>(bytes, startIndex, endianType);
            //}
            //else if (valueType == typeof(float))
            //{
            //    value = GetT<float>(bytes, startIndex, endianType);
            //}
            //else if (valueType == typeof(double))
            //{
            //    value = GetT<double>(bytes, startIndex, endianType);
            //}
            //else if (valueType == typeof(short))
            //{
            //    value = GetT<double>(bytes, startIndex, endianType);
            //}
            return value;
        }

        /// <summary>
        /// bytes转成值对象或值对象数组或值对象集合
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="startIndex"></param>
        /// <param name="endianType"></param>
        /// <param name="valueType"></param>
        /// <returns></returns>
        public static object GetT(List<byte> bytes, int startIndex, EndianType endianType, Type valueType, int arrayLengh = 1)
        {
            Type propertyType = valueType;
            object value = null;
            if (propertyType.IsValueType)
            {
                value = GetValueType(bytes, startIndex, endianType, propertyType);
            }
            else if (propertyType == typeof(string))
            {
                //删除结尾0x00的数据
                int dataLengh = bytes.Skip(startIndex).TakeWhile(a => a != 0x00).Count();
                value = Encoding.UTF8.GetString(bytes.ToArray(), startIndex, dataLengh);
            }
            else if (propertyType.IsArray)
            {
                value = Activator.CreateInstance(propertyType, arrayLengh);
                Type type = propertyType.GetElementType();
                if (type.IsValueType)
                {
                    for (int i = 0; i < arrayLengh; i++)
                    {
                        var v = GetValueType(bytes, startIndex + i * Marshal.SizeOf(type), endianType, type);
                        ((Array)value).SetValue(v, i);
                    }
                }
                else
                {
                    throw new NotImplementedException("尚未实现该类型的数据解码");
                }
            }
            else if (typeof(ICollection).IsAssignableFrom(propertyType))
            {
                value = Activator.CreateInstance(propertyType, arrayLengh) as IList;
                Type type = propertyType.GetGenericArguments()[0];
                if (type.IsValueType)
                {
                    MethodInfo addMethod = propertyType.GetMethod("Add");
                    for (int i = 0; i < arrayLengh; i++)
                    {
                        var v = GetValueType(bytes, startIndex + i * Marshal.SizeOf(type), endianType, type);
                        addMethod.Invoke(value, new object[] { v });
                    }
                }
                else
                {
                    throw new NotImplementedException("尚未实现该类型的数据解码");
                }
            }
            else
            {
                throw new NotImplementedException("尚未实现该类型的数据解码");
            }
            return value;
        }

        #endregion

        #region 协议字段或属性编码
        /// <summary>
        /// 将值类型对象转成bytes
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="endianType"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private static byte[] GetValueTypeToBytes<T>(T value, EndianType endianType)
        {
            Type propertyType = value.GetType();
            if (propertyType.IsValueType)
            {
                if (!propertyType.IsEnum)
                    return ValueToBytes(
                        value,
                        endianType
                        );
                else
                {
                    return ValueToBytes(
                           (byte)Convert.ToInt32(value),
                           endianType
                           );
                }
            }
            else
            {
                throw new NotImplementedException("暂未实现该类型的编码");
            }
        }

        /// <summary>
        /// 将值类型对象或值类型数组或值类型集合转成bytes
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="endianType"></param>
        /// <returns></returns>
        public static byte[] GetBytes<T>(T value, EndianType endianType, int strLengh = 11)
        {
            List<byte> data = new List<byte>();
            Type propertyType = value.GetType();
            if (propertyType.IsValueType)
            {
                data.AddRange(BytesConverter.GetValueTypeToBytes(value, endianType));
            }
            else if (propertyType == typeof(string))
            {
                string strValue = value as string;
                var strbytes = System.Text.Encoding.UTF8.GetBytes(strValue);
                strbytes = strbytes.Length < 11 ? strbytes.Concat(Enumerable.Repeat((byte)0x00, strLengh - strbytes.Length)).ToArray() : strbytes;
                data.AddRange(strbytes);
            }
            else if (propertyType.IsArray)
            {
                Array arrayValue = value as Array;
                Type type = propertyType.GetElementType();
                if (type.IsValueType)
                {
                    for (int i = 0; i < arrayValue.Length; i++)
                    {
                        data.AddRange(BytesConverter.GetValueTypeToBytes(arrayValue.GetValue(i), endianType));
                    }
                }
                else
                {
                    throw new NotImplementedException("尚未实现该类型的数据解码");
                }
            }
            else if (typeof(ICollection).IsAssignableFrom(propertyType))
            {
                IList listValue = value as IList;
                Type type = propertyType.GetGenericArguments()[0];
                if (type.IsValueType)
                {
                    for (int i = 0; i < listValue.Count; i++)
                    {
                        data.AddRange(BytesConverter.GetValueTypeToBytes(listValue[i], endianType));

                    }
                }
                else
                {
                    throw new NotImplementedException("尚未实现该类型的数据解码");
                }
            }
            else
            {
                throw new NotImplementedException("尚未实现该类型的数据编码");
            }

            return data.ToArray();
        }
        #endregion
    }
}
