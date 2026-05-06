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
using Web.Endpoints.Modules.WorkflowTemplate.TemplateStep.Fields.Requests.Commands;
using Web.Endpoints.Routes;
using WorkflowTemplate.Shared.Commands.TemplateStep.Fields;

namespace Web.Endpoints.Modules.WorkflowTemplate.TemplateStep.Fields;

[Authorize(Policy = IdentityPolicies.CanManageWorkflowTemplates)]
public sealed class AddTemplateStepFieldEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<AddTemplateStepFieldRequest>
    .WithActionResult<IdentifierResponse>
{
    [ApiVersion("1.0")]
    [HttpPost(WorkflowTemplateRoutes.AddTemplateStepField)]
    [ProducesResponseType(typeof(IdentifierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "AddTemplateStepField",
        Summary     = "Adicionar campo à etapa",
        Description = """
            Adiciona um novo campo a uma etapa do template e retorna o identificador criado.

            **Regras:**
            - `options` é obrigatório quando `fieldType` é `Select` e deve ser nulo para os demais tipos.

            **Valores de enumeração na requisição:**
            - `fieldType`: `Text` | `Boolean` | `Integer` | `Select` | `Date`

            **Respostas:**
            - `200` - campo criado; retorna o identificador gerado.
            - `400` - dados inválidos ou ausentes.
            - `401` - access token ausente ou inválido.
            - `403` - usuário sem permissão.
            - `404` - template ou etapa não encontrados.

            **Efeitos:** Novo campo criado na etapa.

            **Autorização:** SystemAdmin.
            """,
        Tags = [Tags.WorkflowTemplate.StepFields])]
    public override async Task<ActionResult<IdentifierResponse>> HandleAsync(AddTemplateStepFieldRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(request => new AddTemplateStepFieldCommand(
                TemplateId: request.TemplateId,
                StepId: request.StepId,
                Label: request.Body.Label,
                FieldType: request.Body.FieldType,
                Options: request.Body.Options,
                Order: request.Body.Order))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
