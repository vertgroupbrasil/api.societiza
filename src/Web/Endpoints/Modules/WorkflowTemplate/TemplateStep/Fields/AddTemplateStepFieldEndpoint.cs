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

public sealed class AddTemplateStepFieldEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<AddTemplateStepFieldRequest>
    .WithActionResult<IdentifierResponse>
{
    [ApiVersion("1.0")]
    [Authorize(Policy = IdentityPolicies.CanManageWorkflowTemplates)]
    [HttpPost(WorkflowTemplateRoutes.AddTemplateStepField)]
    [ProducesResponseType(typeof(IdentifierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "AddTemplateStepField",
        Summary     = "Adicionar campo à etapa",
        Description = """
            Adiciona um novo campo a uma etapa do template e retorna o identificador criado.

            **Regras:**
            - Retorna 404 caso o template ou a etapa não existam.
            - Valores aceitos para `fieldType`:
              - `Text` — entrada de texto em linha única.
              - `Boolean` — alternância verdadeiro/falso.
              - `Integer` — entrada numérica.
              - `Select` — lista suspensa com opções predefinidas.
              - `Date` — seletor de data.
            - `options` é obrigatório quando `fieldType` é `Select` e deve ser nulo para os demais tipos.
            **Efeitos:** Novo campo criado na etapa.
            **Autorização:** Requer autenticação. Papel: SystemAdmin.
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
