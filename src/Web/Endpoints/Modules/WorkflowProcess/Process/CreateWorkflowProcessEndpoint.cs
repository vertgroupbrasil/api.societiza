using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Response;
using Core.Shared.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.WorkflowProcess.Process.Requests.Commands;
using Web.Endpoints.Routes;
using WorkflowProcess.Shared.Commands.Process;

namespace Web.Endpoints.Modules.WorkflowProcess.Process;

[Authorize]
public sealed class CreateWorkflowProcessEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<CreateWorkflowProcessRequest>
    .WithActionResult<IdentifierResponse>
{
    [ApiVersion("1.0")]
    [HttpPost(WorkflowProcessRoutes.CreateWorkflowProcess)]
    [ProducesResponseType(typeof(IdentifierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "CreateWorkflowProcess",
        Summary     = "Criar processo societário",
        Description = """
            Cria um novo processo societário a partir de um template ativo e retorna o identificador gerado.

            **Regras:**
            - O template informado deve estar no status `Active`.
            - O processo é materializado com todas as etapas e tarefas do template no momento da criação.

            **Valores de enumeração na requisição:**
            - `processType`: `Abertura` | `Alteracao` | `Baixa`

            **Respostas:**
            - `200` - processo criado; retorna o identificador gerado.
            - `400` - dados inválidos ou ausentes.
            - `401` - access token ausente ou inválido.
            - `403` - usuário sem permissão.
            - `404` - template não encontrado ou não está ativo.

            **Efeitos:** Processo criado no status `InProgress` com todas as etapas em `NotStarted` e tarefas em `Pending`.

            **Autorização:** Qualquer usuário autenticado.
            """,
        Tags = [Tags.WorkflowProcess.Processes])]
    public override async Task<ActionResult<IdentifierResponse>> HandleAsync(
        CreateWorkflowProcessRequest request,
        CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(r => new CreateWorkflowProcessCommand(
                AccountancyId: r.AccountancyId,
                TemplateId: r.TemplateId,
                ProcessType: r.ProcessType,
                TargetClient: r.TargetClient))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}