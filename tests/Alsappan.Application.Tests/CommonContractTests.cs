using System.Text.Json;
using System.Globalization;
using Alsappan.Application.Common.Contracts;

namespace Alsappan.Application.Tests;

public sealed class CommonContractTests
{
  private static readonly JsonSerializerOptions CamelCaseJson = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
  };

  [Fact]
  public void PagedResultComputesNavigationMetadata()
  {
    var result = new PagedResultDto<string>(["a", "b"], page: 2, pageSize: 2, totalItems: 5);

    Assert.Equal(3, result.TotalPages);
    Assert.True(result.HasPreviousPage);
    Assert.True(result.HasNextPage);
  }

  [Fact]
  public void ListFilterNormalizesSearchAndCalculatesOffset()
  {
    var filter = new ListFilterDto(page: 3, pageSize: 20, search: " Calabria ", sort: " name ");

    Assert.Equal("Calabria", filter.Search);
    Assert.Equal("name", filter.Sort);
    Assert.Equal(40, filter.Offset);
  }

  [Fact]
  public void ListFilterRejectsInvalidDateWindow()
  {
    Assert.Throws<ArgumentException>(() =>
      new ListFilterDto(from: new DateOnly(2026, 6, 30), to: new DateOnly(2026, 6, 1)));
  }

  [Fact]
  public void SelectOptionAndStatusLabelNormalizeDisplayContracts()
  {
    var option = new SelectOptionDto(" available ", " Disponivel ");
    var status = new StatusLabelDto(" Available ", " Disponivel ", StatusLabelTones.Success);

    Assert.Equal("available", option.Value);
    Assert.Equal("Disponivel", option.Label);
    Assert.Equal("available", status.Code);
    Assert.Equal("success", status.Tone);
  }

  [Fact]
  public void AuditMetadataTracksDeletionAndConcurrencyToken()
  {
    var createdAt = Timestamp("2026-06-01T10:00:00Z");
    var deletedAt = Timestamp("2026-06-02T10:00:00Z");

    var metadata = new AuditMetadataDto(createdAt, deletedAt: deletedAt, concurrencyToken: " token ");

    Assert.True(metadata.IsDeleted);
    Assert.Equal("token", metadata.ConcurrencyToken);
  }

  [Fact]
  public void ContractsSerializeWithCamelCaseNames()
  {
    var result = new PagedResultDto<SelectOptionDto>(
      [new SelectOptionDto("available", "Disponivel")],
      page: 1,
      pageSize: 20,
      totalItems: 1);

    var json = JsonSerializer.Serialize(result, CamelCaseJson);

    Assert.Contains("\"items\"", json, StringComparison.Ordinal);
    Assert.Contains("\"label\":\"Disponivel\"", json, StringComparison.Ordinal);
  }

  private static DateTimeOffset Timestamp(string value) =>
    DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
}
