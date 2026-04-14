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
using Web.Endpoints.Modules.WorkflowTemplate.TemplateStepTask.Fields.Requests.Commands;
using Web.Endpoints.Routes;
using WorkflowTemplate.Shared.Commands.TemplateStepTask.Fields;

namespace Web.Endpoints.Modules.WorkflowTemplate.TemplateStepTask.Fields;

public sealed class AddTemplateStepTaskFieldEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<AddTemplateStepTaskFieldRequest>
    .WithActionResult<IdentifierResponse>
{
    [ApiVersion("1.0")]
    [Authorize(Policy = IdentityPolicies.CanManageWorkflowTemplates)]
    [HttpPost(WorkflowTemplateRoutes.AddTemplateStepTaskField)]
    [ProducesResponseType(typeof(IdentifierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "AddTemplateStepTaskField",
        Summary     = "Adicionar campo à tarefa",
        Description = """
            Adiciona um novo campo a uma tarefa de uma etapa do template e retorna o identificador criado.

            **Regras:**
            - Retorna 404 caso o template, a etapa ou a tarefa não existam.
            - Valores aceitos para `fieldType`:
              - `Text` — entrada de texto em linha única.
              - `Boolean` — alternância verdadeiro/falso.
              - `Integer` — entrada numérica.
              - `Select` — lista suspensa com opções predefinidas.
              - `Date` — seletor de data.
            - `options` é obrigatório quando `fieldType` é `Select` e deve ser nulo para os demais tipos.
            **Efeitos:** Novo campo criado na tarefa.
            **Autorização:** Requer autenticação. Papel: SystemAdmin.
            """,
        Tags = [Tags.WorkflowTemplate.StepTaskFields])]
    public override async Task<ActionResult<IdentifierResponse>> HandleAsync(AddTemplateStepTaskFieldRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(request => new AddTemplateStepTaskFieldCommand(
                TemplateId: request.TemplateId,
                StepId: request.StepId,
                TaskId: request.TaskId,
                Label: request.Body.Label,
                FieldType: request.Body.FieldType,
                Options: request.Body.Options,
                Order: request.Body.Order))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
