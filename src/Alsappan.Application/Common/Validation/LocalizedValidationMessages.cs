namespace Alsappan.Application.Common.Validation;

public sealed class LocalizedValidationMessages
{
  private readonly Dictionary<string, IReadOnlyDictionary<string, string>> messages = CreateMessages();

  public string Resolve(string messageKey, string? locale)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(messageKey);

    var culture = !string.IsNullOrWhiteSpace(locale) &&
      locale.StartsWith("en", StringComparison.OrdinalIgnoreCase)
        ? "en-US"
        : "pt-BR";

    if (messages.TryGetValue(messageKey, out var localizedMessages) &&
      localizedMessages.TryGetValue(culture, out var message))
    {
      return message;
    }

    return messageKey;
  }

  private static Dictionary<string, IReadOnlyDictionary<string, string>> CreateMessages() =>
    new(StringComparer.Ordinal)
    {
      ["validation"] = Localized(
        "A validação falhou.",
        "Validation failed."),
      [ValidationMessageKeys.CurrencyCode] = Localized(
        "Informe uma moeda ISO válida com três letras.",
        "Enter a valid three-letter ISO currency."),
      [ValidationMessageKeys.DateRange] = Localized(
        "A data final deve ser igual ou posterior à data inicial.",
        "The end date must be the same as or after the start date."),
      [ValidationMessageKeys.Email] = Localized(
        "Informe um e-mail válido.",
        "Enter a valid email address."),
      [ValidationMessageKeys.InvalidDate] = Localized(
        "Informe uma data válida no formato AAAA-MM-DD.",
        "Enter a valid date in YYYY-MM-DD format."),
      [ValidationMessageKeys.InvalidId] = Localized(
        "Informe um identificador válido.",
        "Enter a valid identifier."),
      [ValidationMessageKeys.MaxLength] = Localized(
        "O campo excede o tamanho máximo permitido.",
        "The field exceeds the maximum allowed length."),
      [ValidationMessageKeys.MinValue] = Localized(
        "O valor deve ser maior ou igual ao mínimo permitido.",
        "The value must be greater than or equal to the minimum allowed."),
      [ValidationMessageKeys.Required] = Localized(
        "Campo obrigatório.",
        "Required field."),
      [ValidationMessageKeys.Unsupported] = Localized(
        "Valor ainda não suportado.",
        "Value is not supported yet."),
      ["validation.activeContract"] = Localized(
        "Selecione um contrato ativo.",
        "Select an active contract."),
      ["validation.adjustmentIndex"] = Localized(
        "Informe um índice de reajuste válido.",
        "Enter a valid adjustment index."),
      ["validation.adjustmentInterval"] = Localized(
        "Informe um intervalo de reajuste válido.",
        "Enter a valid adjustment interval."),
      ["validation.assignedUser"] = Localized(
        "Informe um responsável válido.",
        "Enter a valid assigned user."),
      ["validation.assignee"] = Localized(
        "Informe um responsável válido.",
        "Enter a valid assignee."),
      ["validation.billingPeriod"] = Localized(
        "Informe um período de cobrança válido.",
        "Enter a valid billing period."),
      ["validation.branding"] = Localized(
        "Informe uma configuração de marca válida.",
        "Enter valid branding settings."),
      ["validation.branding.colorContrast"] = Localized(
        "As cores de marca precisam ter contraste suficiente.",
        "Brand colors must have enough contrast."),
      ["validation.branding.colorHex"] = Localized(
        "Informe uma cor hexadecimal válida.",
        "Enter a valid hexadecimal color."),
      ["validation.branding.logoAltRequired"] = Localized(
        "Informe um texto alternativo para o logotipo.",
        "Enter alternative text for the logo."),
      ["validation.branding.logoDimension"] = Localized(
        "Informe dimensões válidas para o logotipo.",
        "Enter valid logo dimensions."),
      ["validation.branding.logoMimeType"] = Localized(
        "Informe um tipo de arquivo de logotipo válido.",
        "Enter a valid logo file type."),
      ["validation.branding.logoSize"] = Localized(
        "O logotipo excede o tamanho máximo permitido.",
        "The logo exceeds the maximum allowed size."),
      ["validation.catalogType"] = Localized(
        "Informe um catálogo válido.",
        "Enter a valid catalog type."),
      ["validation.category"] = Localized(
        "Informe uma categoria válida.",
        "Enter a valid category."),
      ["validation.channel"] = Localized(
        "Informe um canal válido.",
        "Enter a valid channel."),
      ["validation.concurrency"] = Localized(
        "O registro foi alterado por outra operação. Atualize os dados e tente novamente.",
        "The record was changed by another operation. Refresh the data and try again."),
      ["validation.contract"] = Localized(
        "Informe um contrato válido.",
        "Enter a valid contract."),
      ["validation.contractStatus"] = Localized(
        "Informe um status de contrato válido.",
        "Enter a valid contract status."),
      ["validation.currency"] = Localized(
        "Informe uma moeda válida.",
        "Enter a valid currency."),
      ["validation.document"] = Localized(
        "Informe um documento válido.",
        "Enter a valid document."),
      ["validation.documentCategory"] = Localized(
        "Informe uma categoria de documento válida.",
        "Enter a valid document category."),
      ["validation.documentLink"] = Localized(
        "Informe ao menos um vínculo de documento válido.",
        "Enter at least one valid document link."),
      ["validation.documentLinkAccess"] = Localized(
        "Você não tem permissão para vincular o documento a esse registro.",
        "You do not have permission to link the document to this record."),
      ["validation.documentLinks"] = Localized(
        "Informe vínculos de documento válidos.",
        "Enter valid document links."),
      ["validation.dueDay"] = Localized(
        "Informe um dia de vencimento entre 1 e 31.",
        "Enter a due day between 1 and 31."),
      ["validation.duplicate"] = Localized(
        "Revise os possíveis registros duplicados antes de continuar.",
        "Review possible duplicate records before continuing."),
      ["validation.emailTaken"] = Localized(
        "Este e-mail já está em uso.",
        "This email is already in use."),
      ["validation.failed"] = Localized(
        "A validação falhou.",
        "Validation failed."),
      ["validation.fileSize"] = Localized(
        "O arquivo excede o tamanho permitido.",
        "The file exceeds the allowed size."),
      ["validation.fileType"] = Localized(
        "O tipo de arquivo não é permitido.",
        "The file type is not allowed."),
      ["validation.garage"] = Localized(
        "Informe dados de garagem válidos.",
        "Enter valid garage data."),
      ["validation.inspectionConditionRating"] = Localized(
        "Informe uma avaliação de condição válida.",
        "Enter a valid condition rating."),
      ["validation.inspectionDocumentKind"] = Localized(
        "Informe um tipo de documento de vistoria válido.",
        "Enter a valid inspection document kind."),
      ["validation.inspectionType"] = Localized(
        "Informe um tipo de vistoria válido.",
        "Enter a valid inspection type."),
      ["validation.lifecycleAction"] = Localized(
        "Informe uma ação de ciclo de vida válida.",
        "Enter a valid lifecycle action."),
      ["validation.linkedEntity"] = Localized(
        "Informe um vínculo válido.",
        "Enter a valid linked entity."),
      ["validation.locale"] = Localized(
        "Informe um idioma válido.",
        "Enter a valid locale."),
      ["validation.mfaPolicy"] = Localized(
        "Informe uma política de MFA válida.",
        "Enter a valid MFA policy."),
      ["validation.money"] = Localized(
        "Informe um valor monetário válido.",
        "Enter a valid money amount."),
      ["validation.moneyPositive"] = Localized(
        "Informe um valor monetário maior que zero.",
        "Enter a money amount greater than zero."),
      ["validation.multipart"] = Localized(
        "Envie a requisição como formulário multipart.",
        "Submit the request as a multipart form."),
      ["validation.occurrenceLink"] = Localized(
        "Informe um imóvel ou contrato vinculado à ocorrência.",
        "Enter a property or contract linked to the occurrence."),
      ["validation.occurrencePriority"] = Localized(
        "Informe uma prioridade de ocorrência válida.",
        "Enter a valid occurrence priority."),
      ["validation.occurrenceStatus"] = Localized(
        "Informe um status de ocorrência válido.",
        "Enter a valid occurrence status."),
      ["validation.occurrenceType"] = Localized(
        "Informe um tipo de ocorrência válido.",
        "Enter a valid occurrence type."),
      ["validation.page"] = Localized(
        "Informe uma página válida.",
        "Enter a valid page."),
      ["validation.pageSize"] = Localized(
        "Informe um tamanho de página válido.",
        "Enter a valid page size."),
      ["validation.parkingGarage"] = Localized(
        "Informe uma garagem com vagas disponíveis.",
        "Enter a garage with available spaces."),
      ["validation.parkingSpaceDuplicate"] = Localized(
        "Esta vaga já está associada a outro veículo ativo.",
        "This parking space is already assigned to another active vehicle."),
      ["validation.parkingSpaceIdentifier"] = Localized(
        "Informe um identificador de vaga válido.",
        "Enter a valid parking space identifier."),
      ["validation.password"] = Localized(
        "A senha não atende à política configurada.",
        "The password does not satisfy the configured policy."),
      ["validation.paymentAlreadySettled"] = Localized(
        "O pagamento já está quitado.",
        "The payment is already settled."),
      ["validation.paymentAmountBelowSettled"] = Localized(
        "O valor não pode ser menor que o total já quitado.",
        "The amount cannot be lower than the already settled total."),
      ["validation.paymentBalance"] = Localized(
        "O valor informado excede o saldo em aberto.",
        "The submitted amount exceeds the open balance."),
      ["validation.paymentGrossAmount"] = Localized(
        "Descontos e acréscimos não podem tornar o valor bruto negativo.",
        "Discounts and additions cannot make the gross amount negative."),
      ["validation.paymentLink"] = Localized(
        "Informe um contrato, morador, imóvel ou conta de consumo para o pagamento.",
        "Enter a contract, resident, property, or utility account for the payment."),
      ["validation.paymentMethod"] = Localized(
        "Informe um método de pagamento válido.",
        "Enter a valid payment method."),
      ["validation.paymentProvider"] = Localized(
        "Informe um provedor de pagamento válido.",
        "Enter a valid payment provider."),
      ["validation.petAuthorizationStatus"] = Localized(
        "Informe um status de autorização de pet válido.",
        "Enter a valid pet authorization status."),
      ["validation.petSpecies"] = Localized(
        "Informe uma espécie de pet válida.",
        "Enter a valid pet species."),
      ["validation.portalStatus"] = Localized(
        "Informe um status de portal válido.",
        "Enter a valid portal status."),
      ["validation.primaryResident"] = Localized(
        "Informe um morador responsável válido.",
        "Enter a valid primary resident."),
      ["validation.privacyFlags"] = Localized(
        "Informe configurações de privacidade válidas.",
        "Enter valid privacy settings."),
      ["validation.property"] = Localized(
        "Informe um imóvel válido.",
        "Enter a valid property."),
      ["validation.propertyAvailability"] = Localized(
        "O imóvel não está disponível para esta operação.",
        "The property is not available for this operation."),
      ["validation.propertyContractMismatch"] = Localized(
        "O imóvel informado não pertence ao contrato selecionado.",
        "The submitted property does not belong to the selected contract."),
      ["validation.providerEvent"] = Localized(
        "Informe um evento de provedor válido.",
        "Enter a valid provider event."),
      ["validation.receiptDocument"] = Localized(
        "Informe um recibo válido.",
        "Enter a valid receipt document."),
      ["validation.reconciliationStatus"] = Localized(
        "Informe um status de conciliação válido.",
        "Enter a valid reconciliation status."),
      ["validation.resident"] = Localized(
        "Informe um morador válido.",
        "Enter a valid resident."),
      ["validation.residentContractMismatch"] = Localized(
        "O morador informado não pertence ao contrato selecionado.",
        "The submitted resident does not belong to the selected contract."),
      ["validation.residentPortalLinked"] = Localized(
        "O morador já possui um acesso de portal vinculado.",
        "The resident already has linked portal access."),
      ["validation.residentPortalState"] = Localized(
        "O acesso do morador ao portal não permite esta operação.",
        "The resident portal access state does not allow this operation."),
      ["validation.residentRole"] = Localized(
        "Informe um papel de morador válido.",
        "Enter a valid resident role."),
      ["validation.residents"] = Localized(
        "Informe moradores válidos.",
        "Enter valid residents."),
      ["validation.role"] = Localized(
        "Informe um papel válido.",
        "Enter a valid role."),
      ["validation.slug"] = Localized(
        "Informe um identificador curto válido.",
        "Enter a valid slug."),
      ["validation.status"] = Localized(
        "Informe um status válido.",
        "Enter a valid status."),
      ["validation.type"] = Localized(
        "Informe um tipo válido.",
        "Enter a valid type."),
      ["validation.url"] = Localized(
        "Informe uma URL válida.",
        "Enter a valid URL."),
      ["validation.utilityAccount"] = Localized(
        "Informe uma conta de consumo válida.",
        "Enter a valid utility account."),
      ["validation.utilityBalance"] = Localized(
        "O valor informado excede o saldo da conta de consumo.",
        "The submitted amount exceeds the utility account balance."),
      ["validation.utilityResponsibility"] = Localized(
        "Informe um responsável válido pela conta de consumo.",
        "Enter a valid utility account responsibility."),
      ["validation.utilityType"] = Localized(
        "Informe um tipo de conta de consumo válido.",
        "Enter a valid utility account type."),
      ["validation.vehicleAuthorizationStatus"] = Localized(
        "Informe um status de autorização de veículo válido.",
        "Enter a valid vehicle authorization status."),
      ["validation.vehicleType"] = Localized(
        "Informe um tipo de veículo válido.",
        "Enter a valid vehicle type."),
      ["validation.vehicleYear"] = Localized(
        "Informe um ano de veículo válido.",
        "Enter a valid vehicle year.")
    };

  private static Dictionary<string, string> Localized(string portuguese, string english) =>
    new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["pt-BR"] = portuguese,
      ["en-US"] = english
    };
}
