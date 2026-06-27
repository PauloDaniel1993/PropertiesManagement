using Alsappan.Application.Payments;
using Alsappan.Application.Payments.Providers;
using Alsappan.Infrastructure.Payments;

namespace Alsappan.Infrastructure.Tests.Payments;

public sealed class MockPaymentInstructionProviderTests
{
  public static TheoryData<IPaymentInstructionProvider, string, string> Providers => new()
  {
    { new MockBoletoPaymentInstructionProvider(), "mock-boleto", "boleto" },
    { new MockPixPaymentInstructionProvider(), "mock-pix", "pix" },
    { new MockPayPalPaymentInstructionProvider(), "mock-paypal", "paypal" }
  };

  [Theory]
  [MemberData(nameof(Providers))]
  public async Task CreateInstructionAsyncUsesStableMockProviderContract(
    IPaymentInstructionProvider provider,
    string providerCode,
    string kind)
  {
    ArgumentNullException.ThrowIfNull(provider);

    var chargeId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    var instruction = await provider.CreateInstructionAsync(new PaymentProviderInstructionRequest(
      chargeId,
      providerCode,
      provider.Kind,
      new PaymentMoneyDto(1234.56m, "BRL"),
      new DateOnly(2026, 6, 30),
      "Joao da Silva",
      "pt-BR"));

    Assert.Equal(chargeId, instruction.ChargeId);
    Assert.Equal(providerCode, instruction.ProviderCode);
    Assert.StartsWith(providerCode, instruction.ProviderReference, StringComparison.Ordinal);
    Assert.Equal(kind, instruction.Kind);
    Assert.Equal("true", instruction.Metadata["mock"]);
    Assert.Equal(providerCode, instruction.Metadata["provider"]);
    Assert.Equal(kind, instruction.Metadata["kind"]);
    Assert.Equal("Joao da Silva", instruction.PayerSummary);
  }

  [Fact]
  public async Task ProviderSpecificFieldsRemainAvailableForResidentDisplay()
  {
    var chargeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    var request = new PaymentProviderInstructionRequest(
      chargeId,
      "mock-pix",
      default,
      new PaymentMoneyDto(500m, "BRL"),
      new DateOnly(2026, 7, 5),
      "Maria Resident",
      "en-US");

    var boleto = await new MockBoletoPaymentInstructionProvider().CreateInstructionAsync(
      request with { ProviderCode = "mock-boleto" });
    var pix = await new MockPixPaymentInstructionProvider().CreateInstructionAsync(request);
    var paypal = await new MockPayPalPaymentInstructionProvider().CreateInstructionAsync(
      request with { ProviderCode = "mock-paypal" });

    Assert.False(string.IsNullOrWhiteSpace(boleto.Barcode));
    Assert.False(string.IsNullOrWhiteSpace(boleto.LinhaDigitavel));
    Assert.False(string.IsNullOrWhiteSpace(pix.QrPayload));
    Assert.False(string.IsNullOrWhiteSpace(pix.CopyPasteCode));
    Assert.False(string.IsNullOrWhiteSpace(paypal.PaymentIntentId));
    Assert.False(string.IsNullOrWhiteSpace(paypal.ApprovalUrl));
  }
}
