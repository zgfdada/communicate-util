using CommunicateUtil.Generator.Models;

namespace CommunicateUtil.Generator
{
    /// <summary>
    /// 定义通讯协议配置的校验器。
    /// </summary>
    public interface ICommunicationSchemaValidator
    {
        /// <summary>
        /// 校验协议配置是否满足代码生成所需的命名、类型和引用约束。
        /// </summary>
        /// <param name="schema">待校验的协议配置。</param>
        /// <returns>校验结果，包含错误列表和整体有效状态。</returns>
        SchemaValidationResult Validate(ProtocolSchema schema);
    }
}
