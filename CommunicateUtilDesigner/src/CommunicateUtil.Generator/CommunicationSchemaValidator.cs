using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using CommunicateUtil.Generator.Models;

namespace CommunicateUtil.Generator
{
    public sealed class CommunicationSchemaValidator : ICommunicationSchemaValidator
    {
        private static readonly Regex IdentifierRegex = new Regex(@"^[_A-Za-z][_A-Za-z0-9]*$", RegexOptions.Compiled);
        private static readonly HashSet<string> BasicTypes = new HashSet<string>(StringComparer.Ordinal)
        {
            "byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong",
            "float", "double", "bool", "char"
        };

        public SchemaValidationResult Validate(ProtocolSchema schema)
        {
            var result = new SchemaValidationResult();
            if (schema == null)
            {
                result.Errors.Add("配置不能为空。");
                return result;
            }

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

            var classNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var cls in schema.Classes)
            {
                RequireIdentifier(cls.Name, "通讯类名称", result);
                if (!string.IsNullOrWhiteSpace(cls.Name) && !classNames.Add(cls.Name))
                {
                    result.Errors.Add("通讯类名称重复：" + cls.Name);
                }
            }

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
                ValidateClass(cls, classNames, enumNames, validationMethods, result);
            }

            return result;
        }

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

                if ((field.TypeKind == FieldTypeKind.String ||
                    field.CollectionKind != FieldCollectionKind.None) &&
                    string.IsNullOrWhiteSpace(field.AutoLengthType) &&
                    string.IsNullOrWhiteSpace(field.ArrayLength))
                {
                    result.Errors.Add("字段缺少数组/字符串长度配置：" + cls.Name + "." + field.Name);
                }

                if (!string.IsNullOrWhiteSpace(field.ValidationMethodName) &&
                    !validationMethods.Contains(field.ValidationMethodName))
                {
                    result.Errors.Add("字段引用了不存在的校验方法：" + cls.Name + "." + field.Name);
                }
            }
        }

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
                    if (!BasicTypes.Contains(field.TypeName))
                    {
                        result.Errors.Add("基础类型不受支持：" + cls.Name + "." + field.Name + " -> " + field.TypeName);
                    }
                    break;
                case FieldTypeKind.String:
                    if (!string.Equals(field.TypeName, "string", StringComparison.Ordinal))
                    {
                        result.Errors.Add("字符串字段 TypeName 必须为 string：" + cls.Name + "." + field.Name);
                    }
                    break;
                case FieldTypeKind.Enum:
                    if (!enumNames.Contains(field.TypeName))
                    {
                        result.Errors.Add("字段引用了不存在的枚举：" + cls.Name + "." + field.Name);
                    }
                    break;
                case FieldTypeKind.CommClass:
                    if (!classNames.Contains(field.TypeName))
                    {
                        result.Errors.Add("字段引用了不存在的通讯类：" + cls.Name + "." + field.Name);
                    }
                    break;
            }
        }

        private static bool TryParseOrderIndex(string value)
        {
            float parsed;
            return !string.IsNullOrWhiteSpace(value) &&
                float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed);
        }

        private static void RequireIdentifier(string value, string label, SchemaValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(value) || !IdentifierRegex.IsMatch(value))
            {
                result.Errors.Add(label + " 不是合法 C# 标识符：" + value);
            }
        }

        private static void RequireNamespace(string value, string label, SchemaValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                result.Errors.Add(label + "不能为空。");
                return;
            }

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
