using CommunicateUtil.BytesDataAdapters.BaseCommObjAdapters;
using CommunicateUtil.BytesDataAdapters.ValueTypeAdapters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CommunicateUtil.BytesDataAdapters.ArrayValueTypeAdapters
{
    public class ListAdapter
    {
        public static object GetListObj(out int lengh,byte[] bytes, Type arrayType, int arrayLengh, EndianType endianType, Type endType = null)
        {
            object value = Activator.CreateInstance(arrayType, arrayLengh) as IList;
            Type type = arrayType.GetGenericArguments()[0];
            if (type.IsValueType)
            {
                MethodInfo addMethod = arrayType.GetMethod("Add");
                int startIndex = 0;
                lengh = 0;
                for (int i = 0; i < arrayLengh; i++)
                {
                    object v = null;
                    int bufferLengh = 0;

                    if (type.IsEnum)
                    {
                        if (endType == null)
                            endType = typeof(byte);
                        int ElementTypeCount = Marshal.SizeOf(endType);
                        v = ValueTypeAdapter.GetValue(out bufferLengh, bytes.Skip(startIndex + i * ElementTypeCount).Take(ElementTypeCount).ToArray(), endianType, endType, type);
                        lengh += bufferLengh;
                    }
                    else
                    {
                        int ElementTypeCount = Marshal.SizeOf(type);
                        v = ValueTypeAdapter.GetValue(out bufferLengh, bytes.Skip(startIndex + i * ElementTypeCount).Take(ElementTypeCount).ToArray(), endianType, type);
                        lengh += bufferLengh;
                    }
                    addMethod.Invoke(value, new object[] { v });
                }
            }
            else if (type.BaseType == typeof(BaseCommunicateArrtObject))
            {
                MethodInfo addMethod = arrayType.GetMethod("Add");
                lengh = 0;
                for (int i = 0; i < arrayLengh; i++)
                {
                    BaseCommunicateArrtObject v = Activator.CreateInstance(type) as BaseCommunicateArrtObject;
                    int bufferLengh = 0;

                    BaseCommObjAdapter.ClassGetValueLogic(v, out bufferLengh, bytes.Skip(lengh).ToList());

                    lengh += bufferLengh;
                    addMethod.Invoke(value, new object[] { v });
                }
            }
            else
            {
                throw new NotImplementedException("尚未实现该类型的数据解码");
            }
            return value;
        }

        public static byte[] GetBytes<T>(out int lengh, T value, EndianType endianType, Type endType = null)
        {
            List<byte> data = new List<byte>();

            IList listValue = value as IList;
            Type type = value.GetType().GetGenericArguments()[0];
            lengh = 0;
            if (type.IsValueType)
            {
                int bufferLengh = 0;
                for (int i = 0; i < listValue.Count; i++)
                {
                    data.AddRange(ValueTypeAdapter.GetBytes(out bufferLengh,listValue[i], endianType, endType));
                    lengh += bufferLengh;
                }
            }
            else if (type.BaseType == typeof(BaseCommunicateArrtObject))
            {
                for (int i = 0; i < listValue.Count; i++)
                {
                    int bufferLengh = 0;
                    data.AddRange(BaseCommObjAdapter.ClassGetBytesLogic(listValue[i] as BaseCommunicateArrtObject, out bufferLengh));
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
