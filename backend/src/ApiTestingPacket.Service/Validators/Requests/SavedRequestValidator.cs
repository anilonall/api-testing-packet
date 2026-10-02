using System.Text.Json;
using ApiTestingPacket.Core;
using ApiTestingPacket.Core.DTOs.Workspace;
using FluentValidation;

namespace ApiTestingPacket.Service.Validators.Requests;

public sealed class SavedRequestValidator : AbstractValidator<SavedRequestInputDto>
{
    private static readonly HashSet<string> Methods = new(["GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS"], StringComparer.OrdinalIgnoreCase);
    public SavedRequestValidator()
    {
        RuleFor(x => x.Definition.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Definition.Method).Must(Methods.Contains).WithMessage("Unsupported HTTP method.");
        RuleFor(x => x.Definition.Url).NotEmpty().MaximumLength(8000);
        RuleFor(x => x.Definition.TimeoutMs).InclusiveBetween(100, 300_000);
        RuleFor(x => x.Definition).Custom((definition, context) =>
        {
            if (definition.Body is not { Kind: RequestBodyKind.Json, Content: not null }) return;
            try { JsonDocument.Parse(definition.Body.Content).Dispose(); }
            catch (JsonException) { context.AddFailure("definition.body.content", "Body must be valid JSON."); }
        });
    }
}
