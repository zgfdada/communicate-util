using CommunicateUtil.Generator.Models;

namespace CommunicateUtil.Generator
{
    public interface ICommunicationSchemaValidator
    {
        SchemaValidationResult Validate(ProtocolSchema schema);
    }
}
