using Alsappan.Application.Payments;
using Alsappan.Application.Payments.Providers;
using Alsappan.Application.Payments.Repositories;
using Alsappan.Infrastructure.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace Alsappan.Infrastructure.Payments;

#pragma warning disable CA1812
internal sealed class PaymentInfrastructureModule : IInfrastructureModule
{
  public int Order => 350;

  public void AddServices(IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);

    services.AddScoped<IPaymentRepository, EfPaymentRepository>();
    services.AddScoped<IPaymentService, PaymentService>();
    services.AddScoped<IPaymentInstructionProvider, MockBoletoPaymentInstructionProvider>();
    services.AddScoped<IPaymentInstructionProvider, MockPixPaymentInstructionProvider>();
    services.AddScoped<IPaymentInstructionProvider, MockPayPalPaymentInstructionProvider>();
  }
}
