using System.Threading.Tasks;
using CommunicateUtil.Generator.Models;

namespace CommunicateUtil.Generator
{
    public interface ICommunicationCodeGenerator
    {
        Task<GenerationResult> GenerateAsync(GenerateRequest request);
        Task<GenerationResult> GenerateClassAsync(ProtocolSchema schema, string className);
        Task<GenerationResult> GenerateEnumAsync(ProtocolSchema schema, string enumName);
    }
}
