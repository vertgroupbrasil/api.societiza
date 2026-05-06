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
using Web.Endpoints.Modules.WorkflowTemplate.TemplateStepTask.Requests.Commands;
using Web.Endpoints.Routes;
using WorkflowTemplate.Shared.Commands.TemplateStepTask;

namespace Web.Endpoints.Modules.WorkflowTemplate.TemplateStepTask;

[Authorize(Policy = IdentityPolicies.CanManageWorkflowTemplates)]
public sealed class AddWorkflowTemplateStepTaskEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<AddWorkflowTemplateStepTaskRequest>
    .WithActionResult<IdentifierResponse>
{
    [ApiVersion("1.0")]
    [HttpPost(WorkflowTemplateRoutes.AddWorkflowTemplateStepTask)]
    [ProducesResponseType(typeof(IdentifierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "AddWorkflowTemplateStepTask",
        Summary     = "Adicionar tarefa à etapa",
        Description = """
            Adiciona uma nova tarefa a uma etapa do template e retorna o identificador criado.

            **Regras:**
            - Se a `order` solicitada já estiver ocupada, as tarefas existentes são deslocadas para frente automaticamente.

            **Valores de enumeração na requisição:**
            - `type`: `Manual` | `Automatic` | `Approval`

            **Respostas:**
            - `200` - tarefa criada; retorna o identificador gerado.
            - `400` - dados inválidos ou ausentes.
            - `401` - access token ausente ou inválido.
            - `403` - usuário sem permissão.
            - `404` - template ou etapa não encontrados.

            **Efeitos:** Nova tarefa criada na etapa, possivelmente reordenando tarefas existentes.

            **Autorização:** SystemAdmin.
            """,
        Tags = [Tags.WorkflowTemplate.StepTasks])]
    public override async Task<ActionResult<IdentifierResponse>> HandleAsync(AddWorkflowTemplateStepTaskRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(request => new AddWorkflowTemplateStepTaskCommand(
                TemplateId: request.TemplateId,
                StepId: request.StepId,
                Title: request.Body.Title,
                Description: request.Body.Description,
                Type: request.Body.Type,
                Configuration: request.Body.Configuration,
                Order: request.Body.Order,
                IsOptional: request.Body.IsOptional))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
