using Core.Shared.Results;
using Microsoft.AspNetCore.Mvc;

namespace Core.Endpoints.Extensions;

public static class ResultExtensions
{
    public static async Task<ActionResult> Match(this Task<Result> resultTask, Func<ActionResult> onSuccess, Func<Result, ActionResult> onFailure)
    {
        Result result = await resultTask;

        return result.IsSuccess ? onSuccess() : onFailure(result);
    }

    public static async Task<ActionResult> Match<TResult>(
            this Task<Result<TResult>> resultTask,
            Func<TResult, ActionResult> onSuccess,
            Func<Result, ActionResult> onFailure)
    {
        Result<TResult> result = await resultTask;

        return result.IsSuccess ? onSuccess(result.Value) : onFailure(result);
    }
}