using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.WorkflowProcess.ProcessStep.Fields.Requests.Commands;
using Web.Endpoints.Routes;
using WorkflowProcess.Shared.Commands.ProcessStep.Fields;

namespace Web.Endpoints.Modules.WorkflowProcess.ProcessStep.Fields;

[Authorize]
public sealed class FillProcessStepFieldEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<FillProcessStepFieldRequest>
    .WithActionResult
{
    [ApiVersion("1.0")]
    [HttpPut(WorkflowProcessRoutes.FillProcessStepField)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [SwaggerOperation(
        OperationId = "FillProcessStepField",
        Summary     = "Preencher campo de etapa do processo",
        Description = """
            Preenche um campo de uma etapa do processo societário.

            **Regras:**
            - O processo deve estar no status `InProgress`.
            - A etapa informada deve ser a etapa atual (a primeira etapa ainda não concluída).

            **Respostas:**
            - `200` - campo preenchido com sucesso.
            - `400` - dados inválidos ou ausentes.
            - `401` - access token ausente ou inválido.
            - `403` - usuário sem permissão.
            - `404` - processo, etapa ou campo não encontrados.
            - `409` - etapa bloqueada ou processo já concluído.

            **Efeitos:** Nenhum.

            **Autorização:** Qualquer usuário autenticado.
            """,
        Tags = [Tags.WorkflowProcess.Fields])]
    public override async Task<ActionResult> HandleAsync(
        FillProcessStepFieldRequest request,
        CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(r => new FillProcessStepFieldCommand(
                ProcessId: r.ProcessId,
                StepInstanceId: r.StepInstanceId,
                FieldInstanceId: r.FieldInstanceId,
                Value: r.Value))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(() => Ok(), this.HandleFailure);
}