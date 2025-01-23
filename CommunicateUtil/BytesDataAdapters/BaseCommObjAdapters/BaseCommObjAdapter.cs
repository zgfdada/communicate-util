using CommunicateUtil.BytesDataAdapters.ArrayValueTypeAdapters;
using CommunicateUtil.BytesDataAdapters.StringTypeAdapters;
using CommunicateUtil.BytesDataAdapters.ValueTypeAdapters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CommunicateUtil.BytesDataAdapters.BaseCommObjAdapters
{

    public static class BaseCommObjAdapter
    {
        #region 编码逻辑

        /// <summary>
        /// 属性编码
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="lengh"></param>
        /// <param name="value"></param>
        /// <param name="commArb"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static byte[] PropGetBytesLogic<T>(out int lengh,T value,CommunicateArrtibute commArb,BaseCommunicateArrtObject classObj = null)
        {
            List<byte> bytes = new List<byte>();
            lengh = 0;
            Type valueType = value.GetType();
            if (valueType.IsValueType)
            {//值类型
                bytes.AddRange(ValueTypeAdapter.GetBytes(out lengh, value, commArb.EndianType, commArb.EnumEndType));
            }
            else if(valueType.IsGenericType)
            {//List<T>
                int bufferlenght = 0;
                if (commArb.AutoLengthType != null)
                {
                    object arrayLengh = Convert.ChangeType((value as IList).Count,commArb.AutoLengthType);
                    bytes.AddRange(ValueTypeAdapter.GetBytes(out bufferlenght, arrayLengh, commArb.EndianType, commArb.EnumEndType));
                }
                bytes.AddRange(ListAdapter.GetBytes(out lengh, value, commArb.EndianType, commArb.EnumEndType));
                lengh += bufferlenght;
            }
            else if(valueType.IsArray)
            {//T[]
                int bufferlenght = 0;
                if (commArb.AutoLengthType != null)
                {
                    object arrayLengh = Convert.ChangeType((value as IList).Count, commArb.AutoLengthType);
                    bytes.AddRange(ValueTypeAdapter.GetBytes(out bufferlenght, arrayLengh, commArb.EndianType, commArb.EnumEndType));
                }
                bytes.AddRange(ArrayAdapter.GetBytes(out lengh, value, commArb.EndianType, commArb.EnumEndType));
                lengh += bufferlenght;

            }
            else if(valueType == typeof(string))
            {
                int bufferLengh;
                int arraylengh = GetArrayLengh(classObj, commArb, null, out bufferLengh);
                bytes.AddRange(StringTypeAdapter.GetBytes(value as string, arraylengh));
                lengh += arraylengh;
            }
            else if(valueType.BaseType == typeof(BaseCommunicateArrtObject))
            {
                int bufferLengh = 0;
                bytes.AddRange(ClassGetBytesLogic(value as BaseCommunicateArrtObject, out bufferLengh));
                lengh += bufferLengh;
            }
            else
            {
                throw new NotImplementedException();
            }
            return bytes.ToArray();
        }
        
        /// <summary>
        /// 类编码
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="lengh"></param>
        /// <returns></returns>
        public static byte[] ClassGetBytesLogic<T>(this T value,out int lengh) where T : BaseCommunicateArrtObject
        {
            lengh = 0;
            List<byte> bytes = new List<byte> { };

            //初始化索引定义
            value.GetBytesIndexDefine.Clear();
            //获取所有CommunicateArrtibute相关的属性
            var props = value.GetType().GetProperties()
                .Where(a => 
                        a.GetCustomAttributes()
                        .Count(b => b.GetType() == typeof(CommunicateArrtibute)) > 0).ToList();
            //属性进行排序
            props = props.OrderBy(a => a.GetCustomAttribute<CommunicateArrtibute>().OrderIndex).ToList();

            //移除所有有效性验证为false的属性
            props.RemoveAll(a => a.GetCustomAttributes()
            .Count(b => b.GetType() == typeof(ValidCheckArrtibute)) > 0 
                && value.ValidateProperty(a.Name) == false);

            //根据属性顺序进行bytes的编码
            for(int i = 0;i<props.Count;i++)
            {
                var prop = props[i];
                CommunicateArrtibute attr = prop.GetCustomAttribute<CommunicateArrtibute>();
                CommPropIndexDefine commPropIndexDefine = new CommPropIndexDefine();
                int startIndex = 0;
                int endIndex = 0;
                //起始索引获取
                if (attr.StartIndex != -1)
                    //自己定义了起始索引就使用定义的
                    startIndex = attr.StartIndex;
                else
                {
                    if(value.GetBytesIndexDefine.Count == 0)
                    {
                        //上一个索引没有值，则使用0作为当前索引的起始索引
                        startIndex = 0;
                    }
                    else
                    {
                        //上一个索引有值，则使用上一个索引的结束索引作为当前索引的起始索引
                        startIndex = value.GetBytesIndexDefine.Last().Value.EndIndex;
                    }
                }

                //填充中间空白区域
                if (bytes.Count < startIndex)
                {
                    bytes.AddRange(Enumerable.Repeat((byte)0x00, startIndex - bytes.Count));
                }
                int propLengh = 0;
                //编码获取bytes
                bytes.AddRange(PropGetBytesLogic(out propLengh, prop.GetValue(value),attr));
                commPropIndexDefine.StartIndex = startIndex;
                commPropIndexDefine.EndIndex = startIndex + propLengh;
                //添加索引定义
                value.GetBytesIndexDefine.Add(attr.OrderIndex,commPropIndexDefine);
            }
            lengh = value.GetBytesIndexDefine.Last().Value.EndIndex;
            return bytes.ToArray();
        }

        #endregion

        #region 解码逻辑

        private static int GetArrayLengh<T>(this T classObj,CommunicateArrtibute arrt, byte[] bytes,out int lengh) where T: BaseCommunicateArrtObject
        {
            int arrayLength = 0;
            lengh = 0;
            if(arrt.AutoLengthType == null)
            {
                //判断字符串是不是纯数字
                bool isNum = Regex.IsMatch(arrt.ArrayLength, @"^\d+$");
                if (isNum)
                {//纯数字为字符长度
                    arrayLength = Convert.ToInt32(arrt.ArrayLength);
                }
                else
                {//非数字为属性值
                    arrayLength = Convert.ToInt32(classObj.GetPropertyValue(arrt.ArrayLength));
                }
                
            }
            else
            {
                arrayLength = Convert.ToInt32(ValueTypeAdapter.GetValue(out lengh,bytes,arrt.EndianType,arrt.AutoLengthType));
            }

            return arrayLength;
        }

        /// <summary>
        /// 属性解码
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <param name="lengh"></param>
        /// <param name="classObj"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static object PropGetValueLogic(this PropertyInfo propertyInfo, out int lengh, BaseCommunicateArrtObject classObj, List<byte> bytes)
        {
            var arrt = propertyInfo.GetCustomAttributes<CommunicateArrtibute>().First();
            var propertyType = propertyInfo.PropertyType;
            object propValue = null;
            lengh = 0;

            if(propertyType.IsValueType)
            {
                Type enumType = null;
                if (propertyType.IsEnum) 
                { enumType = propertyType; }
                int startIndex = classObj.GetSelfIndexDefine.Last().Value.StartIndex;
                Type endType = propertyType;
                if(propertyType.IsEnum)
                    { endType = arrt.EnumEndType; }
                propValue = ValueTypeAdapter.GetValue(out lengh, bytes.ToArray(), arrt.EndianType, endType, enumType);
            }
            else if(propertyType.BaseType == typeof(BaseCommunicateArrtObject))
            {
                propValue = Activator.CreateInstance(propertyType);
                BaseCommunicateArrtObject arrtObject = propValue as BaseCommunicateArrtObject;
                arrtObject.ClassGetValueLogic(out lengh, bytes.ToList());
            }
            else if(propertyType.IsGenericType)
            {
                int bufferLengh = 0;
                int arrayLengh = classObj.GetArrayLengh(arrt, bytes.ToArray(),out bufferLengh);

                propValue = Activator.CreateInstance(propertyType,arrayLengh);

                propValue = ListAdapter.GetListObj(out lengh, bytes.Skip(bufferLengh).ToArray()
                    , propertyType
                    , arrayLengh
                    , arrt.EndianType
                    , arrt.EnumEndType);

                lengh += bufferLengh;
            }
            else if (propertyType.IsArray)
            {
                int bufferLengh = 0;
                int arrayLengh = classObj.GetArrayLengh(arrt, bytes.ToArray(), out bufferLengh);

                propValue = Activator.CreateInstance(propertyType, arrayLengh);

                propValue = ArrayAdapter.GetArrayObj(out lengh, bytes.Skip(bufferLengh).ToArray()
                    , propertyType
                    , arrayLengh
                    , arrt.EndianType
                    , arrt.EnumEndType);

                lengh += bufferLengh;
            }
            else if (propertyType == typeof(string))
            {
                int bufferLengh;
                int arraylengh = GetArrayLengh(classObj, arrt, null, out bufferLengh);
                propValue = StringTypeAdapter.GetString(bytes.ToArray(), arraylengh);
                lengh += arraylengh;
            }
            else
            {
                throw new NotImplementedException();
            }

            return propValue;
        }

        /// <summary>
        /// 类解码
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="classObj"></param>
        /// <param name="datas"></param>
        public static void ClassGetValueLogic(this BaseCommunicateArrtObject classObj, out int lengh, List<byte> datas)
        {
            classObj.GetSelfIndexDefine.Clear();
            var props = classObj.GetType().GetProperties()
                .Where(a => a.GetCustomAttributes().Count(b => b.GetType() == typeof(CommunicateArrtibute)) > 0).ToList();
            //属性进行排序
            props = props.OrderBy(a => a.GetCustomAttribute<CommunicateArrtibute>().OrderIndex).ToList();

            //移除所有有效性验证为false的属性
            props.RemoveAll(a => a.GetCustomAttributes()
            .Count(b => b.GetType() == typeof(ValidCheckArrtibute)) > 0
                && classObj.ValidateProperty(a.Name) == false);

            lengh = 0;
            foreach (var prop in props)
            {
                var propertyType = prop.PropertyType;
                CommPropIndexDefine indexDefine = new CommPropIndexDefine();
                CommunicateArrtibute attr = prop.GetCustomAttribute<CommunicateArrtibute>();
                int startIndex = 0;
                int endIndex = 0;
                //起始索引获取
                if (attr.StartIndex != -1)
                    //自己定义了起始索引就使用定义的
                    startIndex = attr.StartIndex;
                else
                {
                    if (classObj.GetSelfIndexDefine.Count == 0)
                    {
                        //上一个索引没有值，则使用0作为当前索引的起始索引
                        startIndex = 0;
                    }
                    else
                    {
                        //上一个索引有值，则使用上一个索引的结束索引作为当前索引的起始索引
                        startIndex = classObj.GetSelfIndexDefine.Last().Value.EndIndex;
                    }
                }

                int propLengh = 0;
                
                indexDefine.StartIndex = startIndex;
                indexDefine.EndIndex = startIndex + propLengh;
                //添加索引定义
                classObj.GetSelfIndexDefine.Add(attr.OrderIndex, indexDefine);
                prop.SetValue(classObj,prop.PropGetValueLogic(out propLengh, classObj, datas.Skip(startIndex).ToList()));
                
                classObj.GetSelfIndexDefine.Remove(attr.OrderIndex);
                indexDefine.EndIndex = startIndex + propLengh;
                classObj.GetSelfIndexDefine.Add(attr.OrderIndex, indexDefine);

            }
            lengh = classObj.GetSelfIndexDefine.Last().Value.EndIndex;
        }
        #endregion
    }
}
