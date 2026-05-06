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
using Web.Endpoints.Modules.WorkflowTemplate.Template.Requests.Commands;
using Web.Endpoints.Routes;
using WorkflowTemplate.Shared.Commands.Template;

namespace Web.Endpoints.Modules.WorkflowTemplate.Template;

[Authorize(Policy = IdentityPolicies.CanManageWorkflowTemplates)]
public sealed class CreateWorkflowTemplateEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<CreateWorkflowTemplateRequest>
    .WithActionResult<IdentifierResponse>
{
    [ApiVersion("1.0")]
    [HttpPost(WorkflowTemplateRoutes.CreateWorkflowTemplate)]
    [ProducesResponseType(typeof(IdentifierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        OperationId = "CreateWorkflowTemplate",
        Summary     = "Criar template de workflow",
        Description = """
            Cria um novo template de workflow com status `Draft` e retorna o identificador criado.

            **Regras:**
            - Nenhuma validação de unicidade; o nome pode repetir entre templates.

            **Respostas:**
            - `200` - template criado; retorna o identificador gerado.
            - `400` - dados inválidos ou ausentes.
            - `401` - access token ausente ou inválido.
            - `403` - usuário sem permissão.

            **Efeitos:** Novo template criado no status `Draft` — precisa ter ao menos uma etapa com uma tarefa antes de ser ativado.

            **Autorização:** SystemAdmin.
            """,
        Tags = [Tags.WorkflowTemplate.Templates])]
    public override async Task<ActionResult<IdentifierResponse>> HandleAsync(CreateWorkflowTemplateRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(request => new CreateWorkflowTemplateCommand(
                Name: request.Name,
                Description: request.Description))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
