using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using CommunicateUtil.Generator.Models;

namespace CommunicateUtil.Generator
{
    /// <summary>
    /// 校验通讯协议配置是否满足 Generator 生成可编译代码的基本要求。
    /// </summary>
    public sealed class CommunicationSchemaValidator : ICommunicationSchemaValidator
    {
        private static readonly Regex IdentifierRegex = new Regex(@"^[_A-Za-z][_A-Za-z0-9]*$", RegexOptions.Compiled);
        private static readonly HashSet<string> BasicTypes = new HashSet<string>(StringComparer.Ordinal)
        {
            "byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong",
            "float", "double", "bool", "char"
        };

        /// <summary>
        /// 校验协议配置的项目元数据、类型集合、字段配置和方法引用。
        /// </summary>
        /// <param name="schema">待校验的协议配置。</param>
        /// <returns>协议配置校验结果。</returns>
        public SchemaValidationResult Validate(ProtocolSchema schema)
        {
            var result = new SchemaValidationResult();
            if (schema == null)
            {
                result.Errors.Add("配置不能为空。");
                return result;
            }

            // 先校验项目级信息，后续文件名、命名空间和程序集名都依赖这些值。
            RequireIdentifier(schema.ProjectName, "项目名称", result);
            RequireNamespace(schema.Namespace, "命名空间", result);
            RequireIdentifier(schema.AssemblyName, "程序集名称", result);
            if (!string.Equals(schema.TargetFramework, "netstandard2.0", StringComparison.Ordinal))
            {
                result.Errors.Add("生成目标框架必须固定为 netstandard2.0。");
            }

            if (schema.Classes.Count == 0)
            {
                result.Errors.Add("至少需要配置一个通讯类。");
            }

            // 收集通讯类名称，同时检查重复项，后续字段类型引用需要用该集合判断合法性。
            var classNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var cls in schema.Classes)
            {
                RequireIdentifier(cls.Name, "通讯类名称", result);
                if (!string.IsNullOrWhiteSpace(cls.Name) && !classNames.Add(cls.Name))
                {
                    result.Errors.Add("通讯类名称重复：" + cls.Name);
                }
            }

            // 枚举名称和成员名称都必须可生成合法 C# 代码，枚举底层类型也限制为基础类型集合。
            var enumNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var enumSchema in schema.Enums)
            {
                RequireIdentifier(enumSchema.Name, "枚举名称", result);
                if (!string.IsNullOrWhiteSpace(enumSchema.Name) && !enumNames.Add(enumSchema.Name))
                {
                    result.Errors.Add("枚举名称重复：" + enumSchema.Name);
                }

                if (!BasicTypes.Contains(enumSchema.UnderlyingType))
                {
                    result.Errors.Add("枚举底层类型不受支持：" + enumSchema.UnderlyingType);
                }

                var members = new HashSet<string>(StringComparer.Ordinal);
                foreach (var member in enumSchema.Members)
                {
                    RequireIdentifier(member.Name, enumSchema.Name + " 枚举值名称", result);
                    if (!string.IsNullOrWhiteSpace(member.Name) && !members.Add(member.Name))
                    {
                        result.Errors.Add("枚举值名称重复：" + enumSchema.Name + "." + member.Name);
                    }
                }
            }

            // 自定义校验方法名称会生成静态方法并被字段特性引用，因此必须唯一且合法。
            var validationMethods = new HashSet<string>(StringComparer.Ordinal);
            foreach (var method in schema.ValidationMethods)
            {
                RequireIdentifier(method.Name, "校验方法名称", result);
                if (!string.IsNullOrWhiteSpace(method.Name) && !validationMethods.Add(method.Name))
                {
                    result.Errors.Add("校验方法名称重复：" + method.Name);
                }
            }

            foreach (var cls in schema.Classes)
            {
                // 类型集合已经收集完成后再校验字段引用，允许字段引用后声明的通讯类或枚举。
                ValidateClass(cls, classNames, enumNames, validationMethods, result);
            }

            return result;
        }

        /// <summary>
        /// 校验单个通讯类的字段列表、字段顺序、字段类型和校验方法引用。
        /// </summary>
        /// <param name="cls">通讯类配置。</param>
        /// <param name="classNames">已声明的通讯类名称集合。</param>
        /// <param name="enumNames">已声明的枚举名称集合。</param>
        /// <param name="validationMethods">已声明的校验方法名称集合。</param>
        /// <param name="result">校验结果。</param>
        private static void ValidateClass(
            CommClassSchema cls,
            HashSet<string> classNames,
            HashSet<string> enumNames,
            HashSet<string> validationMethods,
            SchemaValidationResult result)
        {
            if (cls.Fields.Count == 0)
            {
                result.Errors.Add("通讯类 " + cls.Name + " 至少需要一个字段。");
            }

            var fields = new HashSet<string>(StringComparer.Ordinal);
            var orderIndexes = new HashSet<string>(StringComparer.Ordinal);
            foreach (var field in cls.Fields)
            {
                // 字段名和 OrderIndex 都必须在同一通讯类内唯一，避免生成重复属性或冲突序列顺序。
                RequireIdentifier(field.Name, cls.Name + " 字段名称", result);
                if (!string.IsNullOrWhiteSpace(field.Name) && !fields.Add(field.Name))
                {
                    result.Errors.Add("字段名称重复：" + cls.Name + "." + field.Name);
                }

                if (!TryParseOrderIndex(field.OrderIndex))
                {
                    result.Errors.Add("字段 OrderIndex 不是合法数字：" + cls.Name + "." + field.Name);
                }
                else if (!orderIndexes.Add(field.OrderIndex.Trim()))
                {
                    result.Errors.Add("字段 OrderIndex 重复：" + cls.Name + "." + field.OrderIndex);
                }

                ValidateFieldType(cls, field, classNames, enumNames, result);

                // 变长字段必须配置固定长度或自动长度类型，否则生成的通讯属性无法确定解析边界。
                if ((field.TypeKind == FieldTypeKind.String ||
                    field.CollectionKind != FieldCollectionKind.None) &&
                    string.IsNullOrWhiteSpace(field.AutoLengthType) &&
                    string.IsNullOrWhiteSpace(field.ArrayLength))
                {
                    result.Errors.Add("字段缺少数组/字符串长度配置：" + cls.Name + "." + field.Name);
                }

                // 字段绑定的校验方法必须已在协议配置中声明，避免生成指向不存在方法的特性。
                if (!string.IsNullOrWhiteSpace(field.ValidationMethodName) &&
                    !validationMethods.Contains(field.ValidationMethodName))
                {
                    result.Errors.Add("字段引用了不存在的校验方法：" + cls.Name + "." + field.Name);
                }
            }
        }

        /// <summary>
        /// 校验字段类型名称是否匹配字段类型分类。
        /// </summary>
        /// <param name="cls">字段所属通讯类。</param>
        /// <param name="field">字段配置。</param>
        /// <param name="classNames">已声明的通讯类名称集合。</param>
        /// <param name="enumNames">已声明的枚举名称集合。</param>
        /// <param name="result">校验结果。</param>
        private static void ValidateFieldType(
            CommClassSchema cls,
            CommFieldSchema field,
            HashSet<string> classNames,
            HashSet<string> enumNames,
            SchemaValidationResult result)
        {
            switch (field.TypeKind)
            {
                case FieldTypeKind.Basic:
                    // 基础字段只能使用明确支持的 C# 基础类型。
                    if (!BasicTypes.Contains(field.TypeName))
                    {
                        result.Errors.Add("基础类型不受支持：" + cls.Name + "." + field.Name + " -> " + field.TypeName);
                    }
                    break;
                case FieldTypeKind.String:
                    // 字符串分类固定生成 string，避免 UI 传入其他 TypeName 后生成不一致代码。
                    if (!string.Equals(field.TypeName, "string", StringComparison.Ordinal))
                    {
                        result.Errors.Add("字符串字段 TypeName 必须为 string：" + cls.Name + "." + field.Name);
                    }
                    break;
                case FieldTypeKind.Enum:
                    // 枚举字段只能引用当前协议中声明过的枚举。
                    if (!enumNames.Contains(field.TypeName))
                    {
                        result.Errors.Add("字段引用了不存在的枚举：" + cls.Name + "." + field.Name);
                    }
                    break;
                case FieldTypeKind.CommClass:
                    // 通讯类字段只能引用当前协议中声明过的通讯类。
                    if (!classNames.Contains(field.TypeName))
                    {
                        result.Errors.Add("字段引用了不存在的通讯类：" + cls.Name + "." + field.Name);
                    }
                    break;
            }
        }

        /// <summary>
        /// 判断字段顺序文本是否为合法浮点数。
        /// </summary>
        /// <param name="value">字段顺序文本。</param>
        /// <returns>字段顺序文本是否合法。</returns>
        private static bool TryParseOrderIndex(string value)
        {
            float parsed;
            return !string.IsNullOrWhiteSpace(value) &&
                float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed);
        }

        /// <summary>
        /// 校验指定文本是否为合法 C# 标识符。
        /// </summary>
        /// <param name="value">待校验文本。</param>
        /// <param name="label">诊断信息中使用的字段标签。</param>
        /// <param name="result">校验结果。</param>
        private static void RequireIdentifier(string value, string label, SchemaValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(value) || !IdentifierRegex.IsMatch(value))
            {
                result.Errors.Add(label + " 不是合法 C# 标识符：" + value);
            }
        }

        /// <summary>
        /// 校验指定文本是否为合法 C# 命名空间。
        /// </summary>
        /// <param name="value">待校验命名空间。</param>
        /// <param name="label">诊断信息中使用的字段标签。</param>
        /// <param name="result">校验结果。</param>
        private static void RequireNamespace(string value, string label, SchemaValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                result.Errors.Add(label + "不能为空。");
                return;
            }

            // 命名空间按点号拆分后，每一段都必须符合 C# 标识符规则。
            foreach (var part in value.Split('.'))
            {
                if (!IdentifierRegex.IsMatch(part))
                {
                    result.Errors.Add(label + " 不是合法 C# 命名空间：" + value);
                    return;
                }
            }
        }
    }
}
