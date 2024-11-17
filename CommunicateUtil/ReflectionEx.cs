//------------------------------------------------------------------------------
//  由Zgf编写 - 致力于让通讯编码解码更简单
//  文档地址：https://gitee.com/zgf211998110/communicate-util.git
//  感谢您的下载和使用
//------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CommunicateUtil
{
    public static class ReflectionEx
    {
        /// <summary>
        /// 获取属性的特性
        /// </summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <param name="propertyInfo"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static TAttribute GetAttribute<TAttribute>(this PropertyInfo propertyInfo)
            where TAttribute : Attribute
        {
            if (propertyInfo == null)
            {
                throw new ArgumentNullException(nameof(propertyInfo));
            }

            return propertyInfo.GetCustomAttribute<TAttribute>();
        }

        /// <summary>
        /// 获取对象属性的特性
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TAttribute"></typeparam>
        /// <param name="obj"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static TAttribute GetAttribute<T, TAttribute>(this T obj, string propertyName)
            where T : class
            where TAttribute : Attribute
        {
            return typeof(T).GetProperty(propertyName).GetAttribute<TAttribute>();
        }

        //根据属性名称获取对象属性值
        public static object GetPropertyValue(this object obj, string propertyName)
        {
            return obj.GetType().GetProperty(propertyName).GetValue(obj, null);
        }
    }
}
