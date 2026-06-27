import { useMemo, useState, type FormEvent, type ReactNode } from 'react'
import { useMutation } from '@tanstack/react-query'
import { Send, Upload } from 'lucide-react'
import { Link } from 'react-router-dom'
import {
  ActionButton,
  DataTable,
  DetailList,
  DetailSection,
  EmptyState,
  ErrorState,
  FormField,
  SelectInput,
  TextAreaInput,
  TextInput,
} from '../../components'
import { PaymentInstructionOutput } from '../payments/PaymentInstructionOutput'
import type { AppLocale } from '../../i18n'
import { useApiClient } from '../../lib/api/ApiClientContext'
import {
  createResidentPaymentInstruction,
  createResidentPortalOccurrence,
  uploadResidentPortalDocument,
  type ResidentPortalContract,
  type ResidentPortalDocument,
  type ResidentPortalInspection,
  type ResidentPortalNotification,
  type ResidentPortalOccurrence,
  type ResidentPortalPayment,
  type ResidentPortalStatusLabel,
} from '../../lib/api/residentPortal'
import type { PaymentInstruction, PaymentProviderCode } from '../../lib/api/payments'
import { formatDate, formatDateTime, formatMoney, formatNumber } from '../../lib/format'
import { useResidentPortalContext } from './ResidentPortalContext'

const occurrenceTypeOptions = [
  { label: 'Maintenance', value: 'maintenance' },
  { label: 'Request', value: 'request' },
  { label: 'Complaint', value: 'complaint' },
  { label: 'Other', value: 'other' },
]

const priorityOptions = [
  { label: 'Low', value: 'low' },
  { label: 'Medium', value: 'medium' },
  { label: 'High', value: 'high' },
  { label: 'Urgent', value: 'urgent' },
]

const documentCategoryOptions = [
  { label: 'Resident', value: 'resident' },
  { label: 'Contract', value: 'contract' },
  { label: 'Property', value: 'property' },
  { label: 'Other', value: 'other' },
]

function toneColor(tone?: string) {
  if (tone === 'success') {
    return 'var(--als-color-success, #027a48)'
  }

  if (tone === 'danger') {
    return 'var(--als-color-danger, #b42318)'
  }

  if (tone === 'warning') {
    return 'var(--als-color-warning, #b54708)'
  }

  return 'var(--als-color-primary, #1877f2)'
}

function StatusBadge({ status }: { status: ResidentPortalStatusLabel }) {
  return (
    <span
      style={{
        background: 'var(--als-color-surface-muted, #f8fafc)',
        border: `1px solid ${toneColor(status.tone)}`,
        borderRadius: 999,
        color: toneColor(status.tone),
        display: 'inline-flex',
        fontSize: '0.82rem',
        fontWeight: 800,
        lineHeight: 1,
        padding: '7px 10px',
        whiteSpace: 'nowrap',
      }}
    >
      {status.label}
    </span>
  )
}

function money(value: { amount: number; currency: string }, locale: AppLocale) {
  return formatMoney(value.amount, { currency: value.currency, locale })
}

function bytes(size: number) {
  if (size < 1024) {
    return `${size} B`
  }

  if (size < 1024 * 1024) {
    return `${Math.round(size / 102.4) / 10} KB`
  }

  return `${Math.round(size / 104857.6) / 10} MB`
}

function PageSurface({ children }: { children: ReactNode }) {
  const { copy, query } = useResidentPortalContext()

  if (query.isError) {
    return (
      <ErrorState
        title={copy.states.error}
        retryLabel={copy.actions.retry}
        onRetry={() => void query.refetch()}
      />
    )
  }

  return <div style={{ display: 'grid', gap: 18 }}>{children}</div>
}

export function ResidentPortalOverviewPage() {
  const { copy, locale, summary } = useResidentPortalContext()

  return (
    <PageSurface>
      <DetailSection title={copy.overview.title} description={copy.overview.subtitle}>
        <div
          style={{
            display: 'grid',
            gap: 12,
            gridTemplateColumns: 'repeat(auto-fit, minmax(160px, 1fr))',
          }}
        >
          {[
            {
              label: copy.nav.contracts,
              value: summary?.contracts.length ?? 0,
              to: '/portal/contratos',
            },
            {
              label: copy.nav.payments,
              value: summary?.payments.length ?? 0,
              to: '/portal/pagamentos',
            },
            {
              label: copy.nav.documents,
              value: summary?.documents.length ?? 0,
              to: '/portal/documentos',
            },
            {
              label: copy.nav.notifications,
              value: summary?.notifications.filter((item) => !item.isRead).length ?? 0,
              to: '/portal/notificacoes',
            },
          ].map((item) => (
            <Link
              key={item.label}
              to={item.to}
              style={{
                background: 'var(--als-color-surface-muted, #f8fafc)',
                border: '1px solid var(--als-color-border, #d7deea)',
                borderRadius: 8,
                display: 'grid',
                gap: 6,
                padding: 14,
              }}
            >
              <span style={{ color: 'var(--als-color-text-muted, #64748b)', fontWeight: 800 }}>
                {item.label}
              </span>
              <strong style={{ fontSize: '1.55rem' }}>
                {formatNumber(item.value, { locale })}
              </strong>
            </Link>
          ))}
        </div>
      </DetailSection>

      <DetailSection title={copy.nav.profile}>
        {summary ? (
          <DetailList
            items={[
              { label: copy.fields.fullName, value: summary.profile.fullName },
              { label: copy.fields.email, value: summary.profile.email ?? '-' },
              { label: copy.fields.phone, value: summary.profile.phone ?? '-' },
              {
                label: copy.fields.status,
                value: <StatusBadge status={summary.profile.portalStatus} />,
              },
            ]}
          />
        ) : null}
      </DetailSection>
    </PageSurface>
  )
}

export function ResidentPortalProfilePage() {
  const { copy, summary } = useResidentPortalContext()
  const profile = summary?.profile

  return (
    <PageSurface>
      <DetailSection title={copy.nav.profile}>
        {profile ? (
          <DetailList
            items={[
              { label: copy.fields.fullName, value: profile.fullName },
              { label: copy.fields.email, value: profile.email ?? '-' },
              { label: copy.fields.phone, value: profile.phone ?? '-' },
              { label: copy.fields.contact, value: profile.secondaryPhone ?? '-' },
              { label: copy.fields.status, value: <StatusBadge status={profile.status} /> },
              { label: copy.shell.account, value: <StatusBadge status={profile.portalStatus} /> },
            ]}
          />
        ) : null}
      </DetailSection>
    </PageSurface>
  )
}

export function ResidentPortalPropertyPage() {
  const { copy, locale, summary } = useResidentPortalContext()
  const property = summary?.linkedProperty

  return (
    <PageSurface>
      {property ? (
        <DetailSection title={property.name} description={property.description}>
          <DetailList
            items={[
              { label: copy.fields.status, value: <StatusBadge status={property.status} /> },
              { label: copy.fields.monthlyRent, value: money(property.suggestedRent, locale) },
              { label: copy.fields.neighborhood, value: property.neighborhood },
              { label: copy.fields.city, value: `${property.city} - ${property.stateCode}` },
              {
                label: copy.fields.property,
                value: `${property.streetLine}, ${property.number}${property.complement ? ` - ${property.complement}` : ''}`,
              },
            ]}
          />
        </DetailSection>
      ) : (
        <EmptyState title={copy.empty.property} />
      )}
    </PageSurface>
  )
}

export function ResidentPortalContractsPage() {
  const { copy, locale, summary } = useResidentPortalContext()

  return (
    <PageSurface>
      <DataTable<ResidentPortalContract>
        columns={[
          { cell: (row) => row.property.name, header: copy.fields.property, id: 'property' },
          {
            cell: (row) => <StatusBadge status={row.status} />,
            header: copy.fields.status,
            id: 'status',
          },
          {
            cell: (row) => formatDate(row.startDate, { locale }),
            header: copy.fields.startDate,
            id: 'start',
          },
          {
            cell: (row) => (row.endDate ? formatDate(row.endDate, { locale }) : '-'),
            header: copy.fields.endDate,
            id: 'end',
          },
          {
            cell: (row) => money(row.monthlyRent, locale),
            header: copy.fields.monthlyRent,
            id: 'rent',
          },
          { cell: (row) => row.dueDay, header: copy.fields.dueDay, id: 'dueDay' },
        ]}
        emptyState={copy.empty.contracts}
        getRowKey={(row) => row.id}
        rows={summary?.contracts ?? []}
      />
    </PageSurface>
  )
}

function defaultProviderFor(payment?: ResidentPortalPayment): PaymentProviderCode {
  if (payment?.preferredMethod === 'boleto') {
    return 'mock-boleto'
  }

  if (payment?.preferredMethod === 'paypal') {
    return 'mock-paypal'
  }

  return 'mock-pix'
}

export function ResidentPortalPaymentsPage() {
  const apiClient = useApiClient()
  const { copy, locale, summary } = useResidentPortalContext()
  const [selectedPayment, setSelectedPayment] = useState<ResidentPortalPayment | undefined>()
  const [providerCode, setProviderCode] = useState<PaymentProviderCode>('mock-pix')
  const [instruction, setInstruction] = useState<PaymentInstruction | undefined>()
  const localizedProviderOptions = [
    { label: copy.payments.pix, value: 'mock-pix' as const },
    { label: copy.payments.boleto, value: 'mock-boleto' as const },
    { label: copy.payments.payPal, value: 'mock-paypal' as const },
  ]
  const instructionMutation = useMutation({
    mutationFn: () =>
      createResidentPaymentInstruction(apiClient, selectedPayment!.id, providerCode, locale),
    onSuccess: setInstruction,
  })

  function selectPayment(payment: ResidentPortalPayment) {
    setSelectedPayment(payment)
    setProviderCode(defaultProviderFor(payment))
    setInstruction(undefined)
  }

  return (
    <PageSurface>
      <DataTable<ResidentPortalPayment>
        columns={[
          { cell: (row) => row.title, header: copy.fields.title, id: 'title' },
          {
            cell: (row) => <StatusBadge status={row.status} />,
            header: copy.fields.status,
            id: 'status',
          },
          {
            cell: (row) => formatDate(row.dueDate, { locale }),
            header: copy.fields.dueDate,
            id: 'due',
          },
          { cell: (row) => row.preferredMethodLabel, header: copy.fields.method, id: 'method' },
          { cell: (row) => money(row.amount, locale), header: copy.fields.amount, id: 'amount' },
          { cell: (row) => money(row.balance, locale), header: copy.fields.balance, id: 'balance' },
        ]}
        emptyState={copy.empty.payments}
        getRowKey={(row) => row.id}
        rowActions={(row) => (
          <ActionButton onClick={() => selectPayment(row)} size="sm">
            {copy.actions.generateInstructions}
          </ActionButton>
        )}
        rows={summary?.payments ?? []}
      />

      {selectedPayment ? (
        <DetailSection title={copy.payments.instructions} description={selectedPayment.title}>
          <div style={{ display: 'grid', gap: 14 }}>
            <FormField label={copy.payments.provider}>
              {({ describedBy, id, isInvalid }) => (
                <SelectInput
                  id={id}
                  aria-describedby={describedBy}
                  isInvalid={isInvalid}
                  onChange={(event) =>
                    setProviderCode(event.currentTarget.value as PaymentProviderCode)
                  }
                  options={localizedProviderOptions}
                  value={providerCode}
                />
              )}
            </FormField>
            <ActionButton
              disabled={!selectedPayment}
              icon={<Send size={16} />}
              isLoading={instructionMutation.isPending}
              onClick={() => instructionMutation.mutate()}
              tone="primary"
              type="button"
            >
              {copy.actions.generateInstructions}
            </ActionButton>
            <PaymentInstructionOutput instruction={instruction} locale={locale} />
          </div>
        </DetailSection>
      ) : null}
    </PageSurface>
  )
}

export function ResidentPortalDocumentsPage() {
  const apiClient = useApiClient()
  const { copy, locale, query, summary } = useResidentPortalContext()
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [category, setCategory] = useState('resident')
  const [file, setFile] = useState<File | null>(null)
  const [contractId, setContractId] = useState('')
  const uploadMutation = useMutation({
    mutationFn: () =>
      uploadResidentPortalDocument(
        apiClient,
        {
          category,
          contractId: contractId || undefined,
          description,
          file: file!,
          propertyId: summary?.linkedProperty?.id,
          title,
        },
        locale,
      ),
    onSuccess: () => {
      setTitle('')
      setDescription('')
      setFile(null)
      void query.refetch()
    },
  })
  const canUpload = Boolean(summary?.settings.allowDocumentUpload)
  const contractOptions = useMemo(
    () =>
      (summary?.contracts ?? []).map((contract) => ({
        label: contract.property.name,
        value: contract.id,
      })),
    [summary?.contracts],
  )

  function handleUpload(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (file && title.trim()) {
      uploadMutation.mutate()
    }
  }

  return (
    <PageSurface>
      <DataTable<ResidentPortalDocument>
        columns={[
          { cell: (row) => row.title, header: copy.fields.title, id: 'title' },
          { cell: (row) => row.categoryLabel, header: copy.documents.category, id: 'category' },
          { cell: (row) => row.fileName, header: copy.fields.fileName, id: 'file' },
          { cell: (row) => bytes(row.sizeBytes), header: copy.fields.size, id: 'size' },
          {
            cell: (row) => formatDateTime(row.uploadedAt, { locale }),
            header: copy.fields.uploadedAt,
            id: 'uploaded',
          },
        ]}
        emptyState={copy.empty.documents}
        getRowKey={(row) => row.id}
        rowActions={(row) => (
          <a href={row.downloadRoute} rel="noreferrer">
            {copy.documents.file}
          </a>
        )}
        rows={summary?.documents ?? []}
      />

      <DetailSection
        title={copy.actions.uploadDocument}
        description={!canUpload ? copy.documents.uploadDisabled : undefined}
      >
        <form onSubmit={handleUpload} style={{ display: 'grid', gap: 14 }}>
          <FormField label={copy.forms.title} required>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                id={id}
                aria-describedby={describedBy}
                disabled={!canUpload}
                isInvalid={isInvalid}
                onChange={(event) => setTitle(event.currentTarget.value)}
                required
                value={title}
              />
            )}
          </FormField>
          <FormField label={copy.documents.category}>
            {({ describedBy, id, isInvalid }) => (
              <SelectInput
                id={id}
                aria-describedby={describedBy}
                disabled={!canUpload}
                isInvalid={isInvalid}
                onChange={(event) => setCategory(event.currentTarget.value)}
                options={documentCategoryOptions}
                value={category}
              />
            )}
          </FormField>
          <FormField label={copy.fields.contract}>
            {({ describedBy, id, isInvalid }) => (
              <SelectInput
                id={id}
                aria-describedby={describedBy}
                disabled={!canUpload}
                isInvalid={isInvalid}
                onChange={(event) => setContractId(event.currentTarget.value)}
                options={contractOptions}
                placeholder="-"
                value={contractId}
              />
            )}
          </FormField>
          <FormField label={copy.documents.description}>
            {({ describedBy, id, isInvalid }) => (
              <TextAreaInput
                id={id}
                aria-describedby={describedBy}
                disabled={!canUpload}
                isInvalid={isInvalid}
                onChange={(event) => setDescription(event.currentTarget.value)}
                value={description}
              />
            )}
          </FormField>
          <FormField label={copy.documents.file} required>
            {({ describedBy, id }) => (
              <input
                id={id}
                aria-describedby={describedBy}
                disabled={!canUpload}
                onChange={(event) => setFile(event.currentTarget.files?.[0] ?? null)}
                required
                type="file"
              />
            )}
          </FormField>
          <ActionButton
            disabled={!canUpload || !title.trim() || !file}
            icon={<Upload size={16} />}
            isLoading={uploadMutation.isPending}
            tone="primary"
            type="submit"
          >
            {copy.actions.uploadDocument}
          </ActionButton>
        </form>
      </DetailSection>
    </PageSurface>
  )
}

export function ResidentPortalOccurrencesPage() {
  const apiClient = useApiClient()
  const { copy, locale, query, summary } = useResidentPortalContext()
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [type, setType] = useState('maintenance')
  const [priority, setPriority] = useState('medium')
  const [contractId, setContractId] = useState(summary?.contracts[0]?.id ?? '')
  const canCreate = Boolean(summary?.settings.allowOccurrenceCreation)
  const occurrenceMutation = useMutation({
    mutationFn: () =>
      createResidentPortalOccurrence(
        apiClient,
        {
          contractId: contractId || undefined,
          description,
          priority,
          propertyId: contractId ? undefined : summary?.linkedProperty?.id,
          title,
          type,
        },
        locale,
      ),
    onSuccess: () => {
      setTitle('')
      setDescription('')
      void query.refetch()
    },
  })
  const contractOptions = useMemo(
    () =>
      (summary?.contracts ?? []).map((contract) => ({
        label: contract.property.name,
        value: contract.id,
      })),
    [summary?.contracts],
  )

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (title.trim() && description.trim()) {
      occurrenceMutation.mutate()
    }
  }

  return (
    <PageSurface>
      <DataTable<ResidentPortalOccurrence>
        columns={[
          { cell: (row) => row.title, header: copy.fields.title, id: 'title' },
          { cell: (row) => row.type.label, header: copy.fields.type, id: 'type' },
          {
            cell: (row) => <StatusBadge status={row.priority} />,
            header: copy.fields.priority,
            id: 'priority',
          },
          {
            cell: (row) => <StatusBadge status={row.status} />,
            header: copy.fields.status,
            id: 'status',
          },
          {
            cell: (row) => formatDateTime(row.createdAt, { locale }),
            header: copy.fields.uploadedAt,
            id: 'created',
          },
        ]}
        emptyState={copy.empty.occurrences}
        getRowKey={(row) => row.id}
        rows={summary?.occurrences ?? []}
      />

      <DetailSection
        title={copy.actions.createOccurrence}
        description={!canCreate ? copy.occurrences.createDisabled : undefined}
      >
        <form onSubmit={handleSubmit} style={{ display: 'grid', gap: 14 }}>
          <FormField label={copy.forms.title} required>
            {({ describedBy, id, isInvalid }) => (
              <TextInput
                id={id}
                aria-describedby={describedBy}
                disabled={!canCreate}
                isInvalid={isInvalid}
                onChange={(event) => setTitle(event.currentTarget.value)}
                required
                value={title}
              />
            )}
          </FormField>
          <FormField label={copy.fields.contract}>
            {({ describedBy, id, isInvalid }) => (
              <SelectInput
                id={id}
                aria-describedby={describedBy}
                disabled={!canCreate}
                isInvalid={isInvalid}
                onChange={(event) => setContractId(event.currentTarget.value)}
                options={contractOptions}
                placeholder={summary?.linkedProperty?.name ?? '-'}
                value={contractId}
              />
            )}
          </FormField>
          <FormField label={copy.forms.type}>
            {({ describedBy, id, isInvalid }) => (
              <SelectInput
                id={id}
                aria-describedby={describedBy}
                disabled={!canCreate}
                isInvalid={isInvalid}
                onChange={(event) => setType(event.currentTarget.value)}
                options={occurrenceTypeOptions}
                value={type}
              />
            )}
          </FormField>
          <FormField label={copy.forms.priority}>
            {({ describedBy, id, isInvalid }) => (
              <SelectInput
                id={id}
                aria-describedby={describedBy}
                disabled={!canCreate}
                isInvalid={isInvalid}
                onChange={(event) => setPriority(event.currentTarget.value)}
                options={priorityOptions}
                value={priority}
              />
            )}
          </FormField>
          <FormField label={copy.occurrences.description} required>
            {({ describedBy, id, isInvalid }) => (
              <TextAreaInput
                id={id}
                aria-describedby={describedBy}
                disabled={!canCreate}
                isInvalid={isInvalid}
                onChange={(event) => setDescription(event.currentTarget.value)}
                required
                value={description}
              />
            )}
          </FormField>
          <ActionButton
            disabled={!canCreate || !title.trim() || !description.trim()}
            icon={<Send size={16} />}
            isLoading={occurrenceMutation.isPending}
            tone="primary"
            type="submit"
          >
            {copy.actions.createOccurrence}
          </ActionButton>
        </form>
      </DetailSection>
    </PageSurface>
  )
}

export function ResidentPortalInspectionsPage() {
  const { copy, locale, summary } = useResidentPortalContext()

  return (
    <PageSurface>
      <DataTable<ResidentPortalInspection>
        columns={[
          { cell: (row) => row.title, header: copy.fields.title, id: 'title' },
          { cell: (row) => row.type.label, header: copy.fields.type, id: 'type' },
          {
            cell: (row) => <StatusBadge status={row.status} />,
            header: copy.fields.status,
            id: 'status',
          },
          { cell: (row) => row.property.name, header: copy.fields.property, id: 'property' },
          {
            cell: (row) => formatDateTime(row.scheduledAt, { locale }),
            header: copy.fields.scheduledAt,
            id: 'scheduled',
          },
          {
            cell: (row) => `${row.completedItems}/${row.totalItems}`,
            header: copy.fields.progress,
            id: 'progress',
          },
        ]}
        emptyState={copy.empty.inspections}
        getRowKey={(row) => row.id}
        rows={summary?.inspections ?? []}
      />
    </PageSurface>
  )
}

export function ResidentPortalNotificationsPage() {
  const { copy, locale, summary } = useResidentPortalContext()

  return (
    <PageSurface>
      <DataTable<ResidentPortalNotification>
        columns={[
          { cell: (row) => row.title, header: copy.fields.title, id: 'title' },
          { cell: (row) => row.category.label, header: copy.documents.category, id: 'category' },
          { cell: (row) => row.message, header: copy.documents.description, id: 'message' },
          {
            cell: (row) => formatDateTime(row.createdAt, { locale }),
            header: copy.fields.uploadedAt,
            id: 'created',
          },
        ]}
        emptyState={copy.empty.notifications}
        getRowKey={(row) => row.id}
        rows={summary?.notifications ?? []}
      />
    </PageSurface>
  )
}
