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

        private static readonly ConcurrentDictionary<string, MethodInfo> ValidationMethods = new ConcurrentDictionary<string, MethodInfo>();

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
                            string key = $"{assembly.GetName()}.{method.Name}";
                            ValidationMethods.TryAdd(key, method);
                        }
                    }
                }
            }
        }

        public static MethodInfo GetValidationMethod(string assemblyName, string methodName)
        {
            string key = $"{assemblyName}.{methodName}";
            return ValidationMethods.TryGetValue(key, out var methodInfo) ? methodInfo : null;
        }
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
            var method = GetValidationMethod(validationAttr.GetType().Assembly.FullName, validationAttr.MethodName);
            if (method == null)
                throw new InvalidOperationException($"Validation method '{validationAttr.MethodName}' not found in assembly '{validationAttr.AssemblyName}'.");

            var propertyValue = property.GetValue(target);
            var result = method.Invoke(null, new object[] { propertyValue });

            if (result is bool && !(bool)result)
            {
                //Console.WriteLine($"Validation failed for property: {property.Name}");
                return false;
            }

            return true;
        }
    }



    public class User
    {
        [ValidCheckArrtibute("CommunicateUtil", "ValidateEmail")]
        public string Email { get; set; }

        [ValidCheckArrtibute("CommunicateUtil", "ValidateNum")]
        public object Test { get; set; }

        [ValidationMethod()]
        public static bool ValidateEmail(string email)
        {
            return email.Contains("@");
        }
    }

    internal class Valid_TestDemo
    {
        [ValidationMethod]
        public static bool ValidateNum(object obj)
        {
            return int.TryParse(obj.ToString(), out _);
        }
        static void Main(string[] args)
        {
            ValidationUtil.LoadValidationMethods();
            var user = new User { Email = "test%example.com" ,Test = "654%65"};
            bool isValid = user.ValidateProperty("Email");
            isValid = user.ValidateProperty("Test");
            user = new User { Email = "test@example.com", Test = 123456 };
            isValid = user.ValidateProperty("Email");
            isValid = user.ValidateProperty("Test");
        }
    }
}
