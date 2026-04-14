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

public sealed class UpdateWorkflowTemplateStepEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<UpdateWorkflowTemplateStepRequest>
    .WithActionResult
{
    [ApiVersion("1.0")]
    [Authorize(Policy = IdentityPolicies.CanManageWorkflowTemplates)]
    [HttpPut(WorkflowTemplateRoutes.UpdateWorkflowTemplateStep)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "UpdateWorkflowTemplateStep",
        Summary     = "Atualizar etapa do template",
        Description = """
            Atualiza título, descrição, ordem e público-alvo de uma etapa do template.

            **Regras:**
            - Retorna 404 caso o template ou a etapa não existam.
            - Se a `order` solicitada já estiver ocupada, as etapas existentes são deslocadas para frente automaticamente.
            **Efeitos:** Dados da etapa atualizados, possivelmente reordenando etapas existentes.
            **Autorização:** Requer autenticação. Papel: SystemAdmin.
            """,
        Tags = [Tags.WorkflowTemplate.Steps])]
    public override async Task<ActionResult> HandleAsync(UpdateWorkflowTemplateStepRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(request => new UpdateWorkflowTemplateStepCommand(
                TemplateId: request.TemplateId,
                StepId: request.StepId,
                Title: request.Body.Title,
                Description: request.Body.Description,
                Order: request.Body.Order,
                AppliesTo: request.Body.AppliesTo))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
