using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
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

public sealed class UpdateTemplateStepFieldEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<UpdateTemplateStepFieldRequest>
    .WithActionResult
{
    [ApiVersion("1.0")]
    [Authorize(Policy = IdentityPolicies.CanManageWorkflowTemplates)]
    [HttpPut(WorkflowTemplateRoutes.UpdateTemplateStepField)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "UpdateTemplateStepField",
        Summary     = "Atualizar campo da etapa",
        Description = """
            Atualiza os dados de um campo de uma etapa do template.

            **Regras:**
            - Retorna 404 caso o template, a etapa ou o campo não existam.
            - Valores aceitos para `fieldType`:
              - `Text` — entrada de texto em linha única.
              - `Boolean` — alternância verdadeiro/falso.
              - `Integer` — entrada numérica.
              - `Select` — lista suspensa com opções predefinidas.
              - `Date` — seletor de data.
            - `options` é obrigatório quando `fieldType` é `Select` e deve ser nulo para os demais tipos.
            **Efeitos:** Dados do campo atualizados.
            **Autorização:** Requer autenticação. Papel: SystemAdmin.
            """,
        Tags = [Tags.WorkflowTemplate.StepFields])]
    public override async Task<ActionResult> HandleAsync(UpdateTemplateStepFieldRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(request => new UpdateTemplateStepFieldCommand(
                TemplateId: request.TemplateId,
                StepId: request.StepId,
                FieldId: request.FieldId,
                Label: request.Body.Label,
                FieldType: request.Body.FieldType,
                Options: request.Body.Options,
                Order: request.Body.Order))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
