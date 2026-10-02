using System.Text.Json;

namespace ApiTestingPacket.Service.Services;

internal static class JsonDefaults
{
    public static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
}
