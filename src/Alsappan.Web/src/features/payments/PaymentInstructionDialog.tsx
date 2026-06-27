import { useEffect, useMemo, useState } from 'react'
import { ActionButton, Dialog, FormField, SelectInput } from '../../components'
import type {
  PaymentInstruction,
  PaymentListItem,
  PaymentProviderCode,
} from '../../lib/api/payments'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { PaymentInstructionOutput } from './PaymentInstructionOutput'
import { getPaymentCopy } from './paymentCopy'

export type PaymentInstructionDialogProps = {
  instruction?: PaymentInstruction
  isGenerating?: boolean
  isOpen: boolean
  onGenerate: (providerCode: string) => Promise<void> | void
  onOpenChange: (isOpen: boolean) => void
  payment?: PaymentListItem
  providerOptions: Array<{ disabled?: boolean; label: string; value: PaymentProviderCode }>
}

function getDefaultProvider(
  payment: PaymentListItem | undefined,
  options: Array<{ value: string }>,
): string {
  const fallbackProvider = options[0]?.value ?? ''

  if (payment?.preferredMethod === 'boleto') {
    return options.find((option) => option.value === 'mock-boleto')?.value ?? fallbackProvider
  }

  if (payment?.preferredMethod === 'paypal') {
    return options.find((option) => option.value === 'mock-paypal')?.value ?? fallbackProvider
  }

  if (payment?.preferredMethod === 'pix') {
    return options.find((option) => option.value === 'mock-pix')?.value ?? fallbackProvider
  }

  return fallbackProvider
}

export function PaymentInstructionDialog({
  instruction,
  isGenerating = false,
  isOpen,
  onGenerate,
  onOpenChange,
  payment,
  providerOptions,
}: PaymentInstructionDialogProps) {
  const locale = useAppPreferencesStore((state) => state.locale)
  const copy = getPaymentCopy(locale)
  const selectableProviders = useMemo(
    () => providerOptions.filter((option) => !option.disabled),
    [providerOptions],
  )
  const [providerCode, setProviderCode] = useState(getDefaultProvider(payment, selectableProviders))

  useEffect(() => {
    if (isOpen) {
      setProviderCode(getDefaultProvider(payment, selectableProviders))
    }
  }, [isOpen, payment, selectableProviders])

  return (
    <Dialog
      isOpen={isOpen}
      onOpenChange={onOpenChange}
      panelStyle={{ width: 'min(760px, calc(100vw - 32px))' }}
      size="lg"
      title={copy.instructions.title}
    >
      <div style={{ display: 'grid', gap: 16 }}>
        <div
          style={{
            alignItems: 'flex-end',
            display: 'grid',
            gap: 10,
            gridTemplateColumns: 'minmax(220px, 1fr) auto',
          }}
        >
          <FormField label={copy.instructions.provider}>
            {({ describedBy, id, isInvalid }) => (
              <SelectInput
                id={id}
                aria-describedby={describedBy}
                isInvalid={isInvalid}
                onChange={(event) => setProviderCode(event.currentTarget.value)}
                options={providerOptions}
                value={providerCode}
              />
            )}
          </FormField>
          <ActionButton
            disabled={!providerCode}
            isLoading={isGenerating}
            onClick={() => onGenerate(providerCode)}
            tone="primary"
          >
            {copy.instructions.generate}
          </ActionButton>
        </div>

        <PaymentInstructionOutput instruction={instruction} locale={locale} />

        <div style={{ display: 'flex', justifyContent: 'flex-end' }}>
          <ActionButton onClick={() => onOpenChange(false)} type="button">
            {copy.instructions.cancel}
          </ActionButton>
        </div>
      </div>
    </Dialog>
  )
}
