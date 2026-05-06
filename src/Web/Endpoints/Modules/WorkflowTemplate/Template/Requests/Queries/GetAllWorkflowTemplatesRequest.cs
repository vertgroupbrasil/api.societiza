using Microsoft.AspNetCore.Mvc;

namespace Web.Endpoints.Modules.WorkflowTemplate.Template.Requests.Queries;

public sealed class GetAllWorkflowTemplatesRequest
{
    [FromQuery] public bool IncludeDraft   { get; set; } = false;
    [FromQuery] public bool IncludeArchive { get; set; } = false;
    [FromQuery] public int  PageNumber     { get; set; } = 1;
    [FromQuery] public int  PageSize       { get; set; } = 10;
}
