using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using Identity.Application.Policies;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.WorkflowTemplate.Template.Requests.Commands;
using Web.Endpoints.Routes;
using WorkflowTemplate.Shared.Commands.Template;

namespace Web.Endpoints.Modules.WorkflowTemplate.Template;

[Authorize(Policy = IdentityPolicies.CanManageWorkflowTemplates)]
public sealed class DeleteWorkflowTemplateEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<DeleteWorkflowTemplateRequest>
    .WithActionResult
{
    [ApiVersion("1.0")]
    [HttpDelete(WorkflowTemplateRoutes.DeleteWorkflowTemplate)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "DeleteWorkflowTemplate",
        Summary     = "Deletar template de workflow",
        Description = """
            Deleta permanentemente um template de workflow (soft delete via projeção).

            **Regras:**
            - Status permitidos de origem: `Draft`, `Archived`.
            - Templates `Active` não podem ser deletados enquanto estiverem em uso.

            **Respostas:**
            - `200` - template deletado com sucesso.
            - `400` - template em status inválido para deleção.
            - `401` - access token ausente ou inválido.
            - `403` - usuário sem permissão.
            - `404` - template não encontrado.

            **Efeitos:** Template marcado como `IsDeleted = true` e removido de todas as listagens.

            **Autorização:** SystemAdmin.
            """,
        Tags = [Tags.WorkflowTemplate.Templates])]
    public override async Task<ActionResult> HandleAsync(
        DeleteWorkflowTemplateRequest request,
        CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(r => new DeleteWorkflowTemplateCommand(r.Id))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
