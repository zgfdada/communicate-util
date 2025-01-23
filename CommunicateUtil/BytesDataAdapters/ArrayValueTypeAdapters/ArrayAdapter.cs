using CommunicateUtil.BytesDataAdapters.BaseCommObjAdapters;
using CommunicateUtil.BytesDataAdapters.ValueTypeAdapters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CommunicateUtil.BytesDataAdapters.ArrayValueTypeAdapters
{
    public class ArrayAdapter
    {
        public static object GetArrayObj(out int lengh,byte[] bytes, Type arrayType, int arrayLengh, EndianType endianType,Type endType = null)
        {
            object value = Activator.CreateInstance(arrayType, arrayLengh);
            Type type = arrayType.GetElementType();
            if (type.IsValueType)
            {
                int startIndex = 0;
                lengh = 0;
                for (int i = 0; i < arrayLengh; i++)
                {
                    object v = null;
                    if (type.IsEnum)
                    {
                        int bufferLengh = 0;
                        if (endType == null)
                            endType = typeof(byte);
                        int ElementTypeCount = Marshal.SizeOf(endType);
                        v = ValueTypeAdapter.GetValue(out bufferLengh, bytes.Skip(startIndex + i * ElementTypeCount).Take(ElementTypeCount).ToArray(), endianType, endType, type);
                        lengh += bufferLengh;
                    }
                    else
                    {
                        int bufferLengh = 0;
                        int ElementTypeCount = Marshal.SizeOf(type);
                        v = ValueTypeAdapter.GetValue(out bufferLengh, bytes.Skip(startIndex + i * ElementTypeCount).Take(ElementTypeCount).ToArray(), endianType,type);
                        lengh += bufferLengh;
                    }
                    ((Array)value).SetValue(v, i);
                }
                return value;
            }
            else if (type.BaseType == typeof(BaseCommunicateArrtObject))
            { 
                lengh = 0;
                for (int i = 0; i < arrayLengh; i++)
                {
                    BaseCommunicateArrtObject v = Activator.CreateInstance(type) as BaseCommunicateArrtObject;
                    int bufferLengh = 0;

                    BaseCommObjAdapter.ClassGetValueLogic(v, out bufferLengh, bytes.Skip(lengh).ToList());

                    lengh += bufferLengh;
                    ((Array)value).SetValue(v, i);
                }
                return value;

            }
            else
            {
                throw new NotImplementedException("尚未实现该类型的数据解码");
            }
        }

        public static byte[] GetBytes<T>(out int lengh, T value, EndianType endianType, Type endType = null)
        {
            List<byte> data = new List<byte>();

            Array arrayValue = value as Array;
            Type type = value.GetType().GetElementType();
            lengh = 0;
            if (type.IsValueType)
            {
                for (int i = 0; i < arrayValue.Length; i++)
                {
                    int bufferLengh = 0;
                    data.AddRange(ValueTypeAdapter.GetBytes(out bufferLengh,arrayValue.GetValue(i), endianType, endType));
                    lengh += bufferLengh;                
                }
            }
            else if (type.BaseType == typeof(BaseCommunicateArrtObject))
            {
                for (int i = 0; i < arrayValue.Length; i++)
                {
                    int bufferLengh = 0;
                    data.AddRange(BaseCommObjAdapter.ClassGetBytesLogic(arrayValue.GetValue(i) as BaseCommunicateArrtObject, out bufferLengh));
                    lengh += bufferLengh;
                }
            }
            else
            {
                throw new NotImplementedException("尚未实现该类型的数据解码");
            }
            return data.ToArray();
        }
    }
}
