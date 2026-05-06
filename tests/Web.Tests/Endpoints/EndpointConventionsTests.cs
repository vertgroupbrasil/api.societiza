using System.Reflection;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Web.Tests.Endpoints;

/// <summary>
/// Testes de convenção que validam o guia de estilo de API do Societiza
/// (veja <c>.claude/skills/architecture/references/web-swagger.md</c>) via reflection.
///
/// Todo endpoint público Ardalis em <c>Web.Endpoints.Modules.*</c> deve:
/// - declarar <see cref="ApiVersionAttribute"/> no método <c>HandleAsync</c>;
/// - declarar <see cref="SwaggerOperationAttribute"/> no método <c>HandleAsync</c>
///   com <c>OperationId</c>, <c>Summary</c> (≤ 60 chars), <c>Description</c> e
///   exatamente uma entrada <c>Tags</c> com prefixo de módulo reconhecido em <c>Tags.cs</c>;
/// - declarar um <see cref="ProducesResponseTypeAttribute"/> de sucesso (200/201/204)
///   no método <c>HandleAsync</c>;
/// - quando decorado com <see cref="AuthorizeAttribute"/> na classe, também declarar
///   <c>ProducesResponseType(401)</c> e <c>ProducesResponseType(403)</c> no método.
///
/// <c>OperationId</c> deve ser globalmente único entre todos os endpoints.
/// </summary>
public sealed class EndpointConventionsTests
{
    private static readonly string[] AllowedTagPrefixes =
    {
        "Accountancy",
        "Identity / ",
        "Workflow Templates / ",
        "Workflow Process / ",
    };

    private static readonly int[] SuccessStatusCodes = { 200, 201, 204 };

    private static IEnumerable<Type> EndpointTypes()
    {
        var webAssembly = typeof(global::Web.Endpoints.Modules.Accountancy.CreateAccountancyEndpoint).Assembly;

        return webAssembly
            .GetTypes()
            .Where(t =>
                t is { IsClass: true, IsAbstract: false } &&
                t.Namespace is not null &&
                t.Namespace.StartsWith("Web.Endpoints.Modules", StringComparison.Ordinal) &&
                t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .Any(m => m.Name == "HandleAsync"));
    }

    /// <summary>
    /// Retorna o método HandleAsync declarado diretamente na classe endpoint.
    /// É nele que ficam [ApiVersion], [Http*], [ProducesResponseType] e [SwaggerOperation].
    /// </summary>
    private static MethodInfo GetHandleAsync(Type t)
        => t.GetMethod("HandleAsync",
               BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)!;

    public static IEnumerable<object[]> EndpointTestData()
        => EndpointTypes().Select(t => new object[] { t });

    [Theory]
    [MemberData(nameof(EndpointTestData))]
    public void Endpoint_has_ApiVersion_attribute(Type endpointType)
    {
        var apiVersion = GetHandleAsync(endpointType).GetCustomAttribute<ApiVersionAttribute>();
        Assert.NotNull(apiVersion);
    }

    [Theory]
    [MemberData(nameof(EndpointTestData))]
    public void Endpoint_has_SwaggerOperation_with_required_fields(Type endpointType)
    {
        var swagger = GetHandleAsync(endpointType).GetCustomAttribute<SwaggerOperationAttribute>();
        Assert.NotNull(swagger);

        Assert.False(string.IsNullOrWhiteSpace(swagger!.OperationId),
            $"{endpointType.Name} must declare a non-empty OperationId.");
        Assert.False(string.IsNullOrWhiteSpace(swagger.Summary),
            $"{endpointType.Name} must declare a non-empty Summary.");
        Assert.False(string.IsNullOrWhiteSpace(swagger.Description),
            $"{endpointType.Name} must declare a non-empty Description.");
        Assert.NotNull(swagger.Tags);
        Assert.Single(swagger.Tags!);
    }

    [Theory]
    [MemberData(nameof(EndpointTestData))]
    public void Endpoint_summary_is_short(Type endpointType)
    {
        var swagger = GetHandleAsync(endpointType).GetCustomAttribute<SwaggerOperationAttribute>();
        Assert.NotNull(swagger);
        Assert.True(swagger!.Summary!.Length <= 60,
            $"{endpointType.Name} Summary is {swagger.Summary.Length} chars (max 60): \"{swagger.Summary}\".");
    }

    [Theory]
    [MemberData(nameof(EndpointTestData))]
    public void Endpoint_tag_matches_known_prefix(Type endpointType)
    {
        var swagger = GetHandleAsync(endpointType).GetCustomAttribute<SwaggerOperationAttribute>();
        Assert.NotNull(swagger);
        var tag = swagger!.Tags![0];

        Assert.True(
            AllowedTagPrefixes.Any(p => tag.StartsWith(p, StringComparison.Ordinal)),
            $"{endpointType.Name} tag \"{tag}\" does not match any allowed prefix ({string.Join(", ", AllowedTagPrefixes)}).");
    }

    [Theory]
    [MemberData(nameof(EndpointTestData))]
    public void Endpoint_declares_success_response(Type endpointType)
    {
        var hasSuccess = GetHandleAsync(endpointType)
            .GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Any(attr => SuccessStatusCodes.Contains(attr.StatusCode));

        Assert.True(hasSuccess,
            $"{endpointType.Name} must declare a success ProducesResponseType (200, 201, or 204).");
    }

    [Theory]
    [MemberData(nameof(EndpointTestData))]
    public void Authorized_endpoint_declares_401_and_403(Type endpointType)
    {
        // [Authorize] fica na classe; [ProducesResponseType] fica no método HandleAsync.
        var requiresAuth = endpointType.GetCustomAttribute<AuthorizeAttribute>() is not null;
        if (!requiresAuth)
        {
            return;
        }

        var statuses = GetHandleAsync(endpointType)
            .GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Select(a => a.StatusCode)
            .ToHashSet();

        Assert.Contains(401, statuses);
        Assert.Contains(403, statuses);
    }

    [Fact]
    public void OperationIds_are_unique()
    {
        var duplicates = EndpointTypes()
            .Select(t => new
            {
                Type = t,
                OperationId = GetHandleAsync(t).GetCustomAttribute<SwaggerOperationAttribute>()?.OperationId,
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.OperationId))
            .GroupBy(x => x.OperationId!)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key}: {string.Join(", ", g.Select(x => x.Type.Name))}")
            .ToArray();

        Assert.True(duplicates.Length == 0,
            $"Duplicate OperationId values:\n{string.Join("\n", duplicates)}");
    }
}
