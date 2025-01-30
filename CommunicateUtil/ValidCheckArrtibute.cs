using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CommunicateUtil
{
    /// <summary>
    /// 有效性校验特性(通讯框架中不使用该特性默认有效，无效则不进行数据编码解码操作)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ValidCheckArrtibute : Attribute
    {
        /// <summary>
        /// 获取校验方法所在命名控件的名称。
        /// </summary>
        public string AssemblyName { get; }

        /// <summary>
        /// 获取校验方法的名称。
        /// </summary>
        public string MethodName { get; }

        /// <summary>
        /// 初始化 ValidCheckArrtibute 类的新实例。
        /// </summary>
        /// <param name="assemblyName">校验方法所在类库的名称。</param>
        /// <param name="methodName">校验方法的名称。</param>
        public ValidCheckArrtibute(string assemblyName, string methodName)
        {
            AssemblyName = assemblyName;
            MethodName = methodName;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ValidationMethodAttribute : Attribute
    {
        
    }

    public static class ValidationUtil
    {
        // 存储验证方法的并发字典，键为 "AssemblyName.MethodName"
        private static readonly ConcurrentDictionary<string, MethodInfo> ValidationMethods = new ConcurrentDictionary<string, MethodInfo>();

        /// <summary>
        /// 加载所有已加载程序集中的验证方法。
        /// 遍历每个类型并缓存带有 ValidationMethodAttribute 特性的公开静态方法。
        /// </summary>
        public static void LoadValidationMethods()
        {
            // 获取所有已加载的程序集
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                    {
                        var validationAttr = method.GetCustomAttribute<ValidationMethodAttribute>();
                        if (validationAttr != null)
                        {
                            // 缓存方法，使用 "AssemblyName.MethodName" 作为键
                            string key = $"{assembly.GetName().Name}.{method.Name}";
                            ValidationMethods.TryAdd(key, method);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 根据程序集名称和方法名称获取缓存的验证方法。
        /// </summary>
        /// <param name="assemblyName">程序集名称</param>
        /// <param name="methodName">方法名称</param>
        /// <returns>找到的 MethodInfo；如果未找到，则返回 null。</returns>
        public static MethodInfo GetValidationMethod(string assemblyName, string methodName)
        {
            string key = $"{assemblyName}.{methodName}";
            return ValidationMethods.TryGetValue(key, out var methodInfo) ? methodInfo : null;
        }

        /// <summary>
        /// 验证目标对象的指定属性。
        /// </summary>
        /// <typeparam name="T">目标对象的类型</typeparam>
        /// <param name="target">目标对象</param>
        /// <param name="propertyName">要验证的属性名</param>
        /// <returns>如果验证成功返回 true，否则返回 false。</returns>
        public static bool ValidateProperty<T>(this T target, string propertyName)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            Type targetType = target.GetType();
            PropertyInfo property = targetType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

            if (property == null)
                throw new ArgumentException($"Property '{propertyName}' not found in type '{targetType.Name}'.");

            var validationAttr = property.GetCustomAttribute<ValidCheckArrtibute>();
            if (validationAttr == null)
                throw new InvalidOperationException($"Property '{propertyName}' does not have a ValidCheckArrtibute.");

            // 从缓存中获取校验方法
            var method = GetValidationMethod(target.GetType().Assembly.GetName().Name, validationAttr.MethodName);
            if (method == null)
                throw new InvalidOperationException($"Validation method '{validationAttr.MethodName}' not found in assembly '{target.GetType().Assembly.ManifestModule.Name}'.");

            //var propertyValue = property.GetValue(target);
            var result = method.Invoke(null, new object[] { target });

            if (result is bool && !(bool)result)
            {
                //Console.WriteLine($"Validation failed for property: {property.Name}");
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// 有效性管理类
    /// </summary>
    public class ValiadManager
    {
        #region 单例模式
        private static volatile ValiadManager instance;
        private static readonly object syncRoot = new object();

        private ValiadManager() { ValidationUtil.LoadValidationMethods(); }

        public static ValiadManager Instance()
        {
            if (instance == null)
            {
                lock (syncRoot)
                {
                    if (instance == null)
                    {
                        instance = new ValiadManager();
                    }
                }
            }
            return instance;
        }
        #endregion
    }


}
