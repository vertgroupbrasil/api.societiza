using Microsoft.AspNetCore.Mvc;

namespace Web.Endpoints.Modules.WorkflowProcess.Process.Requests.Queries;

public sealed class ListProcessesByStepRequest
{
    [FromQuery]
    public Guid AccountancyId { get; set; }

    [FromQuery]
    public Guid? TemplateId { get; set; }

    [FromQuery]
    public int PageNumber { get; set; } = 1;

    [FromQuery]
    public int PageSize { get; set; } = 20;
}
