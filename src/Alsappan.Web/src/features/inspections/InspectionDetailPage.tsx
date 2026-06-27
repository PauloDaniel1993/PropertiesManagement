import { useMemo } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ArrowLeft, Ban, CheckCircle2, Pencil, Play, Trash2 } from 'lucide-react'
import {
  ActionButton,
  CheckboxInput,
  DetailList,
  DetailSection,
  EmptyState,
  ErrorState,
  FormField,
  LoadingState,
  PageHeader,
  RelationshipPanel,
  SelectInput,
  StatusBadge,
  Tabs,
  TextInput,
  type RelationshipPanelItem,
  type StatusBadgeTone,
} from '../../components'
import { useApiClient } from '../../lib/api/ApiClientContext'
import { listDocuments } from '../../lib/api/documents'
import { formatDateTime } from '../../lib/format'
import {
  addInspectionChecklistItem,
  deleteInspectionChecklistItem,
  getInspection,
  inspectionConditionRatings,
  inspectionDocumentKinds,
  linkInspectionDocument,
  listInspectionConditionRatingOptions,
  listInspectionDocumentKindOptions,
  updateInspectionChecklistItem,
  type InspectionChecklistItem,
  type InspectionConditionRating,
  type InspectionDetail,
  type InspectionDocumentKind,
  type InspectionStatus,
} from '../../lib/api/inspections'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { useAuthSessionStore } from '../../stores/useAuthSessionStore'
import { hasAnyPermission } from '../identity/session'
import { getInspectionCopy } from './inspectionCopy'

export type InspectionDetailPageProps = {
  inspectionId: string
  onBack: () => void
  onCancel?: (inspection: InspectionDetail) => void
  onComplete?: (inspection: InspectionDetail) => void
  onEdit?: (inspection: InspectionDetail) => void
  onStart?: (inspection: InspectionDetail) => void
}

const statusTones: Record<InspectionStatus, StatusBadgeTone> = {
  archived: 'archived',
  cancelled: 'archived',
  completed: 'success',
  'in-progress': 'info',
  scheduled: 'warning',
}

const ratingTones: Record<InspectionConditionRating, StatusBadgeTone> = {
  attention: 'warning',
  critical: 'danger',
  damaged: 'danger',
  good: 'success',
  'not-applicable': 'neutral',
  pending: 'warning',
}

const gridStyle = {
  display: 'grid',
  gap: 12,
  gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))',
} as const

function formatEntity(entity: InspectionDetail['property'] | undefined, emptyLabel: string) {
  if (!entity) {
    return emptyLabel
  }

  return entity.description ? `${entity.name} | ${entity.description}` : entity.name
}

function progressLabel(inspection: InspectionDetail, copy: ReturnType<typeof getInspectionCopy>) {
  return copy.terms.progress(
    inspection.progress.completedItems,
    inspection.progress.totalItems,
    inspection.progress.percentage,
  )
}

function readString(formData: FormData, key: string) {
  const value = formData.get(key)

  return typeof value === 'string' ? value.trim() : ''
}

function readRating(formData: FormData) {
  const value = readString(formData, 'conditionRating')

  return inspectionConditionRatings.includes(value as InspectionConditionRating)
    ? (value as InspectionConditionRating)
    : 'pending'
}

function readDocumentKind(formData: FormData) {
  const value = readString(formData, 'kind')

  return inspectionDocumentKinds.includes(value as InspectionDocumentKind)
    ? (value as InspectionDocumentKind)
    : 'attachment'
}

function getRelationshipPanels(
  inspection: InspectionDetail,
  copy: ReturnType<typeof getInspectionCopy>,
) {
  const documents = [...inspection.photoDocuments, ...inspection.linkedDocuments]
  const relationshipItems: Array<RelationshipPanelItem | undefined> = [
    inspection.property
      ? {
          description: inspection.property.description,
          href: inspection.property.route,
          id: inspection.property.id,
          title: inspection.property.name,
        }
      : undefined,
    inspection.contract
      ? {
          description: inspection.contract.description,
          href: inspection.contract.route,
          id: inspection.contract.id,
          title: inspection.contract.name,
        }
      : undefined,
    inspection.resident
      ? {
          description: inspection.resident.description,
          href: inspection.resident.route,
          id: inspection.resident.id,
          title: inspection.resident.name,
        }
      : undefined,
    inspection.assignee
      ? {
          description: inspection.assignee.description,
          href: inspection.assignee.route,
          id: inspection.assignee.id,
          title: inspection.assignee.name,
        }
      : undefined,
  ]

  return (
    <div
      style={{
        display: 'grid',
        gap: 12,
        gridTemplateColumns: 'repeat(auto-fit, minmax(260px, 1fr))',
      }}
    >
      <RelationshipPanel
        emptyState={copy.detail.emptyRelationship}
        items={relationshipItems.filter((item): item is RelationshipPanelItem => Boolean(item))}
        title={copy.detail.relationshipTitle}
      />

      <RelationshipPanel
        emptyState={copy.detail.emptyRelationship}
        items={documents.map((document) => ({
          description: document.kind.label,
          href: document.route,
          id: document.documentId,
          title: document.label ?? document.documentId,
        }))}
        title={copy.detail.documentsTitle}
      />

      <RelationshipPanel
        items={[
          {
            description: inspection.timelineRoute,
            href: inspection.timelineRoute,
            id: 'timeline-link',
            title: copy.detail.relationships.timeline,
          },
        ]}
        title={copy.detail.relationships.timeline}
      />

      <RelationshipPanel
        items={[
          {
            description: inspection.auditRoute,
            href: inspection.auditRoute,
            id: 'audit-link',
            title: copy.detail.relationships.audit,
          },
        ]}
        title={copy.detail.relationships.audit}
      />
    </div>
  )
}

function getItemPayload(formData: FormData, fallback?: InspectionChecklistItem) {
  return {
    areaName: readString(formData, 'areaName') || fallback?.areaName || '',
    conditionRating: readRating(formData),
    isRequired: formData.get('isRequired') === 'on',
    itemName: readString(formData, 'itemName') || fallback?.itemName || '',
    observations: readString(formData, 'observations') || undefined,
    sortOrder: Number(readString(formData, 'sortOrder') || fallback?.sortOrder || 0),
  }
}

export function InspectionDetailPage({
  inspectionId,
  onBack,
  onCancel,
  onComplete,
  onEdit,
  onStart,
}: InspectionDetailPageProps) {
  const apiClient = useApiClient()
  const queryClient = useQueryClient()
  const locale = useAppPreferencesStore((state) => state.locale)
  const authUser = useAuthSessionStore((state) => state.user)
  const copy = getInspectionCopy(locale)
  const canWriteInspections = hasAnyPermission(['inspections.write'], authUser)
  const canManageInspections = hasAnyPermission(['inspections.manage'], authUser)
  const canReadDocuments = hasAnyPermission(['documents.read'], authUser)
  const inspectionQuery = useQuery({
    queryFn: () => getInspection(apiClient, inspectionId, locale),
    queryKey: ['inspections', 'detail', inspectionId, locale],
  })
  const ratingOptionsQuery = useQuery({
    queryFn: () => listInspectionConditionRatingOptions(apiClient, locale),
    queryKey: ['inspections', 'condition-rating-options', locale],
  })
  const documentKindOptionsQuery = useQuery({
    enabled:
      canWriteInspections &&
      canReadDocuments &&
      !['archived', 'cancelled', 'completed'].includes(inspectionQuery.data?.status.code ?? ''),
    queryFn: () => listInspectionDocumentKindOptions(apiClient, locale),
    queryKey: ['inspections', 'document-kind-options', locale],
  })
  const documentsQuery = useQuery({
    enabled:
      canWriteInspections &&
      canReadDocuments &&
      !['archived', 'cancelled', 'completed'].includes(inspectionQuery.data?.status.code ?? ''),
    queryFn: () => listDocuments(apiClient, { category: 'inspection', locale, pageSize: 100 }),
    queryKey: ['documents', 'inspection-picker', locale],
  })
  const ratingOptions =
    ratingOptionsQuery.data ??
    inspectionConditionRatings.map((rating) => ({
      label: copy.terms.conditionRatings[rating],
      value: rating,
    }))
  const documentKindOptions =
    documentKindOptionsQuery.data ??
    inspectionDocumentKinds.map((kind) => ({ label: copy.terms.documentKinds[kind], value: kind }))
  const documentOptions = useMemo(
    () =>
      (documentsQuery.data?.items ?? []).map((document) => ({
        label: `${document.title} | ${document.fileName}`,
        value: document.id,
      })),
    [documentsQuery.data?.items],
  )
  const checklistMutation = useMutation({
    mutationFn: async (request: {
      action: 'add' | 'delete' | 'update'
      formData?: FormData
      item?: InspectionChecklistItem
      itemId?: string
    }) => {
      if (request.action === 'delete' && request.itemId) {
        return deleteInspectionChecklistItem(apiClient, inspectionId, request.itemId, locale)
      }

      if (!request.formData) {
        throw new Error('Missing checklist payload.')
      }

      const payload = getItemPayload(request.formData, request.item)

      if (request.action === 'update' && request.itemId) {
        return updateInspectionChecklistItem(
          apiClient,
          inspectionId,
          request.itemId,
          payload,
          locale,
        )
      }

      return addInspectionChecklistItem(apiClient, inspectionId, payload, locale)
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['inspections'] })
    },
  })
  const documentMutation = useMutation({
    mutationFn: (formData: FormData) =>
      linkInspectionDocument(
        apiClient,
        inspectionId,
        {
          checklistItemId: readString(formData, 'checklistItemId') || undefined,
          documentId: readString(formData, 'documentId'),
          kind: readDocumentKind(formData),
          label: readString(formData, 'label') || undefined,
        },
        locale,
      ),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['inspections'] })
      await queryClient.invalidateQueries({ queryKey: ['documents'] })
    },
  })
  const inspection = inspectionQuery.data

  if (inspectionQuery.isLoading) {
    return <LoadingState title={copy.detail.loading} />
  }

  if (inspectionQuery.isError || !inspection) {
    return (
      <section style={{ display: 'grid', gap: 18 }}>
        <ActionButton icon={<ArrowLeft aria-hidden="true" size={16} />} onClick={onBack}>
          {copy.detail.actions.back}
        </ActionButton>
        <ErrorState title={copy.detail.error} onRetry={() => void inspectionQuery.refetch()} />
      </section>
    )
  }

  const statusAllowsMutation = !['archived', 'cancelled', 'completed'].includes(
    inspection.status.code,
  )
  const canMutate = canWriteInspections && statusAllowsMutation
  const canLinkDocuments = canMutate && canReadDocuments
  const canStart = onStart && canWriteInspections && inspection.status.code === 'scheduled'
  const canComplete = onComplete && canManageInspections && inspection.status.code === 'in-progress'
  const canCancel = onCancel && canManageInspections && statusAllowsMutation
  const canEdit = onEdit && canMutate
  const reportDocumentCount = inspection.photoDocuments.length + inspection.linkedDocuments.length

  return (
    <section style={{ display: 'grid', gap: 18 }}>
      <PageHeader
        actions={
          <>
            <ActionButton icon={<ArrowLeft aria-hidden="true" size={16} />} onClick={onBack}>
              {copy.detail.actions.back}
            </ActionButton>
            {canStart ? (
              <ActionButton
                icon={<Play aria-hidden="true" size={16} />}
                onClick={() => onStart(inspection)}
              >
                {copy.detail.actions.start}
              </ActionButton>
            ) : null}
            {canComplete ? (
              <ActionButton
                icon={<CheckCircle2 aria-hidden="true" size={16} />}
                onClick={() => onComplete(inspection)}
              >
                {copy.detail.actions.complete}
              </ActionButton>
            ) : null}
            {canCancel ? (
              <ActionButton
                icon={<Ban aria-hidden="true" size={16} />}
                onClick={() => onCancel(inspection)}
                tone="danger"
              >
                {copy.detail.actions.cancel}
              </ActionButton>
            ) : null}
          </>
        }
        description={copy.detail.pageDescription}
        primaryAction={
          canEdit
            ? {
                icon: <Pencil aria-hidden="true" size={18} />,
                label: copy.detail.actions.edit,
                onClick: () => onEdit(inspection),
              }
            : undefined
        }
        title={inspection.title}
      />

      <DetailSection title={copy.detail.summaryTitle}>
        <DetailList
          columns={3}
          items={[
            {
              label: copy.detail.labels.status,
              value: (
                <StatusBadge
                  label={inspection.status.label}
                  tone={statusTones[inspection.status.code]}
                />
              ),
            },
            {
              label: copy.detail.labels.type,
              value: inspection.type.label,
            },
            {
              label: copy.detail.labels.scheduledAt,
              value: formatDateTime(inspection.scheduledAt, { locale }),
            },
            {
              label: copy.detail.labels.startedAt,
              value: inspection.startedAt ? formatDateTime(inspection.startedAt, { locale }) : '-',
            },
            {
              label: copy.detail.labels.completedAt,
              value: inspection.completedAt
                ? formatDateTime(inspection.completedAt, { locale })
                : '-',
            },
            {
              label: copy.detail.labels.cancelledAt,
              value: inspection.cancelledAt
                ? formatDateTime(inspection.cancelledAt, { locale })
                : '-',
            },
            {
              label: copy.detail.labels.progress,
              value: progressLabel(inspection, copy),
            },
            {
              label: copy.detail.labels.assignee,
              value: formatEntity(inspection.assignee, copy.terms.none),
            },
            {
              label: copy.detail.labels.property,
              value: formatEntity(inspection.property, copy.terms.none),
            },
            {
              label: copy.detail.labels.contract,
              value: formatEntity(inspection.contract, copy.terms.none),
            },
            {
              label: copy.detail.labels.resident,
              value: formatEntity(inspection.resident, copy.terms.none),
            },
            {
              label: copy.detail.audit.createdAt,
              value: formatDateTime(inspection.createdAt, { locale }),
            },
            {
              label: copy.detail.audit.updatedAt,
              value: inspection.updatedAt ? formatDateTime(inspection.updatedAt, { locale }) : '-',
            },
            {
              label: copy.detail.audit.archivedAt,
              value: inspection.archivedAt
                ? formatDateTime(inspection.archivedAt, { locale })
                : '-',
            },
            {
              label: copy.detail.labels.notes,
              value: inspection.notes ?? copy.detail.noNotes,
            },
          ]}
        />
      </DetailSection>

      {inspection.status.code === 'completed' ? (
        <DetailSection title={copy.detail.completedReportTitle}>
          <DetailList
            columns={2}
            items={[
              {
                label: copy.detail.labels.completedAt,
                value: inspection.completedAt
                  ? formatDateTime(inspection.completedAt, { locale })
                  : '-',
              },
              {
                label: copy.detail.labels.progress,
                value: progressLabel(inspection, copy),
              },
              {
                label: copy.detail.labels.completionNotes,
                value: inspection.completionNotes ?? copy.detail.noNotes,
              },
              {
                label: copy.detail.documentsTitle,
                value: reportDocumentCount,
              },
            ]}
          />
        </DetailSection>
      ) : null}

      <Tabs
        ariaLabel={copy.detail.relationshipTitle}
        tabs={[
          {
            content: (
              <DetailSection
                actions={
                  canMutate ? (
                    <form
                      onSubmit={(event) => {
                        event.preventDefault()
                        checklistMutation.mutate({
                          action: 'add',
                          formData: new FormData(event.currentTarget),
                        })
                        event.currentTarget.reset()
                      }}
                      style={{ display: 'grid', gap: 10, minWidth: 280 }}
                    >
                      <div style={gridStyle}>
                        <TextInput
                          aria-label={copy.detail.checklist.areaName}
                          name="areaName"
                          placeholder={copy.detail.checklist.areaName}
                          required
                        />
                        <TextInput
                          aria-label={copy.detail.checklist.itemName}
                          name="itemName"
                          placeholder={copy.detail.checklist.itemName}
                          required
                        />
                        <SelectInput
                          aria-label={copy.detail.checklist.conditionRating}
                          name="conditionRating"
                          options={ratingOptions}
                          defaultValue="pending"
                        />
                        <TextInput
                          aria-label={copy.detail.checklist.observations}
                          name="observations"
                          placeholder={copy.detail.checklist.observations}
                        />
                        <TextInput
                          aria-label="sortOrder"
                          name="sortOrder"
                          type="number"
                          defaultValue="0"
                          min={0}
                        />
                        <CheckboxInput
                          defaultChecked
                          label={copy.detail.checklist.required}
                          name="isRequired"
                        />
                      </div>
                      <ActionButton
                        isLoading={checklistMutation.isPending}
                        size="sm"
                        tone="primary"
                        type="submit"
                      >
                        {copy.detail.checklist.add}
                      </ActionButton>
                    </form>
                  ) : undefined
                }
                title={copy.detail.checklist.title}
              >
                {inspection.checklistItems.length === 0 ? (
                  <EmptyState title={copy.detail.checklist.empty} />
                ) : (
                  <div style={{ display: 'grid', gap: 10 }}>
                    {inspection.checklistItems.map((item) => (
                      <form
                        key={item.id}
                        onSubmit={(event) => {
                          event.preventDefault()
                          checklistMutation.mutate({
                            action: 'update',
                            formData: new FormData(event.currentTarget),
                            item,
                            itemId: item.id,
                          })
                        }}
                        style={{
                          border: '1px solid var(--als-color-border, #d7deea)',
                          borderRadius: 'var(--als-radius-md, 8px)',
                          display: 'grid',
                          gap: 12,
                          padding: 12,
                        }}
                      >
                        <div style={gridStyle}>
                          <TextInput
                            aria-label={copy.detail.checklist.areaName}
                            defaultValue={item.areaName}
                            disabled={!canMutate}
                            name="areaName"
                          />
                          <TextInput
                            aria-label={copy.detail.checklist.itemName}
                            defaultValue={item.itemName}
                            disabled={!canMutate}
                            name="itemName"
                          />
                          <SelectInput
                            aria-label={copy.detail.checklist.conditionRating}
                            defaultValue={item.conditionRating.code}
                            disabled={!canMutate}
                            name="conditionRating"
                            options={ratingOptions}
                          />
                          <TextInput
                            aria-label={copy.detail.checklist.observations}
                            defaultValue={item.observations ?? ''}
                            disabled={!canMutate}
                            name="observations"
                          />
                          <TextInput
                            aria-label="sortOrder"
                            defaultValue={item.sortOrder}
                            disabled={!canMutate}
                            min={0}
                            name="sortOrder"
                            type="number"
                          />
                          <CheckboxInput
                            defaultChecked={item.isRequired}
                            disabled={!canMutate}
                            label={copy.detail.checklist.required}
                            name="isRequired"
                          />
                        </div>
                        <div
                          style={{
                            alignItems: 'center',
                            display: 'flex',
                            flexWrap: 'wrap',
                            gap: 8,
                            justifyContent: 'space-between',
                          }}
                        >
                          <StatusBadge
                            label={item.conditionRating.label}
                            tone={ratingTones[item.conditionRating.code]}
                          />
                          {canMutate ? (
                            <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8 }}>
                              <ActionButton
                                isLoading={checklistMutation.isPending}
                                size="sm"
                                type="submit"
                              >
                                {copy.detail.checklist.save}
                              </ActionButton>
                              <ActionButton
                                icon={<Trash2 aria-hidden="true" size={16} />}
                                onClick={() =>
                                  checklistMutation.mutate({
                                    action: 'delete',
                                    itemId: item.id,
                                  })
                                }
                                size="sm"
                                tone="danger"
                              >
                                {copy.detail.checklist.delete}
                              </ActionButton>
                            </div>
                          ) : null}
                        </div>
                      </form>
                    ))}
                  </div>
                )}
              </DetailSection>
            ),
            id: 'checklist',
            label: copy.detail.checklist.title,
          },
          {
            content: (
              <div style={{ display: 'grid', gap: 16 }}>
                {canLinkDocuments ? (
                  <DetailSection title={copy.documentLink.title}>
                    <form
                      onSubmit={(event) => {
                        event.preventDefault()
                        documentMutation.mutate(new FormData(event.currentTarget))
                        event.currentTarget.reset()
                      }}
                      style={{ display: 'grid', gap: 12 }}
                    >
                      <div style={gridStyle}>
                        <FormField label={copy.documentLink.documentId}>
                          {({ describedBy, id }) => (
                            <SelectInput
                              id={id}
                              aria-describedby={describedBy}
                              name="documentId"
                              options={documentOptions}
                              placeholder={copy.documentLink.documentId}
                              required
                            />
                          )}
                        </FormField>
                        <FormField label={copy.documentLink.kind}>
                          {({ describedBy, id }) => (
                            <SelectInput
                              id={id}
                              aria-describedby={describedBy}
                              defaultValue="photo"
                              name="kind"
                              options={documentKindOptions}
                            />
                          )}
                        </FormField>
                        <FormField label={copy.documentLink.checklistItem}>
                          {({ describedBy, id }) => (
                            <SelectInput
                              id={id}
                              aria-describedby={describedBy}
                              name="checklistItemId"
                              options={inspection.checklistItems.map((item) => ({
                                label: `${item.areaName} | ${item.itemName}`,
                                value: item.id,
                              }))}
                              placeholder={copy.documentLink.checklistItem}
                            />
                          )}
                        </FormField>
                        <FormField label={copy.documentLink.label}>
                          {({ describedBy, id }) => (
                            <TextInput id={id} aria-describedby={describedBy} name="label" />
                          )}
                        </FormField>
                      </div>
                      <ActionButton
                        isLoading={documentMutation.isPending}
                        tone="primary"
                        type="submit"
                      >
                        {copy.documentLink.submit}
                      </ActionButton>
                    </form>
                  </DetailSection>
                ) : null}
                {getRelationshipPanels(inspection, copy)}
              </div>
            ),
            id: 'documents',
            label: copy.detail.documentsTitle,
          },
          {
            content: (
              <DetailSection title={copy.detail.signaturesTitle}>
                {inspection.signatureSlots.length === 0 ? (
                  <EmptyState title={copy.detail.emptyRelationship} />
                ) : (
                  <DetailList
                    columns={2}
                    items={inspection.signatureSlots.map((slot) => ({
                      description: slot.notes,
                      label: slot.signerRole,
                      value: slot.isSigned
                        ? `${slot.signerName ?? copy.terms.none} | ${
                            slot.signedAt ? formatDateTime(slot.signedAt, { locale }) : '-'
                          }`
                        : copy.terms.none,
                    }))}
                  />
                )}
              </DetailSection>
            ),
            id: 'signatures',
            label: copy.detail.signaturesTitle,
          },
        ]}
      />
    </section>
  )
}
