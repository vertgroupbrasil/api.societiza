using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Response;
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

public sealed class AddWorkflowTemplateStepEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<AddWorkflowTemplateStepRequest>
    .WithActionResult<IdentifierResponse>
{
    [ApiVersion("1.0")]
    [Authorize(Policy = IdentityPolicies.CanManageWorkflowTemplates)]
    [HttpPost(WorkflowTemplateRoutes.AddWorkflowTemplateStep)]
    [ProducesResponseType(typeof(IdentifierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "AddWorkflowTemplateStep",
        Summary     = "Adicionar etapa ao template",
        Description = """
            Adiciona uma nova etapa a um template de workflow e retorna o identificador criado.

            **Regras:**
            - Retorna 404 caso o template não exista.
            - Se a `order` solicitada já estiver ocupada, as etapas existentes são deslocadas para frente automaticamente.

            **Campo `appliesTo`:**
            - Lista de tipos de processo aos quais esta etapa se aplica.
            - Valores aceitos: `Abertura`, `Alteracao`, `Baixa`.
            - Lista vazia indica que a etapa se aplica a **todos** os tipos de processo.
            - O backend não valida os valores enviados — valores inválidos são silenciosamente ignorados durante a filtragem do template.

            **Efeitos:** Nova etapa criada no template, possivelmente reordenando etapas existentes.
            **Autorização:** Requer autenticação. Papel: SystemAdmin.
            """,
        Tags = [Tags.WorkflowTemplate.Steps])]
    public override async Task<ActionResult<IdentifierResponse>> HandleAsync(AddWorkflowTemplateStepRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(request => new AddWorkflowTemplateStepCommand(
                TemplateId: request.TemplateId,
                Title: request.Body.Title,
                Description: request.Body.Description,
                Order: request.Body.Order,
                AppliesTo: request.Body.AppliesTo))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}