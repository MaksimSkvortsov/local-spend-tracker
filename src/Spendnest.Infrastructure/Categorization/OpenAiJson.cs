using System.Text.Json;

namespace Spendnest.Infrastructure.Categorization;

internal static class OpenAiJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
