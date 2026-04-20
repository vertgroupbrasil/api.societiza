using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using Identity.Application.Policies;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.WorkflowTemplate.TemplateStepTask.Fields.Requests.Commands;
using Web.Endpoints.Routes;
using WorkflowTemplate.Shared.Commands.TemplateStepTask.Fields;

namespace Web.Endpoints.Modules.WorkflowTemplate.TemplateStepTask.Fields;

public sealed class UpdateTemplateStepTaskFieldEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<UpdateTemplateStepTaskFieldRequest>
    .WithActionResult
{
    [ApiVersion("1.0")]
    [Authorize(Policy = IdentityPolicies.CanManageWorkflowTemplates)]
    [HttpPut(WorkflowTemplateRoutes.UpdateTemplateStepTaskField)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "UpdateTemplateStepTaskField",
        Summary     = "Atualizar campo da tarefa",
        Description = """
            Atualiza os dados de um campo de uma tarefa do template.

            **Regras:**
            - Retorna 404 caso o template, a etapa, a tarefa ou o campo não existam.
            - Valores aceitos para `fieldType`: `Text` (entrada de texto em linha única), `Boolean` (alternância verdadeiro/falso), `Integer` (entrada numérica), `Select` (lista suspensa com opções predefinidas), `Date` (seletor de data).
            - `options` é obrigatório quando `fieldType` é `Select` e deve ser nulo para os demais tipos.

            **Efeitos:** Dados do campo atualizados.

            **Autorização:** Requer autenticação. Papel: SystemAdmin.
            """,
        Tags = [Tags.WorkflowTemplate.StepTaskFields])]
    public override async Task<ActionResult> HandleAsync(UpdateTemplateStepTaskFieldRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(request => new UpdateTemplateStepTaskFieldCommand(
                TemplateId: request.TemplateId,
                StepId: request.StepId,
                TaskId: request.TaskId,
                FieldId: request.FieldId,
                Label: request.Body.Label,
                FieldType: request.Body.FieldType,
                Options: request.Body.Options,
                Order: request.Body.Order))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
