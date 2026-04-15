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

public sealed class ActivateWorkflowTemplateEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<ActivateWorkflowTemplateRequest>
    .WithActionResult
{
    [ApiVersion("1.0")]
    [Authorize(Policy = IdentityPolicies.CanManageWorkflowTemplates)]
    [HttpPatch(WorkflowTemplateRoutes.ActivateWorkflowTemplate)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "ActivateWorkflowTemplate",
        Summary     = "Ativar template de workflow",
        Description = """
            Ativa um template de workflow.

            **Regras:**
            - Status permitidos de origem: `Draft`
            - Status não permitidos: `Active`, `Archived`
            - O template deve conter ao menos uma etapa com ao menos uma tarefa antes de poder ser ativado.
            - Retorna 404 caso o template não exista.
            **Efeitos:** Status do template muda para `Active`.
            **Autorização:** Requer autenticação. Papel: SystemAdmin.
            """,
        Tags = [Tags.WorkflowTemplate.Templates])]
    public override async Task<ActionResult> HandleAsync(ActivateWorkflowTemplateRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(request => new ActivateWorkflowTemplateCommand(request.Id))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
