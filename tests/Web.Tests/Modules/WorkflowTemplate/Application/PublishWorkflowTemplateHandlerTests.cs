using Moq;
using Core.Shared.Results;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Application.UseCases.Commands.Template;
using WorkflowTemplate.Domain.Enumerations;
using WorkflowTemplate.Shared.Commands.Template;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

namespace Web.Tests.Modules.WorkflowTemplate.Application;

public class PublishWorkflowTemplateHandlerTests
{
    [Fact]
    public async Task Handle_WhenDraftContainsDuplicatedStepTitles_ThrowsArgumentException()
    {
        var source = WorkflowTemplateAggregate.Create("Active Template", "Descricao");
        source.TryDequeueEvent(out _);

        var sourceStepId = source.AddStep("Abertura", "Descricao", 1);
        source.TryDequeueEvent(out _);
        source.Activate();
        source.TryDequeueEvent(out _);

        var draft = WorkflowTemplateAggregate.Create("Draft Template", "Descricao", source.Id);
        draft.TryDequeueEvent(out _);

        _ = draft.AddStep("Abertura", "Descricao", 1);
        var secondStepId = draft.AddStep("Fiscal", "Descricao", 2);
        draft.TryDequeueEvent(out _);
        draft.TryDequeueEvent(out _);
        draft.UpdateStep(secondStepId, "Abertura", "Descricao duplicada", 2);
        draft.TryDequeueEvent(out _);

        var applicationService = new Mock<IWorkflowTemplateApplicationService>();

        applicationService
            .Setup(x => x.LoadAggregateAsync<WorkflowTemplateAggregate>(draft.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(draft));

        applicationService
            .Setup(x => x.LoadAggregateAsync<WorkflowTemplateAggregate>(source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(source));

        var handler = new PublishWorkflowTemplateHandler(applicationService.Object);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(new PublishWorkflowTemplateCommand(draft.Id), CancellationToken.None));

        Assert.Contains("Duplicate step titles detected", exception.Message);
        applicationService.Verify(
            x => x.AppendEventsAsync(It.IsAny<WorkflowTemplateAggregate>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDraftContainsDuplicatedTaskTitlesInTheSameStep_ThrowsArgumentException()
    {
        var source = WorkflowTemplateAggregate.Create("Active Template", "Descricao");
        source.TryDequeueEvent(out _);
        var sourceStepId = source.AddStep("Abertura", "Descricao", 1);
        source.TryDequeueEvent(out _);
        source.Activate();
        source.TryDequeueEvent(out _);

        var draft = WorkflowTemplateAggregate.Create("Draft Template", "Descricao", source.Id);
        draft.TryDequeueEvent(out _);
        var stepId = draft.AddStep("Abertura", "Descricao", 1);
        draft.TryDequeueEvent(out _);

        _ = draft.AddTask(stepId, "Checklist", "Descricao", TaskType.Manual, string.Empty, 1, false);
        var secondTaskId = draft.AddTask(stepId, "Documento", "Descricao", TaskType.Manual, string.Empty, 2, false);
        draft.TryDequeueEvent(out _);
        draft.TryDequeueEvent(out _);
        draft.UpdateTask(stepId, secondTaskId, "Checklist", "Duplicada", TaskType.Manual, string.Empty, 2, false);
        draft.TryDequeueEvent(out _);

        var applicationService = new Mock<IWorkflowTemplateApplicationService>();

        applicationService
            .Setup(x => x.LoadAggregateAsync<WorkflowTemplateAggregate>(draft.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(draft));

        applicationService
            .Setup(x => x.LoadAggregateAsync<WorkflowTemplateAggregate>(source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(source));

        var handler = new PublishWorkflowTemplateHandler(applicationService.Object);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(new PublishWorkflowTemplateCommand(draft.Id), CancellationToken.None));

        Assert.Contains("Duplicate task titles detected", exception.Message);
        applicationService.Verify(
            x => x.AppendEventsAsync(It.IsAny<WorkflowTemplateAggregate>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
