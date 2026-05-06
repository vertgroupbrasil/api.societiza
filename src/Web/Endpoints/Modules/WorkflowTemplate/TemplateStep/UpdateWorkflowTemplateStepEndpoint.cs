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
public sealed class UpdateWorkflowTemplateStepEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<UpdateWorkflowTemplateStepRequest>
    .WithActionResult
{
    [ApiVersion("1.0")]
    [HttpPut(WorkflowTemplateRoutes.UpdateWorkflowTemplateStep)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "UpdateWorkflowTemplateStep",
        Summary     = "Atualizar etapa do template",
        Description = """
            Atualiza título, descrição, ordem e público-alvo de uma etapa do template.

            **Regras:**
            - Se a `order` solicitada já estiver ocupada, as etapas existentes são deslocadas para frente automaticamente.

            **Respostas:**
            - `200` - etapa atualizada com sucesso.
            - `400` - dados inválidos ou ausentes.
            - `401` - access token ausente ou inválido.
            - `403` - usuário sem permissão.
            - `404` - template ou etapa não encontrados.

            **Efeitos:** Dados da etapa atualizados, possivelmente reordenando etapas existentes.

            **Autorização:** SystemAdmin.
            """,
        Tags = [Tags.WorkflowTemplate.Steps])]
    public override async Task<ActionResult> HandleAsync(UpdateWorkflowTemplateStepRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(request => new UpdateWorkflowTemplateStepCommand(
                TemplateId: request.TemplateId,
                StepId: request.StepId,
                Title: request.Body.Title,
                Description: request.Body.Description,
                Order: request.Body.Order))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
