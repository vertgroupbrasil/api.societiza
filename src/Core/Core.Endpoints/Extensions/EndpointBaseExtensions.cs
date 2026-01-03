using Ardalis.ApiEndpoints;
using Core.Shared.Errors;
using Core.Shared.Results;
using Core.Shared.Results.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Core.Endpoints.Extensions;

public static class EndpointBaseExtensions
{
    public static ActionResult HandleFailure(this EndpointBase endpoint, Result result) =>
            result switch
            {
                { IsSuccess: true } => throw new InvalidOperationException("This method can't be invoked for a success result."),
                IValidationResult validationResult =>
                    endpoint.BadRequest(
                        CreateProblemDetails(
                            "Validation Error", StatusCodes.Status400BadRequest, IValidationResult.ValidationError, validationResult.Errors)),
                var notFound when notFound.Error is NotFoundError =>
                    endpoint.NotFound(CreateProblemDetails("Not Found", StatusCodes.Status404NotFound, notFound.Error)),
                var conflict when conflict.Error is ConflictError =>
                    endpoint.Conflict(CreateProblemDetails("Conflict", StatusCodes.Status409Conflict, conflict.Error)),
                var badRequest =>
                    endpoint.BadRequest(CreateProblemDetails("Bad Request", StatusCodes.Status400BadRequest, badRequest.Error))
            };

    private static ProblemDetails CreateProblemDetails(string title, int status, Error error, Error[]? errors = null) =>
        new()
        {
            Title = title,
            Type = error.Code,
            Detail = error.Message,
            Status = status,
            Extensions = { { nameof(errors), errors } }
        };
}