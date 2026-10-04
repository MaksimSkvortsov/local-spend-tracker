namespace Spendnest.UiHarness;

/// <summary>
/// Harness-only control endpoints that let browser tests arrange state the desktop app
/// gets from native dialogs.
/// </summary>
public static class HarnessEndpoints
{
    public static IEndpointRouteBuilder MapHarnessEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var harness = endpoints.MapGroup("/harness");

        harness.MapPut("/statement-file", (StatementFileRequest request, DevStatementFilePicker picker) =>
        {
            if (string.IsNullOrWhiteSpace(request.Path))
            {
                return Results.BadRequest("A statement file path is required.");
            }

            try
            {
                var file = picker.Select(request.Path);
                return Results.Ok(file);
            }
            catch (ArgumentException exception)
            {
                return Results.BadRequest(exception.Message);
            }
            catch (FileNotFoundException exception)
            {
                return Results.BadRequest($"{exception.Message} Path: {exception.FileName}");
            }
        });

        harness.MapDelete("/statement-file", (DevStatementFilePicker picker) =>
        {
            picker.Clear();
            return Results.NoContent();
        });

        return endpoints;
    }

    public sealed record StatementFileRequest(string? Path);
}
