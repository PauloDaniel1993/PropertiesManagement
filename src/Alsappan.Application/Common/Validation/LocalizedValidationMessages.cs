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
      [ValidationMessageKeys.CurrencyCode] = Localized(
        "Informe uma moeda ISO valida com tres letras.",
        "Enter a valid three-letter ISO currency."),
      [ValidationMessageKeys.DateRange] = Localized(
        "A data final deve ser igual ou posterior a data inicial.",
        "The end date must be the same as or after the start date."),
      [ValidationMessageKeys.Email] = Localized(
        "Informe um e-mail valido.",
        "Enter a valid email address."),
      [ValidationMessageKeys.InvalidDate] = Localized(
        "Informe uma data valida no formato AAAA-MM-DD.",
        "Enter a valid date in YYYY-MM-DD format."),
      [ValidationMessageKeys.InvalidId] = Localized(
        "Informe um identificador valido.",
        "Enter a valid identifier."),
      [ValidationMessageKeys.MaxLength] = Localized(
        "O campo excede o tamanho maximo permitido.",
        "The field exceeds the maximum allowed length."),
      [ValidationMessageKeys.MinValue] = Localized(
        "O valor deve ser maior ou igual ao minimo permitido.",
        "The value must be greater than or equal to the minimum allowed."),
      [ValidationMessageKeys.Required] = Localized(
        "Campo obrigatorio.",
        "Required field."),
      [ValidationMessageKeys.Unsupported] = Localized(
        "Valor ainda nao suportado.",
        "Value is not supported yet."),
      ["validation.activeContract"] = Localized(
        "Selecione um contrato ativo.",
        "Select an active contract."),
      ["validation.adjustmentIndex"] = Localized(
        "Informe um indice de reajuste valido.",
        "Enter a valid adjustment index."),
      ["validation.adjustmentInterval"] = Localized(
        "Informe um intervalo de reajuste valido.",
        "Enter a valid adjustment interval."),
      ["validation.assignedUser"] = Localized(
        "Informe um responsavel valido.",
        "Enter a valid assigned user."),
      ["validation.assignee"] = Localized(
        "Informe um responsavel valido.",
        "Enter a valid assignee."),
      ["validation.billingPeriod"] = Localized(
        "Informe um periodo de cobranca valido.",
        "Enter a valid billing period."),
      ["validation.branding"] = Localized(
        "Informe uma configuracao de marca valida.",
        "Enter valid branding settings."),
      ["validation.branding.colorContrast"] = Localized(
        "As cores de marca precisam ter contraste suficiente.",
        "Brand colors must have enough contrast."),
      ["validation.branding.colorHex"] = Localized(
        "Informe uma cor hexadecimal valida.",
        "Enter a valid hexadecimal color."),
      ["validation.branding.logoAltRequired"] = Localized(
        "Informe um texto alternativo para o logotipo.",
        "Enter alternative text for the logo."),
      ["validation.branding.logoDimension"] = Localized(
        "Informe dimensoes validas para o logotipo.",
        "Enter valid logo dimensions."),
      ["validation.branding.logoMimeType"] = Localized(
        "Informe um tipo de arquivo de logotipo valido.",
        "Enter a valid logo file type."),
      ["validation.branding.logoSize"] = Localized(
        "O logotipo excede o tamanho maximo permitido.",
        "The logo exceeds the maximum allowed size."),
      ["validation.catalogType"] = Localized(
        "Informe um catalogo valido.",
        "Enter a valid catalog type."),
      ["validation.category"] = Localized(
        "Informe uma categoria valida.",
        "Enter a valid category."),
      ["validation.channel"] = Localized(
        "Informe um canal valido.",
        "Enter a valid channel."),
      ["validation.concurrency"] = Localized(
        "O registro foi alterado por outra operacao. Atualize os dados e tente novamente.",
        "The record was changed by another operation. Refresh the data and try again."),
      ["validation.contract"] = Localized(
        "Informe um contrato valido.",
        "Enter a valid contract."),
      ["validation.contractStatus"] = Localized(
        "Informe um status de contrato valido.",
        "Enter a valid contract status."),
      ["validation.currency"] = Localized(
        "Informe uma moeda valida.",
        "Enter a valid currency."),
      ["validation.document"] = Localized(
        "Informe um documento valido.",
        "Enter a valid document."),
      ["validation.documentCategory"] = Localized(
        "Informe uma categoria de documento valida.",
        "Enter a valid document category."),
      ["validation.documentLink"] = Localized(
        "Informe ao menos um vinculo de documento valido.",
        "Enter at least one valid document link."),
      ["validation.documentLinkAccess"] = Localized(
        "Voce nao tem permissao para vincular o documento a esse registro.",
        "You do not have permission to link the document to this record."),
      ["validation.dueDay"] = Localized(
        "Informe um dia de vencimento entre 1 e 31.",
        "Enter a due day between 1 and 31."),
      ["validation.duplicate"] = Localized(
        "Revise os possiveis registros duplicados antes de continuar.",
        "Review possible duplicate records before continuing."),
      ["validation.emailTaken"] = Localized(
        "Este e-mail ja esta em uso.",
        "This email is already in use."),
      ["validation.failed"] = Localized(
        "A validacao falhou.",
        "Validation failed."),
      ["validation.fileSize"] = Localized(
        "O arquivo excede o tamanho permitido.",
        "The file exceeds the allowed size."),
      ["validation.fileType"] = Localized(
        "O tipo de arquivo nao e permitido.",
        "The file type is not allowed."),
      ["validation.garage"] = Localized(
        "Informe dados de garagem validos.",
        "Enter valid garage data."),
      ["validation.inspectionConditionRating"] = Localized(
        "Informe uma avaliacao de condicao valida.",
        "Enter a valid condition rating."),
      ["validation.inspectionDocumentKind"] = Localized(
        "Informe um tipo de documento de vistoria valido.",
        "Enter a valid inspection document kind."),
      ["validation.inspectionType"] = Localized(
        "Informe um tipo de vistoria valido.",
        "Enter a valid inspection type."),
      ["validation.lifecycleAction"] = Localized(
        "Informe uma acao de ciclo de vida valida.",
        "Enter a valid lifecycle action."),
      ["validation.linkedEntity"] = Localized(
        "Informe um vinculo valido.",
        "Enter a valid linked entity."),
      ["validation.locale"] = Localized(
        "Informe um idioma valido.",
        "Enter a valid locale."),
      ["validation.mfaPolicy"] = Localized(
        "Informe uma politica de MFA valida.",
        "Enter a valid MFA policy."),
      ["validation.money"] = Localized(
        "Informe um valor monetario valido.",
        "Enter a valid money amount."),
      ["validation.moneyPositive"] = Localized(
        "Informe um valor monetario maior que zero.",
        "Enter a money amount greater than zero."),
      ["validation.occurrenceLink"] = Localized(
        "Informe um imovel ou contrato vinculado a ocorrencia.",
        "Enter a property or contract linked to the occurrence."),
      ["validation.occurrencePriority"] = Localized(
        "Informe uma prioridade de ocorrencia valida.",
        "Enter a valid occurrence priority."),
      ["validation.occurrenceStatus"] = Localized(
        "Informe um status de ocorrencia valido.",
        "Enter a valid occurrence status."),
      ["validation.occurrenceType"] = Localized(
        "Informe um tipo de ocorrencia valido.",
        "Enter a valid occurrence type."),
      ["validation.page"] = Localized(
        "Informe uma pagina valida.",
        "Enter a valid page."),
      ["validation.pageSize"] = Localized(
        "Informe um tamanho de pagina valido.",
        "Enter a valid page size."),
      ["validation.parkingGarage"] = Localized(
        "Informe uma garagem com vagas disponiveis.",
        "Enter a garage with available spaces."),
      ["validation.parkingSpaceDuplicate"] = Localized(
        "Esta vaga ja esta associada a outro veiculo ativo.",
        "This parking space is already assigned to another active vehicle."),
      ["validation.parkingSpaceIdentifier"] = Localized(
        "Informe um identificador de vaga valido.",
        "Enter a valid parking space identifier."),
      ["validation.password"] = Localized(
        "A senha nao atende a politica configurada.",
        "The password does not satisfy the configured policy."),
      ["validation.paymentAlreadySettled"] = Localized(
        "O pagamento ja esta quitado.",
        "The payment is already settled."),
      ["validation.paymentAmountBelowSettled"] = Localized(
        "O valor nao pode ser menor que o total ja quitado.",
        "The amount cannot be lower than the already settled total."),
      ["validation.paymentBalance"] = Localized(
        "O valor informado excede o saldo em aberto.",
        "The submitted amount exceeds the open balance."),
      ["validation.paymentGrossAmount"] = Localized(
        "Descontos e acrescimos nao podem tornar o valor bruto negativo.",
        "Discounts and additions cannot make the gross amount negative."),
      ["validation.paymentLink"] = Localized(
        "Informe um contrato, morador, imovel ou conta de consumo para o pagamento.",
        "Enter a contract, resident, property, or utility account for the payment."),
      ["validation.paymentMethod"] = Localized(
        "Informe um metodo de pagamento valido.",
        "Enter a valid payment method."),
      ["validation.paymentProvider"] = Localized(
        "Informe um provedor de pagamento valido.",
        "Enter a valid payment provider."),
      ["validation.petAuthorizationStatus"] = Localized(
        "Informe um status de autorizacao de pet valido.",
        "Enter a valid pet authorization status."),
      ["validation.petSpecies"] = Localized(
        "Informe uma especie de pet valida.",
        "Enter a valid pet species."),
      ["validation.portalStatus"] = Localized(
        "Informe um status de portal valido.",
        "Enter a valid portal status."),
      ["validation.primaryResident"] = Localized(
        "Informe um morador responsavel valido.",
        "Enter a valid primary resident."),
      ["validation.privacyFlags"] = Localized(
        "Informe configuracoes de privacidade validas.",
        "Enter valid privacy settings."),
      ["validation.property"] = Localized(
        "Informe um imovel valido.",
        "Enter a valid property."),
      ["validation.propertyAvailability"] = Localized(
        "O imovel nao esta disponivel para esta operacao.",
        "The property is not available for this operation."),
      ["validation.propertyContractMismatch"] = Localized(
        "O imovel informado nao pertence ao contrato selecionado.",
        "The submitted property does not belong to the selected contract."),
      ["validation.providerEvent"] = Localized(
        "Informe um evento de provedor valido.",
        "Enter a valid provider event."),
      ["validation.receiptDocument"] = Localized(
        "Informe um recibo valido.",
        "Enter a valid receipt document."),
      ["validation.reconciliationStatus"] = Localized(
        "Informe um status de conciliacao valido.",
        "Enter a valid reconciliation status."),
      ["validation.resident"] = Localized(
        "Informe um morador valido.",
        "Enter a valid resident."),
      ["validation.residentContractMismatch"] = Localized(
        "O morador informado nao pertence ao contrato selecionado.",
        "The submitted resident does not belong to the selected contract."),
      ["validation.residentPortalLinked"] = Localized(
        "O morador ja possui um acesso de portal vinculado.",
        "The resident already has linked portal access."),
      ["validation.residentPortalState"] = Localized(
        "O acesso do morador ao portal nao permite esta operacao.",
        "The resident portal access state does not allow this operation."),
      ["validation.residentRole"] = Localized(
        "Informe um papel de morador valido.",
        "Enter a valid resident role."),
      ["validation.residents"] = Localized(
        "Informe moradores validos.",
        "Enter valid residents."),
      ["validation.role"] = Localized(
        "Informe um papel valido.",
        "Enter a valid role."),
      ["validation.slug"] = Localized(
        "Informe um identificador curto valido.",
        "Enter a valid slug."),
      ["validation.status"] = Localized(
        "Informe um status valido.",
        "Enter a valid status."),
      ["validation.type"] = Localized(
        "Informe um tipo valido.",
        "Enter a valid type."),
      ["validation.url"] = Localized(
        "Informe uma URL valida.",
        "Enter a valid URL."),
      ["validation.utilityAccount"] = Localized(
        "Informe uma conta de consumo valida.",
        "Enter a valid utility account."),
      ["validation.utilityBalance"] = Localized(
        "O valor informado excede o saldo da conta de consumo.",
        "The submitted amount exceeds the utility account balance."),
      ["validation.utilityResponsibility"] = Localized(
        "Informe um responsavel valido pela conta de consumo.",
        "Enter a valid utility account responsibility."),
      ["validation.utilityType"] = Localized(
        "Informe um tipo de conta de consumo valido.",
        "Enter a valid utility account type."),
      ["validation.vehicleAuthorizationStatus"] = Localized(
        "Informe um status de autorizacao de veiculo valido.",
        "Enter a valid vehicle authorization status."),
      ["validation.vehicleType"] = Localized(
        "Informe um tipo de veiculo valido.",
        "Enter a valid vehicle type."),
      ["validation.vehicleYear"] = Localized(
        "Informe um ano de veiculo valido.",
        "Enter a valid vehicle year.")
    };

  private static Dictionary<string, string> Localized(string portuguese, string english) =>
    new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["pt-BR"] = portuguese,
      ["en-US"] = english
    };
}
