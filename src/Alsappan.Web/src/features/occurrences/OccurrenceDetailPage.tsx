import { useEffect, useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Archive,
  ArrowLeft,
  CheckCircle2,
  MessageSquarePlus,
  Paperclip,
  Pencil,
  RotateCcw,
  XCircle,
} from 'lucide-react'
import {
  ActionButton,
  CheckboxInput,
  DetailList,
  DetailSection,
  ErrorState,
  FormField,
  LoadingState,
  PageHeader,
  RelationshipPanel,
  SelectInput,
  StatusBadge,
  Tabs,
  TextAreaInput,
  TextInput,
  type RelationshipPanelItem,
  type StatusBadgeTone,
} from '../../components'
import type { AppLocale } from '../../i18n'
import { useApiClient } from '../../lib/api/ApiClientContext'
import { listAdministrators } from '../../lib/api/administrators'
import { listDocuments } from '../../lib/api/documents'
import {
  addOccurrenceComment,
  archiveOccurrence,
  assignOccurrence,
  attachOccurrenceDocument,
  cancelOccurrence,
  changeOccurrencePriority,
  changeOccurrenceStatus,
  getOccurrence,
  listOccurrencePriorityOptions,
  listOccurrenceStatusOptions,
  restoreOccurrence,
  resolveOccurrence,
  type OccurrenceAssignmentHistory,
  type OccurrenceDetail,
  type OccurrencePriority,
  type OccurrencePriorityHistory,
  type OccurrenceStatus,
  type OccurrenceStatusHistory,
} from '../../lib/api/occurrences'
import { formatDate, formatDateTime } from '../../lib/format'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { useAuthSessionStore } from '../../stores/useAuthSessionStore'
import {
  buildEntityAuditRoute,
  buildEntityTimelineRoute,
  canReadRelationshipModule,
  type RelationshipContext,
} from '../crossModule/relationships'
import { hasAnyPermission } from '../identity/session'
import { EntityTimelinePanel } from '../timeline'
import { getOccurrenceCopy } from './occurrenceCopy'

export type OccurrenceDetailPageProps = {
  occurrenceId: string
  onBack: () => void
  onEdit?: (occurrence: OccurrenceDetail) => void
}

type MutableOccurrenceStatus = Exclude<OccurrenceStatus, 'archived' | 'cancelled' | 'resolved'>

type WorkflowRequest =
  | {
      action: 'archive'
    }
  | {
      action: 'assign'
      assignedUserId?: string
      notes?: string
    }
  | {
      action: 'attach'
      documentId: string
      label?: string
    }
  | {
      action: 'cancel'
      notes?: string
    }
  | {
      action: 'comment'
      body: string
      isInternal: boolean
    }
  | {
      action: 'priority'
      notes?: string
      priority: OccurrencePriority
    }
  | {
      action: 'resolve'
      resolutionNotes: string
    }
  | {
      action: 'restore'
    }
  | {
      action: 'status'
      notes?: string
      status: MutableOccurrenceStatus
    }

const statusTones: Record<OccurrenceStatus, StatusBadgeTone> = {
  archived: 'archived',
  assigned: 'warning',
  cancelled: 'danger',
  'in-progress': 'warning',
  open: 'info',
  resolved: 'success',
  waiting: 'neutral',
}

const priorityTones: Record<OccurrencePriority, StatusBadgeTone> = {
  high: 'warning',
  low: 'neutral',
  medium: 'info',
  urgent: 'danger',
}

const mutableStatuses: MutableOccurrenceStatus[] = ['open', 'assigned', 'in-progress', 'waiting']

const formGridStyle = {
  display: 'grid',
  gap: 12,
  gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))',
} as const

const listStyle = {
  display: 'grid',
  gap: 10,
  listStyle: 'none',
  margin: 0,
  padding: 0,
} as const

function optionalText(value: string) {
  const trimmedValue = value.trim()

  return trimmedValue.length > 0 ? trimmedValue : undefined
}

function isTerminalStatus(status: OccurrenceStatus) {
  return status === 'archived' || status === 'cancelled' || status === 'resolved'
}

function formatEntity(entity: OccurrenceDetail['property'], emptyLabel: string) {
  if (!entity) {
    return emptyLabel
  }

  return entity.description ? `${entity.name} | ${entity.description}` : entity.name
}

function getRelationshipPanels(
  occurrence: OccurrenceDetail,
  copy: ReturnType<typeof getOccurrenceCopy>,
  authUser: ReturnType<typeof useAuthSessionStore.getState>['user'],
) {
  const context: RelationshipContext = { entityId: occurrence.id, entityType: 'occurrence' }
  const relationshipItems: Array<RelationshipPanelItem | undefined> = [
    occurrence.property && canReadRelationshipModule('properties', authUser)
      ? {
          description: occurrence.property.description,
          href: occurrence.property.route,
          id: occurrence.property.id,
          title: occurrence.property.name,
        }
      : undefined,
    occurrence.resident && canReadRelationshipModule('residents', authUser)
      ? {
          description: occurrence.resident.description,
          href: occurrence.resident.route,
          id: occurrence.resident.id,
          title: occurrence.resident.name,
        }
      : undefined,
    occurrence.contract && canReadRelationshipModule('contracts', authUser)
      ? {
          description: occurrence.contract.description,
          href: occurrence.contract.route,
          id: occurrence.contract.id,
          title: occurrence.contract.name,
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

      {canReadRelationshipModule('timeline', authUser) ? (
        <RelationshipPanel
          items={[
            {
              description: occurrence.timelineRoute,
              href: buildEntityTimelineRoute(context),
              id: 'timeline-link',
              title: copy.detail.relationships.timeline,
            },
          ]}
          title={copy.detail.relationships.timeline}
        />
      ) : null}

      {canReadRelationshipModule('audit', authUser) ? (
        <RelationshipPanel
          items={[
            {
              description: occurrence.auditRoute,
              href: buildEntityAuditRoute(context),
              id: 'audit-link',
              title: copy.detail.relationships.audit,
            },
          ]}
          title={copy.detail.relationships.audit}
        />
      ) : null}
    </div>
  )
}

function formatActor(actorDisplayName: string | undefined, actorUserId: string | undefined) {
  return actorDisplayName ?? actorUserId ?? ''
}

function HistoryItem({
  children,
  meta,
  title,
}: {
  children?: string
  meta?: string
  title: string
}) {
  return (
    <li
      style={{
        border: '1px solid var(--als-color-border, #d7deea)',
        borderRadius: 'var(--als-radius-md, 8px)',
        display: 'grid',
        gap: 4,
        padding: 12,
      }}
    >
      <strong>{title}</strong>
      {children ? <span>{children}</span> : null}
      {meta ? (
        <span style={{ color: 'var(--als-color-text-muted, #64748b)', fontSize: '0.86rem' }}>
          {meta}
        </span>
      ) : null}
    </li>
  )
}

function renderStatusHistory(
  rows: OccurrenceStatusHistory[],
  locale: AppLocale,
  copy: ReturnType<typeof getOccurrenceCopy>,
) {
  if (rows.length === 0) {
    return <p>{copy.terms.none}</p>
  }

  return (
    <ul style={listStyle}>
      {rows.map((row) => (
        <HistoryItem
          key={row.id}
          title={`${row.previousStatus?.label ?? '-'} -> ${row.newStatus.label}`}
          meta={[
            formatDateTime(row.createdAt, { locale }),
            formatActor(row.actorDisplayName, row.actorUserId),
          ]
            .filter(Boolean)
            .join(' | ')}
        >
          {row.notes}
        </HistoryItem>
      ))}
    </ul>
  )
}

function renderPriorityHistory(
  rows: OccurrencePriorityHistory[],
  locale: AppLocale,
  copy: ReturnType<typeof getOccurrenceCopy>,
) {
  if (rows.length === 0) {
    return <p>{copy.terms.none}</p>
  }

  return (
    <ul style={listStyle}>
      {rows.map((row) => (
        <HistoryItem
          key={row.id}
          title={`${row.previousPriority?.label ?? '-'} -> ${row.newPriority.label}`}
          meta={[
            formatDateTime(row.createdAt, { locale }),
            formatActor(row.actorDisplayName, row.actorUserId),
          ]
            .filter(Boolean)
            .join(' | ')}
        >
          {row.notes}
        </HistoryItem>
      ))}
    </ul>
  )
}

function renderAssignmentHistory(
  rows: OccurrenceAssignmentHistory[],
  locale: AppLocale,
  copy: ReturnType<typeof getOccurrenceCopy>,
) {
  if (rows.length === 0) {
    return <p>{copy.terms.none}</p>
  }

  return (
    <ul style={listStyle}>
      {rows.map((row) => (
        <HistoryItem
          key={row.id}
          title={`${row.previousAssignedUser?.displayName ?? '-'} -> ${
            row.newAssignedUser?.displayName ?? '-'
          }`}
          meta={[
            formatDateTime(row.createdAt, { locale }),
            formatActor(row.actorDisplayName, row.actorUserId),
          ]
            .filter(Boolean)
            .join(' | ')}
        >
          {row.notes}
        </HistoryItem>
      ))}
    </ul>
  )
}

export function OccurrenceDetailPage({ occurrenceId, onBack, onEdit }: OccurrenceDetailPageProps) {
  const apiClient = useApiClient()
  const queryClient = useQueryClient()
  const locale = useAppPreferencesStore((state) => state.locale)
  const authUser = useAuthSessionStore((state) => state.user)
  const copy = getOccurrenceCopy(locale)
  const canWriteOccurrences = hasAnyPermission(['occurrences.write'], authUser)
  const canManageOccurrences = hasAnyPermission(['occurrences.manage'], authUser)
  const canArchiveOccurrences = hasAnyPermission(['occurrences.archive'], authUser)
  const canReadDocuments = hasAnyPermission(['documents.read'], authUser)
  const canReadTimeline = canReadRelationshipModule('timeline', authUser)
  const [assignedUserId, setAssignedUserId] = useState('')
  const [assignmentNotes, setAssignmentNotes] = useState('')
  const [priority, setPriority] = useState<OccurrencePriority>('medium')
  const [priorityNotes, setPriorityNotes] = useState('')
  const [status, setStatus] = useState<MutableOccurrenceStatus>('open')
  const [statusNotes, setStatusNotes] = useState('')
  const [resolutionNotes, setResolutionNotes] = useState('')
  const [cancelNotes, setCancelNotes] = useState('')
  const [commentBody, setCommentBody] = useState('')
  const [isInternalComment, setIsInternalComment] = useState(false)
  const [documentId, setDocumentId] = useState('')
  const [documentLabel, setDocumentLabel] = useState('')
  const occurrenceQuery = useQuery({
    queryFn: () => getOccurrence(apiClient, occurrenceId, locale),
    queryKey: ['occurrences', 'detail', occurrenceId, locale],
  })
  const priorityOptionsQuery = useQuery({
    queryFn: () => listOccurrencePriorityOptions(apiClient, locale),
    queryKey: ['occurrences', 'priority-options', locale],
  })
  const statusOptionsQuery = useQuery({
    queryFn: () => listOccurrenceStatusOptions(apiClient, locale),
    queryKey: ['occurrences', 'status-options', locale],
  })
  const administratorsQuery = useQuery({
    queryFn: () => listAdministrators(apiClient, { pageSize: 100, status: 'active' }),
    queryKey: ['administrators', 'occurrence-detail-assignees'],
  })
  const documentsQuery = useQuery({
    enabled: canReadDocuments,
    queryFn: () =>
      listDocuments(apiClient, {
        category: 'occurrence',
        includeArchived: false,
        locale,
        pageSize: 100,
      }),
    queryKey: ['documents', 'occurrence-attachment-picker', locale],
  })
  const occurrence = occurrenceQuery.data
  const priorityOptions =
    priorityOptionsQuery.data ??
    (['low', 'medium', 'high', 'urgent'] as OccurrencePriority[]).map((option) => ({
      label: copy.terms.priorities[option],
      value: option,
    }))
  const statusOptions = useMemo(
    () =>
      (statusOptionsQuery.data ?? [])
        .filter((option) => mutableStatuses.includes(option.value as MutableOccurrenceStatus))
        .map((option) => ({
          label: option.label,
          value: option.value as MutableOccurrenceStatus,
        })),
    [statusOptionsQuery.data],
  )
  const assigneeOptions = useMemo(() => {
    const options =
      administratorsQuery.data?.items.map((administrator) => ({
        label: administrator.displayName,
        value: administrator.id,
      })) ?? []

    if (
      occurrence?.assignedUser &&
      !options.some((option) => option.value === occurrence.assignedUser?.id)
    ) {
      return [
        { label: occurrence.assignedUser.displayName, value: occurrence.assignedUser.id },
        ...options,
      ]
    }

    return options
  }, [administratorsQuery.data?.items, occurrence?.assignedUser])
  const documentOptions =
    documentsQuery.data?.items.map((document) => ({
      label: document.title,
      value: document.id,
    })) ?? []
  const workflowMutation = useMutation({
    mutationFn: async (request: WorkflowRequest) => {
      if (request.action === 'archive') {
        await archiveOccurrence(apiClient, occurrenceId)
        return undefined
      }

      if (request.action === 'assign') {
        return assignOccurrence(
          apiClient,
          occurrenceId,
          request.assignedUserId,
          request.notes,
          locale,
        )
      }

      if (request.action === 'attach') {
        return attachOccurrenceDocument(
          apiClient,
          occurrenceId,
          request.documentId,
          request.label,
          locale,
        )
      }

      if (request.action === 'cancel') {
        return cancelOccurrence(apiClient, occurrenceId, request.notes, locale)
      }

      if (request.action === 'comment') {
        return addOccurrenceComment(
          apiClient,
          occurrenceId,
          request.body,
          request.isInternal,
          locale,
        )
      }

      if (request.action === 'priority') {
        return changeOccurrencePriority(
          apiClient,
          occurrenceId,
          request.priority,
          request.notes,
          locale,
        )
      }

      if (request.action === 'resolve') {
        return resolveOccurrence(apiClient, occurrenceId, request.resolutionNotes, locale)
      }

      if (request.action === 'restore') {
        return restoreOccurrence(apiClient, occurrenceId, locale)
      }

      return changeOccurrenceStatus(apiClient, occurrenceId, request.status, request.notes, locale)
    },
    onSuccess: async (_, request) => {
      if (request.action === 'assign') {
        setAssignmentNotes('')
      }

      if (request.action === 'attach') {
        setDocumentId('')
        setDocumentLabel('')
      }

      if (request.action === 'comment') {
        setCommentBody('')
        setIsInternalComment(false)
      }

      if (request.action === 'priority') {
        setPriorityNotes('')
      }

      if (request.action === 'status') {
        setStatusNotes('')
      }

      if (request.action === 'resolve') {
        setResolutionNotes('')
      }

      if (request.action === 'cancel') {
        setCancelNotes('')
      }

      await queryClient.invalidateQueries({ queryKey: ['occurrences'] })
    },
  })

  useEffect(() => {
    if (!occurrence) {
      return
    }

    setAssignedUserId(occurrence.assignedUser?.id ?? '')
    setPriority(occurrence.priority.code)
    if (mutableStatuses.includes(occurrence.status.code as MutableOccurrenceStatus)) {
      setStatus(occurrence.status.code as MutableOccurrenceStatus)
    }
  }, [occurrence])

  if (occurrenceQuery.isLoading) {
    return <LoadingState title={copy.detail.loading} />
  }

  if (occurrenceQuery.isError || !occurrence) {
    return (
      <section style={{ display: 'grid', gap: 18 }}>
        <ActionButton icon={<ArrowLeft aria-hidden="true" size={16} />} onClick={onBack}>
          {copy.detail.actions.back}
        </ActionButton>
        <ErrorState title={copy.detail.error} onRetry={() => void occurrenceQuery.refetch()} />
      </section>
    )
  }

  const isArchived = occurrence.isArchived || occurrence.status.code === 'archived'
  const isTerminal = isTerminalStatus(occurrence.status.code)
  const canEdit = onEdit && canWriteOccurrences && !isTerminal
  const canUseWorkflow = canManageOccurrences && !isTerminal
  const canWriteLinkedNotes = canWriteOccurrences && !isArchived
  const canLinkDocuments = canWriteLinkedNotes && canReadDocuments

  return (
    <section style={{ display: 'grid', gap: 18 }}>
      <PageHeader
        actions={
          <>
            <ActionButton icon={<ArrowLeft aria-hidden="true" size={16} />} onClick={onBack}>
              {copy.detail.actions.back}
            </ActionButton>
            {canArchiveOccurrences && isArchived ? (
              <ActionButton
                icon={<RotateCcw aria-hidden="true" size={16} />}
                onClick={() => workflowMutation.mutate({ action: 'restore' })}
              >
                {copy.detail.actions.restore}
              </ActionButton>
            ) : null}
            {canArchiveOccurrences && !isArchived ? (
              <ActionButton
                icon={<Archive aria-hidden="true" size={16} />}
                onClick={() => workflowMutation.mutate({ action: 'archive' })}
                tone="danger"
              >
                {copy.detail.actions.archive}
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
                onClick: () => onEdit(occurrence),
              }
            : undefined
        }
        title={occurrence.title}
      />

      <DetailSection title={copy.detail.labels.title}>
        <DetailList
          columns={3}
          items={[
            {
              label: copy.detail.labels.status,
              value: (
                <StatusBadge
                  label={occurrence.status.label}
                  tone={statusTones[occurrence.status.code]}
                />
              ),
            },
            {
              label: copy.detail.labels.priority,
              value: (
                <StatusBadge
                  label={occurrence.priority.label}
                  tone={priorityTones[occurrence.priority.code]}
                />
              ),
            },
            {
              label: copy.detail.labels.type,
              value: occurrence.type.label,
            },
            {
              label: copy.detail.labels.assignedUser,
              value: occurrence.assignedUser?.displayName ?? copy.terms.none,
              description: occurrence.assignedUser?.email,
            },
            {
              label: copy.detail.labels.dueDate,
              value: occurrence.dueDate ? formatDate(occurrence.dueDate, { locale }) : '-',
            },
            {
              label: copy.detail.labels.property,
              value: formatEntity(occurrence.property, copy.terms.none),
            },
            {
              label: copy.detail.labels.resident,
              value: formatEntity(occurrence.resident, copy.terms.none),
            },
            {
              label: copy.detail.labels.contract,
              value: formatEntity(occurrence.contract, copy.terms.none),
            },
            {
              label: copy.detail.audit.createdAt,
              value: formatDateTime(occurrence.createdAt, { locale }),
            },
            {
              label: copy.detail.audit.updatedAt,
              value: occurrence.updatedAt ? formatDateTime(occurrence.updatedAt, { locale }) : '-',
            },
            {
              label: copy.detail.audit.archivedAt,
              value: occurrence.archivedAt
                ? formatDateTime(occurrence.archivedAt, { locale })
                : '-',
            },
            {
              label: copy.detail.labels.resolution,
              value:
                occurrence.resolutionNotes ?? occurrence.cancellationNotes ?? copy.detail.noNotes,
            },
            {
              label: copy.detail.labels.description,
              value: occurrence.description,
            },
          ]}
        />
      </DetailSection>

      <Tabs
        ariaLabel={copy.detail.relationshipTitle}
        tabs={[
          {
            content: getRelationshipPanels(occurrence, copy, authUser),
            id: 'relationships',
            label: copy.detail.relationshipTitle,
          },
          ...(canReadTimeline
            ? [
                {
                  content: <EntityTimelinePanel entityId={occurrence.id} entityType="occurrence" />,
                  id: 'timeline',
                  label: copy.detail.relationships.timeline,
                },
              ]
            : []),
          {
            content: (
              <DetailSection title={copy.detail.workflow.title}>
                <div style={{ display: 'grid', gap: 18 }}>
                  <div style={formGridStyle}>
                    <FormField label={copy.detail.workflow.assignee}>
                      {({ describedBy, id, isInvalid }) => (
                        <SelectInput
                          id={id}
                          aria-describedby={describedBy}
                          disabled={!canUseWorkflow}
                          isInvalid={isInvalid}
                          onChange={(event) => setAssignedUserId(event.currentTarget.value)}
                          options={assigneeOptions}
                          placeholder={copy.detail.workflow.assignee}
                          value={assignedUserId}
                        />
                      )}
                    </FormField>
                    <FormField label={copy.detail.workflow.notes}>
                      {({ describedBy, id, isInvalid }) => (
                        <TextInput
                          id={id}
                          aria-describedby={describedBy}
                          disabled={!canUseWorkflow}
                          isInvalid={isInvalid}
                          onChange={(event) => setAssignmentNotes(event.currentTarget.value)}
                          value={assignmentNotes}
                        />
                      )}
                    </FormField>
                    <ActionButton
                      disabled={!canUseWorkflow}
                      onClick={() =>
                        workflowMutation.mutate({
                          action: 'assign',
                          assignedUserId: optionalText(assignedUserId),
                          notes: optionalText(assignmentNotes),
                        })
                      }
                      style={{ alignSelf: 'end' }}
                      type="button"
                    >
                      {copy.detail.workflow.assign}
                    </ActionButton>
                  </div>

                  <div style={formGridStyle}>
                    <FormField label={copy.detail.labels.priority}>
                      {({ describedBy, id, isInvalid }) => (
                        <SelectInput
                          id={id}
                          aria-describedby={describedBy}
                          disabled={!canUseWorkflow}
                          isInvalid={isInvalid}
                          onChange={(event) =>
                            setPriority(event.currentTarget.value as OccurrencePriority)
                          }
                          options={priorityOptions}
                          value={priority}
                        />
                      )}
                    </FormField>
                    <FormField label={copy.detail.workflow.notes}>
                      {({ describedBy, id, isInvalid }) => (
                        <TextInput
                          id={id}
                          aria-describedby={describedBy}
                          disabled={!canUseWorkflow}
                          isInvalid={isInvalid}
                          onChange={(event) => setPriorityNotes(event.currentTarget.value)}
                          value={priorityNotes}
                        />
                      )}
                    </FormField>
                    <ActionButton
                      disabled={!canUseWorkflow}
                      onClick={() =>
                        workflowMutation.mutate({
                          action: 'priority',
                          notes: optionalText(priorityNotes),
                          priority,
                        })
                      }
                      style={{ alignSelf: 'end' }}
                      type="button"
                    >
                      {copy.detail.workflow.changePriority}
                    </ActionButton>
                  </div>

                  <div style={formGridStyle}>
                    <FormField label={copy.detail.labels.status}>
                      {({ describedBy, id, isInvalid }) => (
                        <SelectInput
                          id={id}
                          aria-describedby={describedBy}
                          disabled={!canUseWorkflow}
                          isInvalid={isInvalid}
                          onChange={(event) =>
                            setStatus(event.currentTarget.value as MutableOccurrenceStatus)
                          }
                          options={statusOptions}
                          value={status}
                        />
                      )}
                    </FormField>
                    <FormField label={copy.detail.workflow.notes}>
                      {({ describedBy, id, isInvalid }) => (
                        <TextInput
                          id={id}
                          aria-describedby={describedBy}
                          disabled={!canUseWorkflow}
                          isInvalid={isInvalid}
                          onChange={(event) => setStatusNotes(event.currentTarget.value)}
                          value={statusNotes}
                        />
                      )}
                    </FormField>
                    <ActionButton
                      disabled={!canUseWorkflow}
                      onClick={() =>
                        workflowMutation.mutate({
                          action: 'status',
                          notes: optionalText(statusNotes),
                          status,
                        })
                      }
                      style={{ alignSelf: 'end' }}
                      type="button"
                    >
                      {copy.detail.workflow.changeStatus}
                    </ActionButton>
                  </div>

                  <div style={formGridStyle}>
                    <FormField label={copy.detail.workflow.resolutionNotes}>
                      {({ describedBy, id, isInvalid }) => (
                        <TextAreaInput
                          id={id}
                          aria-describedby={describedBy}
                          disabled={!canUseWorkflow}
                          isInvalid={isInvalid}
                          onChange={(event) => setResolutionNotes(event.currentTarget.value)}
                          value={resolutionNotes}
                        />
                      )}
                    </FormField>
                    <ActionButton
                      disabled={!canUseWorkflow || !optionalText(resolutionNotes)}
                      icon={<CheckCircle2 aria-hidden="true" size={16} />}
                      onClick={() =>
                        workflowMutation.mutate({
                          action: 'resolve',
                          resolutionNotes,
                        })
                      }
                      style={{ alignSelf: 'end' }}
                      type="button"
                    >
                      {copy.detail.actions.resolve}
                    </ActionButton>
                  </div>

                  <div style={formGridStyle}>
                    <FormField label={copy.detail.workflow.cancelNotes}>
                      {({ describedBy, id, isInvalid }) => (
                        <TextAreaInput
                          id={id}
                          aria-describedby={describedBy}
                          disabled={!canUseWorkflow}
                          isInvalid={isInvalid}
                          onChange={(event) => setCancelNotes(event.currentTarget.value)}
                          value={cancelNotes}
                        />
                      )}
                    </FormField>
                    <ActionButton
                      disabled={!canUseWorkflow}
                      icon={<XCircle aria-hidden="true" size={16} />}
                      onClick={() =>
                        workflowMutation.mutate({
                          action: 'cancel',
                          notes: optionalText(cancelNotes),
                        })
                      }
                      style={{ alignSelf: 'end' }}
                      tone="danger"
                      type="button"
                    >
                      {copy.detail.workflow.cancelOccurrence}
                    </ActionButton>
                  </div>
                </div>
              </DetailSection>
            ),
            id: 'workflow',
            label: copy.detail.workflow.title,
          },
          {
            content: (
              <DetailSection title={copy.detail.comments.title}>
                <div style={{ display: 'grid', gap: 14 }}>
                  {occurrence.comments.length === 0 ? (
                    <p>{copy.detail.comments.empty}</p>
                  ) : (
                    <ul style={listStyle}>
                      {occurrence.comments.map((comment) => (
                        <HistoryItem
                          key={comment.id}
                          title={comment.authorDisplayName ?? copy.terms.none}
                          meta={formatDateTime(comment.createdAt, { locale })}
                        >
                          {comment.body}
                        </HistoryItem>
                      ))}
                    </ul>
                  )}

                  {canWriteLinkedNotes ? (
                    <div style={{ display: 'grid', gap: 12 }}>
                      <FormField label={copy.detail.comments.body}>
                        {({ describedBy, id, isInvalid }) => (
                          <TextAreaInput
                            id={id}
                            aria-describedby={describedBy}
                            isInvalid={isInvalid}
                            onChange={(event) => setCommentBody(event.currentTarget.value)}
                            value={commentBody}
                          />
                        )}
                      </FormField>
                      <CheckboxInput
                        checked={isInternalComment}
                        label={copy.detail.comments.internal}
                        onChange={(event) => setIsInternalComment(event.currentTarget.checked)}
                      />
                      <ActionButton
                        disabled={!optionalText(commentBody)}
                        icon={<MessageSquarePlus aria-hidden="true" size={16} />}
                        onClick={() =>
                          workflowMutation.mutate({
                            action: 'comment',
                            body: commentBody,
                            isInternal: isInternalComment,
                          })
                        }
                        type="button"
                      >
                        {copy.detail.comments.add}
                      </ActionButton>
                    </div>
                  ) : null}
                </div>
              </DetailSection>
            ),
            id: 'comments',
            label: copy.detail.comments.title,
          },
          ...(canReadDocuments
            ? [
                {
                  content: (
                    <DetailSection title={copy.detail.attachment.title}>
                      <div style={{ display: 'grid', gap: 14 }}>
                        {occurrence.attachments.length === 0 ? (
                          <p>{copy.detail.attachment.empty}</p>
                        ) : (
                          <RelationshipPanel
                            items={occurrence.attachments.map((attachment) => ({
                              description: formatDateTime(attachment.createdAt, { locale }),
                              href: attachment.route,
                              id: attachment.documentId,
                              title: attachment.label ?? attachment.documentId,
                            }))}
                            title={copy.detail.attachment.title}
                          />
                        )}

                        {canLinkDocuments ? (
                          <div style={formGridStyle}>
                            <FormField label={copy.detail.attachment.documentId}>
                              {({ describedBy, id, isInvalid }) => (
                                <SelectInput
                                  id={id}
                                  aria-describedby={describedBy}
                                  isInvalid={isInvalid}
                                  onChange={(event) => setDocumentId(event.currentTarget.value)}
                                  options={documentOptions}
                                  placeholder={copy.detail.attachment.documentId}
                                  value={documentId}
                                />
                              )}
                            </FormField>
                            <FormField label={copy.detail.attachment.label}>
                              {({ describedBy, id, isInvalid }) => (
                                <TextInput
                                  id={id}
                                  aria-describedby={describedBy}
                                  isInvalid={isInvalid}
                                  onChange={(event) => setDocumentLabel(event.currentTarget.value)}
                                  value={documentLabel}
                                />
                              )}
                            </FormField>
                            <ActionButton
                              disabled={!optionalText(documentId)}
                              icon={<Paperclip aria-hidden="true" size={16} />}
                              onClick={() =>
                                workflowMutation.mutate({
                                  action: 'attach',
                                  documentId,
                                  label: optionalText(documentLabel),
                                })
                              }
                              style={{ alignSelf: 'end' }}
                              type="button"
                            >
                              {copy.detail.attachment.add}
                            </ActionButton>
                          </div>
                        ) : null}
                      </div>
                    </DetailSection>
                  ),
                  id: 'attachments',
                  label: copy.detail.attachment.title,
                },
              ]
            : []),
          {
            content: (
              <div style={{ display: 'grid', gap: 14 }}>
                <DetailSection title={copy.detail.history.status}>
                  {renderStatusHistory(occurrence.statusHistory, locale, copy)}
                </DetailSection>
                <DetailSection title={copy.detail.history.priority}>
                  {renderPriorityHistory(occurrence.priorityHistory, locale, copy)}
                </DetailSection>
                <DetailSection title={copy.detail.history.assignment}>
                  {renderAssignmentHistory(occurrence.assignmentHistory, locale, copy)}
                </DetailSection>
              </div>
            ),
            id: 'history',
            label: copy.detail.history.status,
          },
        ]}
      />
    </section>
  )
}
