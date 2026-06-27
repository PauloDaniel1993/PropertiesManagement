using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Alsappan.Application.Common.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Alsappan.Api.Tests;

#pragma warning disable CA1812

public sealed partial class ApiAuthorizationCoverageTests : IClassFixture<WebApplicationFactory<Program>>
{
  private static readonly Guid UserId = new("22222222-2222-2222-2222-222222222222");
  private static readonly Guid EntityId = new("33333333-3333-3333-3333-333333333333");
  private static readonly Guid RelatedEntityId = new("44444444-4444-4444-4444-444444444444");

  private static readonly HashSet<string> PublicIdentityEndpointNames = new(StringComparer.Ordinal)
  {
    "Identity_AdminLogin",
    "Identity_ResidentLogin",
    "Identity_RefreshSession",
    "Identity_Logout"
  };

  private readonly WebApplicationFactory<Program> factory;

  public ApiAuthorizationCoverageTests(WebApplicationFactory<Program> factory)
  {
    this.factory = factory;
  }

  [Fact]
  public void AllModuleEndpointsDeclareAuthenticationUnlessExplicitlyPublicIdentity()
  {
    _ = factory.CreateClient();

    var failures = GetVersionedModuleEndpoints()
      .Where(endpoint => !IsAuthorized(endpoint) && !IsAllowedPublicIdentityEndpoint(endpoint))
      .Select(Describe)
      .ToArray();

    Assert.Empty(failures);
  }

  [Fact]
  public void OnlyLoginRefreshAndLogoutIdentityEndpointsAllowAnonymousAccess()
  {
    _ = factory.CreateClient();

    var anonymousEndpointNames = GetVersionedModuleEndpoints()
      .Where(endpoint => endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
      .Select(GetEndpointName)
      .ToHashSet(StringComparer.Ordinal);

    var failures = GetVersionedModuleEndpoints()
      .Where(endpoint => anonymousEndpointNames.Contains(GetEndpointName(endpoint)))
      .Where(endpoint => !PublicIdentityEndpointNames.Contains(GetEndpointName(endpoint)))
      .Select(Describe)
      .ToArray();
    var missingPublicEndpoints = PublicIdentityEndpointNames
      .Except(anonymousEndpointNames, StringComparer.Ordinal)
      .ToArray();

    Assert.Empty(failures);
    Assert.Empty(missingPublicEndpoints);
  }

  [Fact]
  public async Task ProtectedModuleEndpointsRejectAnonymousRequests()
  {
    using var client = factory.CreateClient();
    var endpoints = GetVersionedModuleEndpoints()
      .Where(endpoint => endpoint.Metadata.GetMetadata<IAllowAnonymous>() is null)
      .OrderBy(GetEndpointName, StringComparer.Ordinal)
      .ToArray();

    var failures = new List<string>();
    foreach (var endpoint in endpoints)
    {
      foreach (var method in GetHttpMethods(endpoint))
      {
        using var request = new HttpRequestMessage(new HttpMethod(method), BuildProbePath(endpoint));
        using var response = await client.SendAsync(request);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
          failures.Add($"{Describe(endpoint)} returned {(int)response.StatusCode} {response.StatusCode}");
        }
      }
    }

    Assert.Empty(failures);
  }

  [Theory]
  [MemberData(nameof(ForbiddenEndpointProbes))]
  public async Task RepresentativeModuleEndpointsRejectAuthenticatedUsersWithoutAuthorization(
    AuthorizationProbe probe)
  {
    ArgumentNullException.ThrowIfNull(probe);

    using var client = factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new("Bearer", CreateJwtWithoutOrganizationPermissions());

    using var request = probe.CreateRequest();
    using var response = await client.SendAsync(request);

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  public static TheoryData<AuthorizationProbe> ForbiddenEndpointProbes()
  {
    var probes = new TheoryData<AuthorizationProbe>
    {
      Read("Administrators list", "/v1/administrators"),
      Write("Administrators create", "/v1/administrators", AdministratorCreateJson),
      Read("Properties list", "/v1/properties"),
      Write("Properties create", "/v1/properties", PropertyJson),
      Write("Properties status", $"/v1/properties/{EntityId}/status", """{"status":"maintenance"}"""),
      Read("Residents list", "/v1/residents"),
      Write("Residents create", "/v1/residents", ResidentJson),
      Write("Residents portal invite", $"/v1/residents/{EntityId}/portal-account/invite", """{"email":"resident@example.test","temporaryPassword":"Temp!23456"}"""),
      Read("Contracts list", "/v1/contracts"),
      Write("Contracts create", "/v1/contracts", ContractJson),
      Write("Contracts activate", $"/v1/contracts/{EntityId}/activate", LifecycleJson),
      Read("Documents list", "/v1/documents"),
      Delete("Documents archive", $"/v1/documents/{EntityId}"),
      Read("Payments list", "/v1/payments"),
      Write("Payments create", "/v1/payments", PaymentJson),
      Write("Payments instruction", $"/v1/payments/{EntityId}/instructions", """{"providerCode":"mock-pix"}"""),
      Read("Utility accounts list", "/v1/utility-accounts"),
      Write("Utility accounts create", "/v1/utility-accounts", UtilityAccountJson),
      Write("Utility accounts mark paid", $"/v1/utility-accounts/{EntityId}/mark-paid", UtilityMarkPaidJson),
      Read("Pets list", "/v1/pets"),
      Write("Pets create", "/v1/pets", PetJson),
      Write("Pets authorize", $"/v1/pets/{EntityId}/authorize", LifecycleJson),
      Read("Vehicles list", "/v1/vehicles"),
      Write("Vehicles create", "/v1/vehicles", VehicleJson),
      Write("Vehicles authorize", $"/v1/vehicles/{EntityId}/authorize", LifecycleJson),
      Read("Occurrences list", "/v1/occurrences"),
      Write("Occurrences create", "/v1/occurrences", OccurrenceJson),
      Write("Occurrences resolve", $"/v1/occurrences/{EntityId}/resolve", """{"resolutionNotes":"Resolved in authorization probe."}"""),
      Read("Inspections list", "/v1/inspections"),
      Write("Inspections schedule", "/v1/inspections", InspectionJson),
      Write("Inspections checklist item", $"/v1/inspections/{EntityId}/checklist-items", InspectionChecklistItemJson),
      Read("Notifications list", "/v1/notifications"),
      Write("Notifications preferences", "/v1/notifications/preferences", NotificationPreferencesJson, HttpMethod.Put),
      Read("Timeline list", "/v1/timeline"),
      Read("Audit list", "/v1/audit"),
      Read("Settings dashboard", "/v1/settings"),
      Write("Settings organization profile", "/v1/settings/organization", OrganizationProfileJson, HttpMethod.Put),
      Write("Branding update", "/v1/settings/branding", BrandingJson, HttpMethod.Put),
      Write("Branding reset", "/v1/settings/branding/reset", "{}"),
      Read("Dashboard overview", "/v1/dashboard"),
      Read("Global search", "/v1/search?query=calabria"),
      Read("Resident portal summary", "/v1/resident-portal/summary"),
      Write("Resident portal occurrence", "/v1/resident-portal/occurrences", ResidentPortalOccurrenceJson),
      Write("Resident portal payment instruction", $"/v1/resident-portal/payments/{EntityId}/instructions", """{"providerCode":"mock-boleto"}""")
    };

    return probes;
  }

  private RouteEndpoint[] GetVersionedModuleEndpoints()
  {
    var dataSource = factory.Services.GetRequiredService<EndpointDataSource>();
    return dataSource.Endpoints
      .OfType<RouteEndpoint>()
      .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith("/v1/", StringComparison.Ordinal) == true)
      .Where(endpoint => !HasTag(endpoint, "System"))
      .ToArray();
  }

  private static bool IsAuthorized(Endpoint endpoint) =>
    endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Count > 0;

  private static bool IsAllowedPublicIdentityEndpoint(Endpoint endpoint) =>
    endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null &&
    PublicIdentityEndpointNames.Contains(GetEndpointName(endpoint));

  private static string GetEndpointName(Endpoint endpoint) =>
    endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName ??
    endpoint.DisplayName ??
    "<unnamed>";

  private static string[] GetHttpMethods(Endpoint endpoint) =>
    endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.ToArray() ?? ["GET"];

  private static bool HasTag(Endpoint endpoint, string tag) =>
    endpoint.Metadata.GetMetadata<ITagsMetadata>()?.Tags.Contains(tag, StringComparer.Ordinal) == true;

  private static string Describe(RouteEndpoint endpoint) =>
    $"{GetEndpointName(endpoint)} [{string.Join(", ", GetHttpMethods(endpoint))}] {endpoint.RoutePattern.RawText}";

  private static string BuildProbePath(RouteEndpoint endpoint)
  {
    var rawText = endpoint.RoutePattern.RawText ??
      throw new InvalidOperationException($"Endpoint {GetEndpointName(endpoint)} does not have a raw route pattern.");

    return RouteParameterRegex().Replace(rawText, match =>
    {
      var name = match.Groups["name"].Value;
      return name switch
      {
        "catalogType" => "property-types",
        "documentName" => "v1",
        "entityType" => "property",
        "versionNumber" => "1",
        _ when match.Value.Contains(":int", StringComparison.Ordinal) => "1",
        _ => EntityId.ToString()
      };
    });
  }

  private static AuthorizationProbe Read(string name, string path) =>
    new(name, HttpMethod.Get, path, null);

  private static AuthorizationProbe Write(
    string name,
    string path,
    string json,
    HttpMethod? method = null) =>
    new(name, method ?? HttpMethod.Post, path, () => new StringContent(json, Encoding.UTF8, "application/json"));

  private static AuthorizationProbe Delete(string name, string path) =>
    new(name, HttpMethod.Delete, path, null);

  private static string CreateJwtWithoutOrganizationPermissions()
  {
    var organizationId = Guid.NewGuid();
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
      "replace-this-dev-only-signing-key-with-at-least-32-characters"));
    var token = new JwtSecurityToken(
      issuer: "Alsappan",
      audience: "Alsappan.Web",
      claims:
      [
        new Claim(JwtRegisteredClaimNames.Sub, UserId.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, "authorization-probe@alsappan.local"),
        new Claim(
          AuthClaimTypes.Membership,
          JsonSerializer.Serialize(
            new OrganizationMembershipClaimDto(organizationId, [], [], true)))
      ],
      expires: DateTime.UtcNow.AddMinutes(15),
      signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

    return new JwtSecurityTokenHandler().WriteToken(token);
  }

  private const string LifecycleJson = """{"notes":"Authorization probe"}""";

  private const string AdministratorCreateJson = """
    {
      "email": "operator@example.test",
      "displayName": "Operator Test",
      "roleCodes": ["organization.staff"],
      "temporaryPassword": "Temp!23456"
    }
    """;

  private const string PropertyJson = """
    {
      "name": "Authorization Probe Property",
      "type": "house",
      "description": "Test property",
      "address": {
        "streetLine": "Rua Calabria",
        "number": "82",
        "complement": "Casa 1",
        "neighborhood": "Centro",
        "city": "Sao Paulo",
        "stateCode": "SP",
        "postalCode": "00000-000"
      },
      "status": "available",
      "suggestedRent": { "amount": 700, "currency": "BRL" },
      "garageSpaceCount": 1,
      "garageSpaceIdentifiers": "A1",
      "notes": "Authorization probe"
    }
    """;

  private const string ResidentJson = """
    {
      "fullName": "Resident Probe",
      "preferredName": "Probe",
      "email": "resident@example.test",
      "phone": "+5511999999999",
      "secondaryPhone": null,
      "documentType": "cpf",
      "documentIdentifier": "12345678900",
      "birthDate": "1990-01-01",
      "emergencyContact": {
        "name": "Emergency Contact",
        "relationship": "Family",
        "phone": "+5511888888888"
      },
      "status": "active",
      "portalStatus": "inactive",
      "privacyFlags": [],
      "notes": "Authorization probe",
      "linkedUserId": null
    }
    """;

  private static readonly string ContractJson = $$"""
    {
      "propertyId": "{{EntityId}}",
      "primaryResidentId": "{{RelatedEntityId}}",
      "residentIds": ["{{RelatedEntityId}}"],
      "startDate": "2026-07-01",
      "endDate": "2027-06-30",
      "monthlyRent": { "amount": 1200, "currency": "BRL" },
      "dueDay": 10,
      "depositAmount": { "amount": 1200, "currency": "BRL" },
      "adjustmentIndex": "ipca",
      "adjustmentIntervalMonths": 12,
      "nextAdjustmentDate": "2027-07-01",
      "penaltyNotes": null,
      "discountNotes": null,
      "generatePaymentsAutomatically": true,
      "notes": "Authorization probe",
      "lifecycleAction": "draft"
    }
    """;

  private static readonly string PaymentJson = $$"""
    {
      "title": "Rent charge",
      "description": "Authorization probe",
      "contractId": "{{EntityId}}",
      "propertyId": "{{RelatedEntityId}}",
      "residentId": "{{RelatedEntityId}}",
      "utilityAccountId": null,
      "dueDate": "2026-07-10",
      "amount": { "amount": 1200, "currency": "BRL" },
      "discountAmount": { "amount": 0, "currency": "BRL" },
      "penaltyAmount": { "amount": 0, "currency": "BRL" },
      "preferredMethod": "pix",
      "reconciliationStatus": "pending",
      "notes": "Authorization probe"
    }
    """;

  private static readonly string UtilityAccountJson = $$"""
    {
      "title": "Electricity bill",
      "description": "Authorization probe",
      "type": "electricity",
      "responsibility": "organization",
      "propertyId": "{{EntityId}}",
      "contractId": null,
      "residentId": null,
      "billingPeriodStart": "2026-06-01",
      "billingPeriodEnd": "2026-06-30",
      "dueDate": "2026-07-10",
      "amount": { "amount": 250, "currency": "BRL" },
      "billDocumentId": null,
      "notes": "Authorization probe"
    }
    """;

  private const string UtilityMarkPaidJson = """
    {
      "amount": { "amount": 250, "currency": "BRL" },
      "paidOn": "2026-07-10",
      "paymentMethod": "pix",
      "bankReference": "AUTH-PROBE",
      "receiptDocumentId": null,
      "notes": "Authorization probe"
    }
    """;

  private static readonly string PetJson = $$"""
    {
      "residentId": "{{EntityId}}",
      "propertyId": "{{RelatedEntityId}}",
      "contractId": null,
      "name": "Probe Pet",
      "species": "dog",
      "breed": "Mixed",
      "authorizationStatus": "pending",
      "authorizationNotes": "Authorization probe",
      "vaccinationRecordDocumentId": null,
      "authorizationFormDocumentId": null,
      "notes": "Authorization probe"
    }
    """;

  private static readonly string VehicleJson = $$"""
    {
      "residentId": "{{EntityId}}",
      "propertyId": "{{RelatedEntityId}}",
      "contractId": null,
      "plate": "ABC1D23",
      "type": "car",
      "color": "White",
      "brand": "Test",
      "model": "Probe",
      "year": 2024,
      "authorizationStatus": "pending",
      "parkingSpaceIdentifier": "A1",
      "parkingAllocationNotes": "Authorization probe",
      "notes": "Authorization probe"
    }
    """;

  private static readonly string OccurrenceJson = $$"""
    {
      "title": "Leak report",
      "description": "Authorization probe",
      "type": "maintenance",
      "priority": "medium",
      "propertyId": "{{EntityId}}",
      "residentId": "{{RelatedEntityId}}",
      "contractId": null,
      "assignedUserId": null,
      "dueDate": "2026-07-15"
    }
    """;

  private static readonly string InspectionJson = $$"""
    {
      "type": "move-in",
      "propertyId": "{{EntityId}}",
      "contractId": null,
      "residentId": "{{RelatedEntityId}}",
      "scheduledAt": "2026-07-15T10:00:00+00:00",
      "assignedUserId": "{{UserId}}",
      "title": "Move-in inspection",
      "notes": "Authorization probe",
      "signatureSlots": [
        {
          "signerRole": "resident",
          "signerName": "Resident Probe",
          "isRequired": false
        }
      ]
    }
    """;

  private const string InspectionChecklistItemJson = """
    {
      "areaName": "Kitchen",
      "itemName": "Sink",
      "isRequired": true,
      "conditionRating": "good",
      "observations": "Authorization probe",
      "sortOrder": 1
    }
    """;

  private const string NotificationPreferencesJson = """
    {
      "preferences": [
        {
          "category": "occurrences",
          "channel": "in-app",
          "isEnabled": true
        }
      ]
    }
    """;

  private const string OrganizationProfileJson = """
    {
      "slug": "alsappan",
      "name": "Alsappan",
      "displayName": "Alsappan",
      "currencyCode": "BRL",
      "timeZone": "America/Sao_Paulo",
      "contactEmail": "support@example.test",
      "contactPhone": "+5511999999999",
      "contactWebsite": "https://example.test"
    }
    """;

  private const string BrandingJson = """
    {
      "displayName": "Alsappan",
      "logoUrl": null,
      "logoAlt": "Alsappan",
      "primaryColor": "#155EEF",
      "primaryForegroundColor": "#FFFFFF",
      "accentColor": "#12B76A",
      "accentForegroundColor": "#062C1D",
      "supportEmail": "support@example.test",
      "supportPhone": "+5511999999999",
      "supportUrl": "https://example.test"
    }
    """;

  private static readonly string ResidentPortalOccurrenceJson = $$"""
    {
      "title": "Resident occurrence",
      "description": "Authorization probe",
      "type": "maintenance",
      "priority": "medium",
      "propertyId": "{{EntityId}}",
      "contractId": null,
      "dueDate": "2026-07-15"
    }
    """;

  [GeneratedRegex(@"\{(?<name>[^}:]+)(?::[^}]+)?\}", RegexOptions.CultureInvariant)]
  private static partial Regex RouteParameterRegex();
}

#pragma warning disable CA1515
public sealed record AuthorizationProbe(
  string Name,
  HttpMethod Method,
  string Path,
  Func<HttpContent>? ContentFactory)
{
  public HttpRequestMessage CreateRequest()
  {
    var request = new HttpRequestMessage(Method, Path);
    if (ContentFactory is not null)
    {
      request.Content = ContentFactory();
    }

    return request;
  }

  public override string ToString() => Name;
}
#pragma warning restore CA1515

#pragma warning restore CA1812
