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

public sealed class UpdateWorkflowTemplateEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<UpdateWorkflowTemplateRequest>
    .WithActionResult
{
    [ApiVersion("1.0")]
    [Authorize(Policy = IdentityPolicies.CanManageWorkflowTemplates)]
    [HttpPut(WorkflowTemplateRoutes.UpdateWorkflowTemplate)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "UpdateWorkflowTemplate",
        Summary     = "Atualizar template de workflow",
        Description = """
            Atualiza o nome e a descrição de um template de workflow existente.

            **Regras:** Retorna 404 caso o template não exista.
            **Efeitos:** Nenhum.
            **Autorização:** Requer autenticação. Papel: SystemAdmin.
            """,
        Tags = [Tags.WorkflowTemplate.Templates])]
    public override async Task<ActionResult> HandleAsync(UpdateWorkflowTemplateRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(request => new UpdateWorkflowTemplateCommand(
                Id: request.Id,
                Name: request.Body.Name,
                Description: request.Body.Description))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
