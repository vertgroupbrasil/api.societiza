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

public sealed class AddWorkflowTemplateStepTaskEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<AddWorkflowTemplateStepTaskRequest>
    .WithActionResult<IdentifierResponse>
{
    [ApiVersion("1.0")]
    [Authorize(Policy = IdentityPolicies.CanManageWorkflowTemplates)]
    [HttpPost(WorkflowTemplateRoutes.AddWorkflowTemplateStepTask)]
    [ProducesResponseType(typeof(IdentifierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "AddWorkflowTemplateStepTask",
        Summary     = "Adicionar tarefa à etapa",
        Description = """
            Adiciona uma nova tarefa a uma etapa do template e retorna o identificador criado.

            **Regras:**
            - Retorna 404 caso o template ou a etapa não existam.
            - Valores aceitos para `type`:
              - `Manual` — tarefa concluída manualmente por um usuário.
              - `Automatic` — tarefa executada automaticamente pelo sistema.
              - `Approval` — tarefa que requer aprovação explícita.
            - Se a `order` solicitada já estiver ocupada, as tarefas existentes são deslocadas para frente automaticamente.
            **Efeitos:** Nova tarefa criada na etapa, possivelmente reordenando tarefas existentes.
            **Autorização:** Requer autenticação. Papel: SystemAdmin.
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
