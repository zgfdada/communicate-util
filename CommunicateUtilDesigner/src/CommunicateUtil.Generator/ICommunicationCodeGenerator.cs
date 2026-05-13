using System.Threading.Tasks;
using CommunicateUtil.Generator.Models;

namespace CommunicateUtil.Generator
{
    /// <summary>
    /// 定义通讯模型代码生成器的统一入口。
    /// </summary>
    public interface ICommunicationCodeGenerator
    {
        /// <summary>
        /// 根据完整生成请求生成项目文件、模型代码、枚举代码和辅助文件。
        /// </summary>
        /// <param name="request">生成请求，包含协议配置、输出模式和本地输出目录。</param>
        /// <returns>生成结果，包含文件集合、ZIP 内容、诊断信息和本地输出目录。</returns>
        Task<GenerationResult> GenerateAsync(GenerateRequest request);

        /// <summary>
        /// 只生成指定通讯类的代码文件。
        /// </summary>
        /// <param name="schema">协议配置。</param>
        /// <param name="className">要生成的通讯类名称。</param>
        /// <returns>包含单个通讯类文件的生成结果。</returns>
        Task<GenerationResult> GenerateClassAsync(ProtocolSchema schema, string className);

        /// <summary>
        /// 只生成指定枚举的代码文件。
        /// </summary>
        /// <param name="schema">协议配置。</param>
        /// <param name="enumName">要生成的枚举名称。</param>
        /// <returns>包含单个枚举文件的生成结果。</returns>
        Task<GenerationResult> GenerateEnumAsync(ProtocolSchema schema, string enumName);
    }
}
