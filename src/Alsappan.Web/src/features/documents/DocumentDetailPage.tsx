import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Archive, ArrowLeft, Download, Pencil, RotateCcw, Upload } from 'lucide-react'
import {
  ActionButton,
  DataTable,
  DetailList,
  DetailSection,
  Drawer,
  EmptyState,
  ErrorState,
  LoadingState,
  PageHeader,
  RelationshipPanel,
  StatusBadge,
  Tabs,
} from '../../components'
import {
  archiveDocument,
  downloadDocument,
  downloadDocumentVersion,
  getDocument,
  listAllowedDocumentFileTypes,
  restoreDocument,
  uploadDocumentVersion,
  type DocumentDetail,
  type DocumentVersionUploadRequest,
} from '../../lib/api/documents'
import { useApiClient } from '../../lib/api/ApiClientContext'
import { formatDateTime } from '../../lib/format'
import { useAppPreferencesStore } from '../../stores/useAppPreferencesStore'
import { useAuthSessionStore } from '../../stores/useAuthSessionStore'
import { hasAnyPermission } from '../identity/session'
import { getDocumentCopy } from './documentCopy'
import { formatFileSize, getDocumentStatusTone } from './documentFormat'
import { DocumentVersionForm } from './DocumentVersionForm'
import { saveDownloadedDocument } from './downloadFile'

export type DocumentDetailPageProps = {
  documentId: string
  onBack: () => void
  onEdit?: (document: DocumentDetail) => void
}

function getLinksPanel(document: DocumentDetail, copy: ReturnType<typeof getDocumentCopy>) {
  return (
    <div
      style={{
        display: 'grid',
        gap: 12,
        gridTemplateColumns: 'repeat(auto-fit, minmax(260px, 1fr))',
      }}
    >
      <RelationshipPanel
        emptyState={copy.detail.emptyLinks}
        items={document.links.map((link) => ({
          description: copy.terms.entityTypes[link.entityType],
          href: link.route,
          id: `${link.entityType}-${link.entityId}`,
          meta: link.entityId,
          title: link.label ?? link.entityId,
        }))}
        title={copy.detail.labels.links}
      />

      <RelationshipPanel
        items={[
          {
            description: document.timelineRoute,
            href: document.timelineRoute,
            id: 'timeline-link',
            title: copy.detail.relationships.timeline,
          },
        ]}
        title={copy.detail.relationships.timeline}
      />

      <RelationshipPanel
        items={[
          {
            description: document.auditRoute,
            href: document.auditRoute,
            id: 'audit-link',
            title: copy.detail.relationships.audit,
          },
        ]}
        title={copy.detail.relationships.audit}
      />
    </div>
  )
}

export function DocumentDetailPage({ documentId, onBack, onEdit }: DocumentDetailPageProps) {
  const apiClient = useApiClient()
  const queryClient = useQueryClient()
  const locale = useAppPreferencesStore((state) => state.locale)
  const authUser = useAuthSessionStore((state) => state.user)
  const copy = getDocumentCopy(locale)
  const canWriteDocuments = hasAnyPermission(['documents.write'], authUser)
  const canArchiveDocuments = hasAnyPermission(['documents.archive'], authUser)
  const [isVersionDrawerOpen, setIsVersionDrawerOpen] = useState(false)
  const documentQuery = useQuery({
    queryFn: () => getDocument(apiClient, documentId, locale),
    queryKey: ['documents', 'detail', documentId, locale],
  })
  const allowedFileTypesQuery = useQuery({
    queryFn: () => listAllowedDocumentFileTypes(apiClient, locale),
    queryKey: ['documents', 'allowed-file-types', locale],
  })
  const allowedFileTypes = (allowedFileTypesQuery.data ?? []).map((option) => option.value)
  const versionMutation = useMutation({
    mutationFn: (request: DocumentVersionUploadRequest) =>
      uploadDocumentVersion(apiClient, documentId, request, locale),
    onSuccess: async () => {
      setIsVersionDrawerOpen(false)
      await queryClient.invalidateQueries({ queryKey: ['documents'] })
    },
  })
  const downloadMutation = useMutation({
    mutationFn: (request: { versionNumber?: number }) =>
      request.versionNumber
        ? downloadDocumentVersion(apiClient, documentId, request.versionNumber)
        : downloadDocument(apiClient, documentId),
    onSuccess: (download) => saveDownloadedDocument(download),
  })
  const lifecycleMutation = useMutation({
    mutationFn: async (request: { action: 'archive' | 'restore' }) => {
      if (request.action === 'archive') {
        await archiveDocument(apiClient, documentId)
        return
      }

      await restoreDocument(apiClient, documentId, locale)
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['documents'] })
    },
  })
  const document = documentQuery.data

  if (documentQuery.isLoading) {
    return <LoadingState title={copy.detail.loading} />
  }

  if (documentQuery.isError || !document) {
    return (
      <section style={{ display: 'grid', gap: 18 }}>
        <ActionButton icon={<ArrowLeft aria-hidden="true" size={16} />} onClick={onBack}>
          {copy.detail.actions.back}
        </ActionButton>
        <ErrorState title={copy.detail.error} onRetry={() => void documentQuery.refetch()} />
      </section>
    )
  }

  const canEditDocument = canWriteDocuments && !document.isArchived && onEdit
  const canUploadVersion = canWriteDocuments && !document.isArchived
  const lifecycleAction = document.isArchived
    ? {
        icon: <RotateCcw aria-hidden="true" size={16} />,
        label: copy.detail.actions.restore,
        onClick: () => lifecycleMutation.mutate({ action: 'restore' }),
        tone: 'secondary' as const,
      }
    : {
        icon: <Archive aria-hidden="true" size={16} />,
        label: copy.detail.actions.archive,
        onClick: () => lifecycleMutation.mutate({ action: 'archive' }),
        tone: 'danger' as const,
      }

  return (
    <section style={{ display: 'grid', gap: 18 }}>
      <PageHeader
        actions={
          <>
            <ActionButton icon={<ArrowLeft aria-hidden="true" size={16} />} onClick={onBack}>
              {copy.detail.actions.back}
            </ActionButton>
            <ActionButton
              icon={<Download aria-hidden="true" size={16} />}
              isLoading={downloadMutation.isPending}
              onClick={() => downloadMutation.mutate({})}
            >
              {copy.detail.actions.download}
            </ActionButton>
            {canUploadVersion ? (
              <ActionButton
                icon={<Upload aria-hidden="true" size={16} />}
                onClick={() => setIsVersionDrawerOpen(true)}
              >
                {copy.detail.actions.uploadVersion}
              </ActionButton>
            ) : null}
            {canArchiveDocuments ? (
              <ActionButton
                icon={lifecycleAction.icon}
                isLoading={lifecycleMutation.isPending}
                onClick={lifecycleAction.onClick}
                tone={lifecycleAction.tone}
              >
                {lifecycleAction.label}
              </ActionButton>
            ) : null}
          </>
        }
        description={copy.detail.pageDescription}
        primaryAction={
          canEditDocument
            ? {
                icon: <Pencil aria-hidden="true" size={18} />,
                label: copy.detail.actions.edit,
                onClick: () => onEdit(document),
              }
            : undefined
        }
        title={document.title}
      />

      <DetailSection title={copy.detail.summaryTitle}>
        <DetailList
          columns={3}
          items={[
            {
              label: copy.detail.labels.status,
              value: (
                <StatusBadge
                  label={document.status.label ?? copy.terms.status[document.status.code]}
                  tone={getDocumentStatusTone(document.status)}
                />
              ),
            },
            {
              label: copy.detail.labels.category,
              value: document.categoryLabel ?? copy.terms.categories[document.category],
            },
            {
              label: copy.detail.labels.currentVersion,
              value: copy.terms.versionNumber(document.currentVersionNumber),
            },
            {
              label: copy.detail.labels.fileName,
              value: document.fileName,
            },
            {
              label: copy.detail.labels.fileType,
              value: document.contentType,
            },
            {
              label: copy.detail.labels.size,
              value: copy.terms.sizeBytes(formatFileSize(document.sizeBytes, locale)),
            },
            {
              label: copy.detail.audit.uploadedAt,
              value: formatDateTime(document.uploadedAt, { locale }),
            },
            {
              label: copy.detail.audit.createdAt,
              value: formatDateTime(document.createdAt, { locale }),
            },
            {
              label: copy.detail.audit.updatedAt,
              value: document.updatedAt ? formatDateTime(document.updatedAt, { locale }) : '-',
            },
            {
              label: copy.detail.audit.archivedAt,
              value: document.archivedAt ? formatDateTime(document.archivedAt, { locale }) : '-',
            },
            {
              label: copy.detail.labels.description,
              value: document.description ?? copy.detail.noDescription,
            },
          ]}
        />
      </DetailSection>

      <Tabs
        ariaLabel={copy.detail.relationshipTitle}
        tabs={[
          {
            content: getLinksPanel(document, copy),
            id: 'relationships',
            label: copy.detail.relationshipTitle,
          },
          {
            content: (
              <DataTable
                columns={[
                  {
                    cell: (version) => copy.terms.versionNumber(version.versionNumber),
                    header: copy.detail.columns.version,
                    id: 'version',
                    width: 120,
                  },
                  {
                    cell: (version) => (
                      <div style={{ display: 'grid', gap: 4, minWidth: 0 }}>
                        <strong>{version.fileName}</strong>
                        <span style={{ color: 'var(--als-color-text-muted, #64748b)' }}>
                          {formatFileSize(version.sizeBytes, locale)}
                        </span>
                      </div>
                    ),
                    header: copy.detail.columns.file,
                    id: 'file',
                  },
                  {
                    cell: (version) => formatDateTime(version.uploadedAt, { locale }),
                    header: copy.detail.columns.uploadedAt,
                    id: 'uploadedAt',
                    width: 180,
                  },
                  {
                    cell: (version) => version.notes ?? copy.detail.noNotes,
                    header: copy.detail.columns.notes,
                    id: 'notes',
                  },
                  {
                    align: 'right',
                    cell: (version) => (
                      <ActionButton
                        aria-label={`${copy.list.download} ${copy.terms
                          .versionNumber(version.versionNumber)
                          .toLocaleLowerCase(locale)}`}
                        icon={<Download aria-hidden="true" size={16} />}
                        onClick={() =>
                          downloadMutation.mutate({ versionNumber: version.versionNumber })
                        }
                        size="sm"
                      >
                        {copy.list.download}
                      </ActionButton>
                    ),
                    header: copy.list.columns.actions,
                    id: 'actions',
                    width: 132,
                  },
                ]}
                emptyState={<EmptyState title={copy.detail.emptyVersions} />}
                getRowKey={(version) => version.id}
                loadingLabel={copy.detail.loading}
                rows={document.versions}
              />
            ),
            id: 'versions',
            label: copy.detail.versionsTitle,
          },
        ]}
      />

      <Drawer
        isOpen={isVersionDrawerOpen}
        onOpenChange={(isOpen) => setIsVersionDrawerOpen(isOpen)}
        title={copy.form.versionTitle}
        width={520}
      >
        <DocumentVersionForm
          allowedFileTypes={allowedFileTypes}
          isSubmitting={versionMutation.isPending}
          onCancel={() => setIsVersionDrawerOpen(false)}
          onSubmit={async (values) => {
            await versionMutation.mutateAsync(values)
          }}
        />
      </Drawer>
    </section>
  )
}
