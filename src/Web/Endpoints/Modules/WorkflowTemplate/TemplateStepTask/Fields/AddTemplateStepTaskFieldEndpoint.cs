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

[Authorize(Policy = IdentityPolicies.CanManageWorkflowTemplates)]
public sealed class AddTemplateStepTaskFieldEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<AddTemplateStepTaskFieldRequest>
    .WithActionResult<IdentifierResponse>
{
    [ApiVersion("1.0")]
    [HttpPost(WorkflowTemplateRoutes.AddTemplateStepTaskField)]
    [ProducesResponseType(typeof(IdentifierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "AddTemplateStepTaskField",
        Summary     = "Adicionar campo à tarefa",
        Description = """
            Adiciona um novo campo a uma tarefa de uma etapa do template e retorna o identificador criado.

            **Regras:**
            - `options` é obrigatório quando `fieldType` é `Select` e deve ser nulo para os demais tipos.

            **Valores de enumeração na requisição:**
            - `fieldType`: `Text` | `Boolean` | `Integer` | `Select` | `Date`

            **Respostas:**
            - `200` - campo criado; retorna o identificador gerado.
            - `400` - dados inválidos ou ausentes.
            - `401` - access token ausente ou inválido.
            - `403` - usuário sem permissão.
            - `404` - template, etapa ou tarefa não encontrados.

            **Efeitos:** Novo campo criado na tarefa.

            **Autorização:** SystemAdmin.
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
