using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using Identity.Application.Policies;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.WorkflowTemplate.Template.Requests.Commands;
using Web.Endpoints.Routes;
using WorkflowTemplate.Shared.Commands.Template;

namespace Web.Endpoints.Modules.WorkflowTemplate.Template;

public sealed class ArchiveWorkflowTemplateEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<ArchiveWorkflowTemplateRequest>
    .WithActionResult
{
    [ApiVersion("1.0")]
    [Authorize(Policy = IdentityPolicies.CanManageWorkflowTemplates)]
    [HttpPatch(WorkflowTemplateRoutes.ArchiveWorkflowTemplate)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "ArchiveWorkflowTemplate",
        Summary     = "Arquivar template de workflow",
        Description = """
            Arquiva um template de workflow.

            **Regras:**
            - Status permitidos de origem: `Draft`, `Active`.
            - Status não permitidos: `Archived`.
            - Retorna 404 caso o template não exista.

            **Efeitos:** Status do template muda para `Archived`.

            **Autorização:** Requer autenticação. Papel: SystemAdmin.
            """,
        Tags = [Tags.WorkflowTemplate.Templates])]
    public override async Task<ActionResult> HandleAsync(ArchiveWorkflowTemplateRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(request => new ArchiveWorkflowTemplateCommand(request.Id))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
