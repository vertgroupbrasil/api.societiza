using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using Identity.Application.Policies;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.WorkflowTemplate.TemplateStep.Requests.Commands;
using Web.Endpoints.Routes;
using WorkflowTemplate.Shared.Commands.TemplateStep;

namespace Web.Endpoints.Modules.WorkflowTemplate.TemplateStep;

[Authorize(Policy = IdentityPolicies.CanManageWorkflowTemplates)]
public sealed class RemoveWorkflowTemplateStepEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<RemoveWorkflowTemplateStepRequest>
    .WithActionResult
{
    [ApiVersion("1.0")]
    [HttpDelete(WorkflowTemplateRoutes.RemoveWorkflowTemplateStep)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "RemoveWorkflowTemplateStep",
        Summary     = "Remover etapa do template",
        Description = """
            Remove uma etapa do template, junto com todas as suas tarefas e campos.

            **Regras:** Nenhuma.

            **Respostas:**
            - `200` - etapa removida com sucesso.
            - `400` - dados inválidos ou ausentes.
            - `401` - access token ausente ou inválido.
            - `403` - usuário sem permissão.
            - `404` - template ou etapa não encontrados.

            **Efeitos:** Etapa e todas as tarefas e campos aninhados são removidos permanentemente.

            **Autorização:** SystemAdmin.
            """,
        Tags = [Tags.WorkflowTemplate.Steps])]
    public override async Task<ActionResult> HandleAsync(RemoveWorkflowTemplateStepRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(request => new RemoveWorkflowTemplateStepCommand(
                TemplateId: request.TemplateId,
                StepId: request.StepId))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
