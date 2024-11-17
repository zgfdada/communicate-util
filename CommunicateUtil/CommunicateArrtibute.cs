//------------------------------------------------------------------------------
//  由Zgf编写 - 致力于让通讯编码解码更简单
//  文档地址：https://github.com/ztg920917/BytesConverter
//  感谢您的下载和使用
//------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CommunicateUtil
{
    /// <summary>
    /// 通讯特征类
    /// </summary>
    public class CommunicateArrtibute : Attribute
    {
        /// <summary>
        /// 通讯字段或属性的排序索引
        /// </summary>
        public int OrderIndex { get; set; } = 0;

        /// <summary>
        /// 通讯字段或属性在字节流数据中的起始索引
        /// </summary>
        public string StartIndex { get; set; } = "0";

        /// <summary>
        /// 通讯字段或属性的数组或集合长度
        /// </summary>
        public string ArrayLength { get; set; } = "1";

        /// <summary>
        /// 通讯字段或属性的字节编码类型
        /// </summary>
        public EndianType EndianType { get; set; } = EndianType.Little;
    }

    /// <summary>
    /// 通讯特征对象基类
    /// </summary>
    public class BaseCommunicateArrtObject
    {
        public virtual void GetSelf(List<byte> datas)
        {
            this.GetComArtObjSelf(datas);
        }

        public virtual byte[] GetBytes()
        {
            return this.GetComArtObjSelfBytes();
        }
    }

    /// <summary>
    /// 通讯特征工具类
    /// </summary>
    public static class CommunicateArrtUtil
    {
        public static int GetEndIndex<T>(this T classObj, string propertyName)
        {
            int index = 0;
            PropertyInfo propertyInfo = classObj.GetType().GetProperty(propertyName);
            Type propertyType = propertyInfo.PropertyType;
            if (propertyType.IsValueType)
            {
                if(propertyType.IsEnum)
                    index = propertyInfo.GetStartIndex(classObj)
                   + Marshal.SizeOf(typeof(byte));
                else
                    index = propertyInfo.GetStartIndex(classObj)
                       + Marshal.SizeOf(propertyType);
            }
            else if (propertyType.IsArray)
            {
                index = propertyInfo.GetStartIndex(classObj);
                Array arrayValue = classObj.GetPropertyValue(propertyName) as Array;
                Type type = propertyType.GetElementType();
                if (type.IsValueType)
                {
                    for (int i = 0; i < arrayValue.Length; i++)
                    {
                        index += Marshal.SizeOf(type);
                    }
                }
            }
            else if (typeof(ICollection).IsAssignableFrom(propertyType))
            {
                index = propertyInfo.GetStartIndex(classObj);
                IList listValue = classObj.GetPropertyValue(propertyName) as IList;
                Type type = propertyType.GetGenericArguments()[0];
                if (type.IsValueType)
                {
                    for (int i = 0; i < listValue.Count; i++)
                    {
                        index += Marshal.SizeOf(type);
                    }
                }
            }
            else if (propertyType == typeof(string))
            {
                index = propertyInfo.GetStartIndex(classObj) + propertyInfo.GetArrtLength(classObj);
            }
            return index;
        }
        public static int GetStartIndex<T>(this PropertyInfo propertyInfo, T classObj)
        {
            var arrt = propertyInfo.GetCustomAttribute<CommunicateArrtibute>();
            int arrayLength = 0;
            //判断字符串是不是纯数字
            bool isNum = Regex.IsMatch(arrt.StartIndex, @"^\d+$");
            if (isNum)
                arrayLength = Convert.ToInt32(arrt.StartIndex);
            else if (arrt.StartIndex.Contains("."))
            {
                var lastPropertyName = arrt.StartIndex.Split('.')[0];
                arrayLength = classObj.GetEndIndex(lastPropertyName);
            }
            else
            {
                arrayLength = Convert.ToInt32(classObj.GetPropertyValue(arrt.StartIndex));
            }
            return arrayLength;
        }
        public static int GetArrtLength<T>(this PropertyInfo propertyInfo, T classObj)
        {
            var arrt = propertyInfo.GetCustomAttribute<CommunicateArrtibute>();
            int arrayLength = 0;
            //判断字符串是不是纯数字
            bool isNum = Regex.IsMatch(arrt.ArrayLength, @"^\d+$");
            if (isNum)
                arrayLength = Convert.ToInt32(arrt.ArrayLength);
            else
            {
                arrayLength = Convert.ToInt32(classObj.GetPropertyValue(arrt.ArrayLength));
            }
            return arrayLength;
        }
        #region 属性解码
        /// <summary>
        /// 获取通讯特征属性的值(让解码更简单更快乐)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyInfo"></param>
        /// <param name="classObj"></param>
        /// <param name="datas"></param>
        public static void GetComtArtPropValue<T>
            (this PropertyInfo propertyInfo, T classObj, List<byte> datas) where T : class, new()
        {
            var arrts = propertyInfo.GetCustomAttributes();
            if (arrts.Count(a => a.GetType() == typeof(CommunicateArrtibute)) == 1)
            {
                var arrt = arrts.Single(a => a.GetType() == typeof(CommunicateArrtibute)) as CommunicateArrtibute;
                Type propertyType = propertyInfo.PropertyType;
                object value = null;
                int startIndex = propertyInfo.GetStartIndex(classObj);
                int arrayLengh = propertyInfo.GetArrtLength(classObj);
                value = BytesConverter.GetT(datas, startIndex,
                    arrt.EndianType, propertyType, arrayLengh);
                if (value != null)
                {
                    propertyInfo.SetValue(classObj, value);
                }
            }
        }
        #endregion

        #region 类解码
        /// <summary>
        /// 获取通讯特征类的值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="classObj"></param>
        /// <param name="datas"></param>
        public static void GetComArtObjSelf(this BaseCommunicateArrtObject classObj, List<byte> datas)
        {
            var props = classObj.GetType().GetProperties().Where(a => a.GetCustomAttributes().Count(b => b.GetType() == typeof(CommunicateArrtibute)) > 0);
            props = props.OrderBy(a => a.GetCustomAttribute<CommunicateArrtibute>().OrderIndex).ToArray();
            foreach (var prop in props)
            {
                prop.GetComtArtPropValue(classObj, datas);
            }
        }
        #endregion

        #region 属性编码

        /// <summary>
        /// 获取通讯特征属性的值的bytes
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyInfo"></param>
        /// <param name="classObj"></param>
        /// <param name="datas"></param>
        /// <returns></returns>
        public static byte[] GetComtArtPropValueBytes<T>
            (this PropertyInfo propertyInfo, T classObj) where T : class, new()
        {
            List<byte> data = new List<byte>();
            var arrts = propertyInfo.GetCustomAttributes();
            if (arrts.Count(a => a.GetType() == typeof(CommunicateArrtibute)) == 1)
            {
                var arrt = arrts.Single(a => a.GetType() == typeof(CommunicateArrtibute)) as CommunicateArrtibute;

                Type propertyType = propertyInfo.PropertyType;
                object value = propertyInfo.GetValue(classObj, null);

                data.AddRange(BytesConverter.GetBytes(value, arrt.EndianType, propertyInfo.GetArrtLength(classObj)));

                if (value != null)
                {
                    propertyInfo.SetValue(classObj, value);
                }
            }
            return data.ToArray();
        }
        #endregion

        #region 类编码
        /// <summary>
        /// 获取通讯特征类的bytes(让编码更简单更快乐)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="classObj"></param>
        /// <returns></returns>
        public static byte[] GetComArtObjSelfBytes(this BaseCommunicateArrtObject classObj)
        {
            List<byte> data = new List<byte>();
            var props = classObj.GetType().GetProperties().Where(a => a.GetCustomAttributes().Count(b => b.GetType() == typeof(CommunicateArrtibute)) > 0);
            props = props.OrderBy(a => a.GetCustomAttribute<CommunicateArrtibute>().OrderIndex).ToArray();
            foreach (var prop in props)
            {
                int startIndex = prop.GetStartIndex(classObj);
                int arrayLengh = prop.GetArrtLength(classObj);
                if (data.Count < startIndex)
                {
                    data.AddRange(Enumerable.Repeat((byte)0x00, startIndex - data.Count));
                }
                data.AddRange(prop.GetComtArtPropValueBytes(classObj));
            }
            return data.ToArray();
        }
        #endregion
    }
}
