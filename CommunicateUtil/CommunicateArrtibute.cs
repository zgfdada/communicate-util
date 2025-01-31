//------------------------------------------------------------------------------
//  由Zgf编写 - 致力于让通讯编码解码更简单
//  文档地址：https://gitee.com/zgf211998110/communicate-util.git
//  感谢您的下载和使用
//------------------------------------------------------------------------------
using CommunicateUtil.BytesDataAdapters.BaseCommObjAdapters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
        /// 描述
        /// </summary>
        public string Desc { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remarks { get; set; }

        /// <summary>
        /// 通讯字段或属性的排序索引
        /// </summary>
        public float OrderIndex { get; set; } = 0;

        /// <summary>
        /// 通讯字段或属性在字节流数据中的起始索引,食用于上一属性结束所有与当前属性起始索引不相同的情况
        /// </summary>
        public int StartIndex { get; set; } = -1;

        /// <summary>
        /// 通讯字段或属性的数组或集合长度(适用于协议中数组长度后未紧跟数组内容的情况)
        /// 固定长度时填写数字，动态长度时填写动态长度所在的属性名称
        /// </summary>
        public string ArrayLength { get; set; } = "-1";

        /// <summary>
        /// 通讯字段或属性的数组或集合长度类型(适用于协议中数组长度后紧跟数组内容的情况)
        /// 数据解析器会根据类型进行解析及编码(不用在类中的属性中再做定义了)
        /// </summary>
        public Type AutoLengthType { get; set; }
        
        /// <summary>
        /// 枚举结束类型(根据枚举在协议中所占的长度决定)
        /// </summary>
        public Type EnumEndType { get; set; }

        /// <summary>
        /// 通讯字段或属性的字节编码类型
        /// </summary>
        public EndianType EndianType { get; set; } = EndianType.Big;
    }

    /// <summary>
    /// 通讯属性索引定义
    /// </summary>
    public class CommPropIndexDefine
    {
        /// <summary>
        /// 通讯字段或属性在字节流数据中的起始索引
        /// </summary>
        public int StartIndex = 0;
        /// <summary>
        /// 通讯字段或属性的结束索引
        /// </summary>
        public int EndIndex = 0;

    }
    /// <summary>
    /// 通讯特征对象基类
    /// </summary>
    public class BaseCommunicateArrtObject
    {
        /// <summary>
        /// 编码索引定义
        /// </summary>
        public Dictionary<float, CommPropIndexDefine> GetBytesIndexDefine = new Dictionary<float, CommPropIndexDefine>();

        /// <summary>
        /// 解码索引定义
        /// </summary>
        public Dictionary<float, CommPropIndexDefine> GetSelfIndexDefine = new Dictionary<float, CommPropIndexDefine>();

        /// <summary>
        /// 获取通讯特征对象的起始索引
        /// </summary>
        /// <param name="attr"></param>
        /// <returns></returns>
        public int GetStartIndex(CommunicateArrtibute attr, Dictionary<float, CommPropIndexDefine> define)
        {
            int startIndex = 0;
            if (attr.StartIndex != -1)
                //自己定义了起始索引就使用定义的
                startIndex = attr.StartIndex;
            else
            {
                if (define.Count(a => a.Key == attr.OrderIndex - 1) == 1)
                {
                    //上一个索引有值，则使用上一个索引的结束索引作为当前索引的起始索引
                    startIndex = define[attr.OrderIndex - 1].EndIndex;
                }
                else
                {
                    //上一个索引没有值，则使用0作为当前索引的起始索引
                    startIndex = 0;
                }
            }
            return startIndex;
        }

        /// <summary>
        /// 初始化通讯特征对象编码时的所有属性的起始索引
        /// </summary>
        public void Init_GetBytesIndexDefine()
        {
            GetBytesIndexDefine = new Dictionary<float, CommPropIndexDefine>();

            List<byte> data = new List<byte>();
            var props = this.GetType().GetProperties().Where(a => a.GetCustomAttributes().Count(b => b.GetType() == typeof(CommunicateArrtibute)) > 0);
            props = props.OrderBy(a => a.GetCustomAttribute<CommunicateArrtibute>().OrderIndex).ToArray();
            foreach (var prop in props)
            {
                var attr = prop.GetCustomAttribute<CommunicateArrtibute>();
                float orderIndex = attr.OrderIndex;
                CommPropIndexDefine commPropIndexDefine = new CommPropIndexDefine();
                int startIndex = this.GetStartIndex(attr,this.GetBytesIndexDefine);
                commPropIndexDefine.StartIndex = startIndex;
                commPropIndexDefine.EndIndex = startIndex + this.GetPropByteLength_GetBytes(prop.Name);

                GetBytesIndexDefine.Add(orderIndex, commPropIndexDefine);

            }
        }

        /// <summary>
        /// 根据bytes获取通讯特征对象的所有属性值
        /// </summary>
        /// <param name="datas"></param>
        public virtual void GetSelf(List<byte> datas)
        {
            //this.GetComArtObjSelf(datas);
            int lengh;
            this.ClassGetValueLogic(out lengh, datas);
        }

        /// <summary>
        /// 获取通讯特征对象的字节数组
        /// </summary>
        /// <returns></returns>
        public virtual byte[] GetBytes()
        {
            ////return this.GetComArtObjSelfBytes();
            int lengh;
            return this.ClassGetBytesLogic(out lengh);
        }
    }

    /// <summary>
    /// 通讯特征工具类
    /// </summary>
    public static class CommunicateArrtUtil
    {
        /// <summary>
        /// 获取通讯特征对象的属性字节长度
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="classObj"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static int GetPropByteLength_GetBytes<T>(this T classObj, string propertyName) 
            where T : BaseCommunicateArrtObject
        {
            int length = 0;
            PropertyInfo propertyInfo = classObj.GetType().GetProperty(propertyName);
            Type propertyType = propertyInfo.PropertyType;
            if (propertyType.IsValueType)
            {
                if (propertyType.IsEnum)
                    length = Marshal.SizeOf(typeof(byte));
                else
                    length = Marshal.SizeOf(propertyType);
            }
            else if (propertyType.IsArray)
            {
                Array arrayValue = classObj.GetPropertyValue(propertyName) as Array;
                Type type = propertyType.GetElementType();
                if (type.IsValueType)
                {
                    for (int i = 0; i < arrayValue.Length; i++)
                    {
                        length += Marshal.SizeOf(type);
                    }
                }
            }
            else if (typeof(ICollection).IsAssignableFrom(propertyType))
            {
                IList listValue = classObj.GetPropertyValue(propertyName) as IList;
                Type type = propertyType.GetGenericArguments()[0];
                if (type.IsValueType)
                {
                    for (int i = 0; i < listValue.Count; i++)
                    {
                        length += Marshal.SizeOf(type);
                    }
                }
            }
            else if (propertyType == typeof(string))
            {
                length = propertyInfo.GetArrayLength(classObj,null,0);
            }

            //添加数组长度
            Type arrayLengthType = propertyInfo.GetCustomAttribute<CommunicateArrtibute>().AutoLengthType;
            if (arrayLengthType != null )
            {
                length += Marshal.SizeOf(arrayLengthType);
            }
            return length;
        }

        /// <summary>
        /// 获取通讯特征对象的属性字节长度
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="classObj"></param>
        /// <param name="propertyName"></param>
        /// <param name="bytes"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        public static int GetPropBytesLength_GetSelf<T>(
            this T classObj, string propertyName, byte[] bytes,int startIndex ,out int arrayLength)
            where T : BaseCommunicateArrtObject
        {
            int length = 0;
            arrayLength = 0;
            PropertyInfo propertyInfo = classObj.GetType().GetProperty(propertyName);
            Type propertyType = propertyInfo.PropertyType;
            CommunicateArrtibute arrt = propertyInfo.GetCustomAttribute<CommunicateArrtibute>();
            if (propertyType.IsValueType)
            {
                if (propertyType.IsEnum)
                    length = Marshal.SizeOf(typeof(byte));
                else
                    length = Marshal.SizeOf(propertyType);
            }
            else if(propertyType == typeof(string))
            {
                length = propertyInfo.GetArrayLength(classObj, bytes.ToList(), startIndex);
                arrayLength = length;
            }
            else if (arrt.AutoLengthType == null)
            {
                length = propertyInfo.GetArrayLength(classObj, bytes.ToList(), startIndex);
                arrayLength = length;
                if (propertyType.IsArray)
                {
                    Array arrayValue = classObj.GetPropertyValue(propertyName) as Array;
                    Type type = propertyType.GetElementType();
                    if (type.IsValueType)
                    {
                        length *= Marshal.SizeOf(type);
                    }
                }
                else if (typeof(ICollection).IsAssignableFrom(propertyType))
                {
                    IList listValue = classObj.GetPropertyValue(propertyName) as IList;
                    Type type = propertyType.GetGenericArguments()[0];
                    if (type.IsValueType)
                    {
                        length *= Marshal.SizeOf(type);
                    }
                }
            }
            else if(arrt.AutoLengthType != null)
            {
                object arrayLength_1 = BytesConverter.GetT(bytes.ToList(), startIndex, arrt.EndianType, arrt.AutoLengthType);
                length =(int)Convert.ChangeType(arrayLength_1, typeof(int));
                arrayLength = length;
                if (propertyType.IsArray)
                {
                    Array arrayValue = classObj.GetPropertyValue(propertyName) as Array;
                    Type type = propertyType.GetElementType();
                    if (type.IsValueType)
                    {
                        length *= Marshal.SizeOf(type);
                    }
                }
                else if (typeof(ICollection).IsAssignableFrom(propertyType))
                {
                    IList listValue = classObj.GetPropertyValue(propertyName) as IList;
                    Type type = propertyType.GetGenericArguments()[0];
                    if (type.IsValueType)
                    {
                        length *= Marshal.SizeOf(type);
                    }
                }
            }

            return length;
        }
        /// <summary>
        /// 获取通讯特征属性的长度
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyInfo"></param>
        /// <param name="classObj"></param>
        /// <returns></returns>
        public static int GetArrayLength<T>(this PropertyInfo propertyInfo, T classObj,List<byte> bytes, int startIndex)
        {
            if(propertyInfo.PropertyType != typeof(string))
            if (bytes == null || startIndex == 0)
                return 0;
            var arrt = propertyInfo.GetCustomAttribute<CommunicateArrtibute>();
            int arrayLength = 0;

            //判断字符串是不是纯数字
            bool isNum = Regex.IsMatch(arrt.ArrayLength, @"^\d+$");
            if (isNum)
            {
                arrayLength = Convert.ToInt32(arrt.ArrayLength);
            }
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
        public static void GetComtArtPropValue
            (this PropertyInfo propertyInfo, BaseCommunicateArrtObject classObj, List<byte> datas)
        {
            var arrts = propertyInfo.GetCustomAttributes();
            if (arrts.Count(a => a.GetType() == typeof(CommunicateArrtibute)) == 1)
            {
                var arrt = arrts.Single(a => a.GetType() == typeof(CommunicateArrtibute)) as CommunicateArrtibute;
                Type propertyType = propertyInfo.PropertyType;
                float orderIndex = arrt.OrderIndex;
                int startIndex = classObj.GetStartIndex(arrt,classObj.GetSelfIndexDefine);
                int arrayLength = 0;
                int bytesLength = classObj.GetPropBytesLength_GetSelf(propertyInfo.Name,datas.ToArray(),startIndex,out arrayLength);
                int endIndex = 0;
                if (arrt.AutoLengthType != null)
                    endIndex = startIndex + Marshal.SizeOf(arrt.AutoLengthType) + bytesLength;
                else
                    endIndex = startIndex + bytesLength;
                classObj.GetSelfIndexDefine.Add(orderIndex, new CommPropIndexDefine() { StartIndex = startIndex, EndIndex = endIndex });

                object value = null;
                if (arrt.AutoLengthType != null)
                    value = BytesConverter.GetT(datas, startIndex + Marshal.SizeOf(arrt.AutoLengthType),
                        arrt.EndianType, propertyType, arrayLength);
                else
                    value = BytesConverter.GetT(datas, startIndex, arrt.EndianType, propertyType, arrayLength);
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
            classObj.GetSelfIndexDefine = new Dictionary<float, CommPropIndexDefine>();
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
        public static byte[] GetComtArtPropValueBytes
            (this PropertyInfo propertyInfo, BaseCommunicateArrtObject classObj)
        {
            List<byte> data = new List<byte>();
            var arrts = propertyInfo.GetCustomAttributes();
            if (arrts.Count(a => a.GetType() == typeof(CommunicateArrtibute)) == 1)
            {
                var arrt = arrts.Single(a => a.GetType() == typeof(CommunicateArrtibute)) as CommunicateArrtibute;

                Type propertyType = propertyInfo.PropertyType;
                object value = propertyInfo.GetValue(classObj, null);

                data.AddRange(BytesConverter.GetBytes(value, arrt.EndianType, propertyInfo.GetArrayLength(classObj, null, 0)));

                //自动添加数组长度
                if (arrt.AutoLengthType != null)
                {
                    var length = 0;
                    if (value is Array arr)
                    {
                        length = arr.Length;
                    }
                    else if (value is ICollection icol)
                    {
                        length = icol.Count;
                    }
                    var length_1 = Convert.ChangeType(length, arrt.AutoLengthType);
                    List<byte> bytes = BytesConverter.GetBytes(length_1, arrt.EndianType).Reverse().ToList();
                    foreach (var item in bytes)
                    {
                        data.Insert(0,item);
                    }
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
            classObj.Init_GetBytesIndexDefine();
            List<byte> data = new List<byte>();
            var props = classObj.GetType().GetProperties().Where(a => a.GetCustomAttributes().Count(b => b.GetType() == typeof(CommunicateArrtibute)) > 0);
            props = props.OrderBy(a => a.GetCustomAttribute<CommunicateArrtibute>().OrderIndex).ToArray();
            foreach (var prop in props)
            {
                int startIndex = classObj.GetBytesIndexDefine
                    .Single(a => a.Key == 
                        prop.GetCustomAttribute<CommunicateArrtibute>().OrderIndex)
                    .Value.StartIndex;
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
