namespace Web.Endpoints;

internal static class Tags
{
    internal const string Accountancy = "Accountancy";

    internal static class Identity
    {
        internal const string Auth    = "Identity / 01. Auth";
        internal const string Invites = "Identity / 02. Invites";
        internal const string Users    = "Identity / 03. Users";
    }

    internal static class WorkflowTemplate
    {
        internal const string Templates      = "Workflow Templates / 01. Templates";
        internal const string Steps          = "Workflow Templates / 02. Steps";
        internal const string StepTasks      = "Workflow Templates / 03. Step Tasks";
        internal const string StepFields     = "Workflow Templates / 04. Step Fields";
        internal const string StepTaskFields = "Workflow Templates / 05. Step Task Fields";
    }

    internal static class WorkflowProcess
    {
        internal const string Processes = "Workflow Process / 01. Processes";
        internal const string Tasks     = "Workflow Process / 02. Tasks";
        internal const string Fields    = "Workflow Process / 03. Fields";
    }
}