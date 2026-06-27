import type { Page, Route } from '@playwright/test'

type HttpMethod = 'DELETE' | 'GET' | 'POST' | 'PUT'
type JsonRecord = Record<string, unknown>

type Organization = {
  branding?: JsonRecord
  currency: string
  displayName: string
  id: string
  locale: string
  name: string
  permissionCodes: string[]
  roleCodes: string[]
  slug: string
}

type MockEntity = JsonRecord & {
  id: string
  organizationId: string
}

type MockApiState = ReturnType<typeof createMockApiState>

const now = '2026-06-27T10:00:00.000Z'
const defaultPageSize = 10

const adminPermissions = ['*']

const organizations: Organization[] = [
  {
    branding: {
      displayName: 'Alsappan',
      logoAlt: 'Alsappan',
      primaryColor: '#1877f2',
      primaryForegroundColor: '#ffffff',
    },
    currency: 'BRL',
    displayName: 'Alsappan Demo',
    id: 'org-alsappan',
    locale: 'pt-BR',
    name: 'Alsappan',
    permissionCodes: adminPermissions,
    roleCodes: ['Administrador'],
    slug: 'alsappan',
  },
  {
    branding: {
      accentColor: '#f59e0b',
      accentForegroundColor: '#111827',
      displayName: 'Moradas Prime',
      logoAlt: 'Moradas Prime',
      primaryColor: '#0f766e',
      primaryForegroundColor: '#ffffff',
    },
    currency: 'BRL',
    displayName: 'Moradas Prime',
    id: 'org-moradas',
    locale: 'pt-BR',
    name: 'Moradas Prime',
    permissionCodes: adminPermissions,
    roleCodes: ['Administrador'],
    slug: 'moradas-prime',
  },
]

function label(code: string, value: string, tone = 'neutral') {
  return { code, label: value, tone }
}

function option(value: string, labelValue: string) {
  return { label: labelValue, value }
}

function money(amount: number, currency = 'BRL') {
  return { amount, currency }
}

function entity(id: string, name: string, route?: string, description?: string) {
  return { description, id, name, route }
}

function createMockApiState() {
  const properties: MockEntity[] = [
    property('org-alsappan', 'prop-calabria', 'Casa Calabria', 'rented', 2800, 1),
    property(
      'org-alsappan',
      'prop-long-name',
      'Apartamento com nome extremamente longo para validar quebra de texto responsiva',
      'available',
      4200,
      2,
    ),
    property('org-moradas', 'prop-moradas', 'Loft Moradas Prime', 'available', 3600, 1),
  ]

  const residents: MockEntity[] = [
    resident('org-alsappan', 'resident-joao', 'Joao da Silva', 'joao@example.com'),
    resident('org-alsappan', 'resident-ana', 'Ana Portal', 'resident@example.com'),
    resident('org-moradas', 'resident-moradas', 'Clara Prime', 'clara@example.com'),
  ]

  const administrators: MockEntity[] = [
    {
      displayName: 'Paulo Daniel Carneiro',
      email: 'paulo.carneiro.dev@protonmail.com',
      id: 'admin-paulo',
      organizationId: 'org-alsappan',
      permissionCodes: adminPermissions,
      roleCodes: ['Administrador'],
      roleLabels: ['Administrador'],
      status: 'active',
      statusLabel: 'Ativo',
      updatedAt: now,
    },
    {
      displayName: 'Operadora Moradas',
      email: 'operadora@example.com',
      id: 'admin-moradas',
      organizationId: 'org-moradas',
      permissionCodes: adminPermissions,
      roleCodes: ['Administrador'],
      roleLabels: ['Administrador'],
      status: 'active',
      statusLabel: 'Ativo',
      updatedAt: now,
    },
  ]

  const contracts: MockEntity[] = [
    contract('org-alsappan', 'contract-calabria', properties[0], residents[1]),
    contract('org-moradas', 'contract-moradas', properties[2], residents[2]),
  ]

  const documents: MockEntity[] = [
    document('org-alsappan', 'document-contract', 'Contrato assinado', 'contract', [
      {
        entityId: 'contract-calabria',
        entityType: 'contract',
        label: 'Contrato Calabria',
        route: '/contratos?contractId=contract-calabria',
      },
    ]),
  ]

  const payments: MockEntity[] = [
    payment(
      'org-alsappan',
      'payment-june',
      'Aluguel junho - Casa Calabria',
      properties[0],
      residents[1],
      contracts[0],
    ),
    payment(
      'org-moradas',
      'payment-moradas',
      'Aluguel julho - Moradas',
      properties[2],
      residents[2],
      contracts[1],
    ),
  ]

  const occurrences: MockEntity[] = [
    occurrence(
      'org-alsappan',
      'occurrence-leak',
      'Vazamento na cozinha',
      properties[0],
      residents[1],
      contracts[0],
    ),
  ]

  const inspections: MockEntity[] = [
    inspection(
      'org-alsappan',
      'inspection-entry',
      'Vistoria de entrada',
      properties[0],
      residents[1],
      contracts[0],
    ),
  ]

  const notifications: MockEntity[] = [
    notification(
      'org-alsappan',
      'notification-payment',
      'Pagamento vencido',
      '/pagamentos?search=aluguel',
    ),
    notification('org-moradas', 'notification-moradas', 'Contrato atualizado', '/contratos'),
  ]

  const auditEntries: MockEntity[] = [
    audit(
      'org-alsappan',
      'audit-property-created',
      'property.created',
      'Imovel criado',
      'Casa Calabria',
      'property',
      'prop-calabria',
    ),
    audit(
      'org-moradas',
      'audit-moradas-created',
      'property.created',
      'Imovel criado',
      'Loft Moradas Prime',
      'property',
      'prop-moradas',
    ),
  ]

  return {
    activeAccount: 'anonymous' as 'admin' | 'anonymous' | 'resident',
    activeOrganizationId: 'org-alsappan',
    activeResidentId: 'resident-ana',
    adminPermissions: [...adminPermissions],
    administrators,
    auditEntries,
    contracts,
    documents,
    inspections,
    notifications,
    occurrences,
    organizations: organizations.map((organizationItem) => ({ ...organizationItem })),
    payments,
    properties,
    residents,
    settings: createSettingsDashboard(),
  }
}

export async function installMockApi(page: Page, state = createMockApiState()) {
  await page.route('**/*', async (route) => {
    const url = new URL(route.request().url())

    if (!url.pathname.startsWith('/v1/')) {
      await route.continue()
      return
    }

    await handleApiRoute(route, state)
  })

  return state
}

function property(
  organizationId: string,
  id: string,
  name: string,
  status: string,
  rent: number,
  garageSpaceCount: number,
): MockEntity {
  return {
    address: {
      city: organizationId === 'org-moradas' ? 'Campinas' : 'Sao Paulo',
      complement: 'Casa 1',
      countryCode: 'BR',
      neighborhood: organizationId === 'org-moradas' ? 'Cambuí' : 'Vila Fazzione',
      number: '82',
      postalCode: '01000-000',
      stateCode: 'SP',
      streetLine: organizationId === 'org-moradas' ? 'Rua Prime' : 'Rua Calabria',
    },
    createdAt: now,
    description: 'Imovel usado nos testes E2E',
    garageSpaceCount,
    garageSummary: garageSpaceCount > 0 ? `${garageSpaceCount} vaga(s)` : 'Nao possui',
    id,
    isArchived: status === 'archived',
    name,
    notes: 'Sol da manha',
    organizationId,
    relationships: relationshipSummaries(id),
    status: propertyStatus(status),
    suggestedRent: money(rent),
    type: 'house',
    typeLabel: 'Casa',
    updatedAt: now,
  }
}

function resident(organizationId: string, id: string, fullName: string, email: string): MockEntity {
  return {
    contactSummary: `${email} · +55 11 99999-0000`,
    createdAt: now,
    documentIdentifier: '123.456.789-00',
    documentType: 'CPF',
    email,
    emergencyContact: {
      name: 'Contato de emergencia',
      phone: '+55 11 98888-0000',
      relationship: 'Familiar',
    },
    fullName,
    id,
    isArchived: false,
    isSensitiveMasked: false,
    notes: 'Morador de teste',
    organizationId,
    phone: '+55 11 99999-0000',
    portalStatus: label('active', 'Ativo', 'success'),
    preferredName: fullName.split(' ')[0],
    privacyFlags: [],
    relationships: relationshipSummaries(id),
    status: label('active', 'Ativo', 'success'),
    updatedAt: now,
  }
}

function contract(
  organizationId: string,
  id: string,
  propertyItem: MockEntity,
  residentItem: MockEntity,
): MockEntity {
  const propertySummary = entity(
    String(propertyItem.id),
    String(propertyItem.name),
    `/imoveis?propertyId=${propertyItem.id}`,
  )
  const residentSummary = { id: residentItem.id, isPrimary: true, name: residentItem.fullName }

  return {
    adjustmentIndex: 'ipca',
    adjustmentIndexLabel: 'IPCA',
    adjustmentIntervalMonths: 12,
    createdAt: now,
    depositAmount: money(2500),
    documents: [
      {
        category: 'contract',
        count: 1,
        documentId: 'document-contract',
        label: 'Contrato assinado',
        route: '/documentos?linkedEntityType=contract&linkedEntityId=contract-calabria',
      },
    ],
    dueDay: 10,
    endDate: '2027-06-30',
    generatePaymentsAutomatically: true,
    id,
    isArchived: false,
    monthlyRent: money(2800),
    notes: 'Contrato ativo para fluxo E2E',
    organizationId,
    primaryResident: residentSummary,
    property: propertySummary,
    relationships: relationshipSummaries(id),
    residents: [residentSummary],
    startDate: '2026-07-01',
    status: label('active', 'Ativo', 'success'),
    updatedAt: now,
  }
}

function document(
  organizationId: string,
  id: string,
  title: string,
  category: string,
  links: unknown[],
): MockEntity {
  return {
    auditRoute: `/auditoria?entityType=document&entityId=${id}`,
    category,
    categoryLabel: documentCategoryLabel(category),
    contentType: 'application/pdf',
    createdAt: now,
    currentVersionNumber: 1,
    description: 'Documento de teste',
    downloadRoute: `/v1/documents/${id}/download`,
    fileName: `${title.toLowerCase().replace(/\s+/g, '-')}.pdf`,
    id,
    isArchived: false,
    links,
    organizationId,
    sizeBytes: 2048,
    status: label('active', 'Ativo', 'success'),
    timelineRoute: `/timeline?entityType=document&entityId=${id}`,
    title,
    updatedAt: now,
    uploadedAt: now,
    versions: [
      {
        contentType: 'application/pdf',
        fileName: `${title.toLowerCase().replace(/\s+/g, '-')}.pdf`,
        id: `${id}-version-1`,
        notes: 'Versao inicial',
        sizeBytes: 2048,
        uploadedAt: now,
        versionNumber: 1,
      },
    ],
  }
}

function payment(
  organizationId: string,
  id: string,
  title: string,
  propertyItem: MockEntity,
  residentItem: MockEntity,
  contractItem: MockEntity,
): MockEntity {
  return {
    amount: money(2800),
    auditRoute: `/auditoria?entityType=payment&entityId=${id}`,
    balance: money(2800),
    contract: entity(
      String(contractItem.id),
      String(contractItem.property?.name ?? 'Contrato'),
      `/contratos?contractId=${contractItem.id}`,
    ),
    createdAt: now,
    description: 'Aluguel mensal',
    discountAmount: money(0),
    dueDate: '2026-06-20',
    grossAmount: money(2800),
    id,
    isArchived: false,
    isOverdue: true,
    organizationId,
    penaltyAmount: money(0),
    preferredMethod: 'pix',
    preferredMethodLabel: 'Pix',
    property: entity(
      String(propertyItem.id),
      String(propertyItem.name),
      `/imoveis?propertyId=${propertyItem.id}`,
    ),
    receiptDocuments: [],
    reconciliationStatus: label('pending', 'Pendente', 'warning'),
    resident: entity(
      String(residentItem.id),
      String(residentItem.fullName),
      `/moradores?residentId=${residentItem.id}`,
    ),
    settledAmount: money(0),
    status: label('pending', 'Pendente', 'warning'),
    timelineRoute: `/timeline?entityType=payment&entityId=${id}`,
    title,
    transactions: [],
    updatedAt: now,
  }
}

function occurrence(
  organizationId: string,
  id: string,
  title: string,
  propertyItem: MockEntity,
  residentItem: MockEntity,
  contractItem: MockEntity,
): MockEntity {
  return {
    assignedUser: {
      displayName: 'Paulo Daniel Carneiro',
      email: 'paulo.carneiro.dev@protonmail.com',
      id: 'admin-paulo',
    },
    assignmentHistory: [],
    attachments: [],
    auditRoute: `/auditoria?entityType=occurrence&entityId=${id}`,
    comments: [],
    contract: entity(
      String(contractItem.id),
      String(contractItem.property?.name ?? 'Contrato'),
      `/contratos?contractId=${contractItem.id}`,
    ),
    createdAt: now,
    description: 'Vazamento identificado pela vistoria.',
    dueDate: '2026-07-31',
    id,
    isArchived: false,
    isUnresolved: true,
    organizationId,
    priority: label('high', 'Alta', 'danger'),
    priorityHistory: [],
    property: entity(
      String(propertyItem.id),
      String(propertyItem.name),
      `/imoveis?propertyId=${propertyItem.id}`,
    ),
    resident: entity(
      String(residentItem.id),
      String(residentItem.fullName),
      `/moradores?residentId=${residentItem.id}`,
    ),
    status: label('assigned', 'Atribuida', 'warning'),
    statusHistory: [],
    timelineRoute: `/timeline?entityType=occurrence&entityId=${id}`,
    title,
    type: label('maintenance', 'Manutencao', 'info'),
    updatedAt: now,
  }
}

function inspection(
  organizationId: string,
  id: string,
  title: string,
  propertyItem: MockEntity,
  residentItem: MockEntity,
  contractItem: MockEntity,
): MockEntity {
  return {
    assignee: entity('admin-paulo', 'Paulo Daniel Carneiro'),
    auditRoute: `/auditoria?entityType=inspection&entityId=${id}`,
    checklistItems: [
      checklistItem(`${id}-item-1`, 'Sala', 'Pintura', 'good', true),
      checklistItem(`${id}-item-2`, 'Cozinha', 'Hidraulica', 'attention', true),
    ],
    contract: entity(
      String(contractItem.id),
      String(contractItem.property?.name ?? 'Contrato'),
      `/contratos?contractId=${contractItem.id}`,
    ),
    createdAt: now,
    id,
    isArchived: false,
    isPending: true,
    linkedDocuments: [
      {
        documentId: 'document-contract',
        kind: label('report', 'Laudo', 'info'),
        label: 'Laudo final',
        route: '/documentos/document-contract',
      },
    ],
    notes: 'Vistoria agendada via E2E',
    organizationId,
    photoDocuments: [
      {
        documentId: 'document-contract',
        kind: label('photo', 'Foto', 'info'),
        label: 'Foto da sala',
        route: '/documentos/document-contract',
      },
    ],
    progress: { completedItems: 2, percentage: 100, totalItems: 2 },
    property: entity(
      String(propertyItem.id),
      String(propertyItem.name),
      `/imoveis?propertyId=${propertyItem.id}`,
    ),
    resident: entity(
      String(residentItem.id),
      String(residentItem.fullName),
      `/moradores?residentId=${residentItem.id}`,
    ),
    scheduledAt: '2026-06-30T10:00:00.000Z',
    signatureSlots: [
      {
        id: `${id}-signature`,
        isRequired: true,
        isSigned: false,
        signerName: 'Joao da Silva',
        signerRole: 'Morador',
      },
    ],
    status: label('scheduled', 'Agendada', 'info'),
    timelineRoute: `/timeline?entityType=inspection&entityId=${id}`,
    title,
    type: label('move-in', 'Entrada', 'info'),
    updatedAt: now,
  }
}

function notification(
  organizationId: string,
  id: string,
  title: string,
  deepLink: string,
): MockEntity {
  return {
    category: label('payments', 'Pagamentos', 'warning'),
    channel: label('in-app', 'Aplicativo', 'info'),
    createdAt: now,
    deepLink,
    deliveryStatus: label('delivered', 'Entregue', 'success'),
    eventId: `${id}-event`,
    eventName: 'payment.overdue',
    eventTypeLabel: 'Pagamento vencido',
    id,
    isArchived: false,
    isRead: false,
    message: 'Revise a cobranca em aberto.',
    occurredAt: now,
    organizationId,
    payload: { amount: '2800' },
    readState: label('unread', 'Nao lida', 'warning'),
    recipientUserId: 'admin-paulo',
    subjectDisplayName: title,
    subjectEntityId: 'payment-june',
    subjectEntityType: 'payment',
    title,
  }
}

function audit(
  organizationId: string,
  id: string,
  action: string,
  actionLabel: string,
  targetDisplayName: string,
  targetEntityType: string,
  targetEntityId: string,
): MockEntity {
  return {
    action,
    actionLabel,
    actorDisplayName: 'Paulo Daniel Carneiro',
    actorKind: 'user',
    actorUserId: 'admin-paulo',
    category: label('Mutation', 'Mutacao', 'info'),
    changedFields: { status: 'Disponivel' },
    context: { ip: '127.0.0.1', userAgent: 'Playwright' },
    correlationId: 'trace-a',
    id,
    occurredAt: now,
    organizationId,
    targetDisplayName,
    targetEntityId,
    targetEntityType,
  }
}

function checklistItem(
  id: string,
  areaName: string,
  itemName: string,
  rating: string,
  isComplete: boolean,
) {
  return {
    areaName,
    conditionRating: label(
      rating,
      rating === 'good' ? 'Bom' : 'Atenção',
      rating === 'good' ? 'success' : 'warning',
    ),
    createdAt: now,
    id,
    isComplete,
    isRequired: true,
    itemName,
    observations: 'Sem ressalvas',
    sortOrder: 1,
    updatedAt: now,
  }
}

function relationshipSummaries(entityId: string) {
  return [
    {
      count: 1,
      label: 'Contratos vinculados',
      module: 'contracts',
      route: `/contratos?entityId=${entityId}`,
    },
    {
      count: 1,
      label: 'Abrir documentos vinculados',
      module: 'documents',
      route: `/documentos?entityId=${entityId}`,
    },
    {
      count: 1,
      label: 'Abrir timeline',
      module: 'timeline',
      route: `/timeline?entityId=${entityId}`,
    },
    {
      count: 1,
      label: 'Abrir auditoria',
      module: 'audit',
      route: `/auditoria?entityId=${entityId}`,
    },
  ]
}

async function handleApiRoute(route: Route, state: MockApiState) {
  const request = route.request()
  const url = new URL(request.url())
  const method = request.method() as HttpMethod
  const path = url.pathname

  try {
    const response = await routeRequest(method, path, route, state, url)

    if (response === undefined) {
      await notFound(route, method, path)
      return
    }

    await fulfill(route, response.body, response.status)
  } catch (error) {
    await fulfill(
      route,
      {
        detail: error instanceof Error ? error.message : String(error),
        status: 500,
        title: 'Mock API failure',
      },
      500,
    )
  }
}

async function routeRequest(
  method: HttpMethod,
  path: string,
  route: Route,
  state: MockApiState,
  url: URL,
) {
  if (method === 'POST' && path === '/v1/auth/admin/login') {
    const body = await requestJson(route)
    const permissionCodes = Array.isArray(body.permissionCodes)
      ? body.permissionCodes.filter(
          (permissionCode): permissionCode is string => typeof permissionCode === 'string',
        )
      : undefined

    if (permissionCodes) {
      state.adminPermissions = permissionCodes
      for (const organization of state.organizations) {
        organization.permissionCodes = permissionCodes
      }
    }

    state.activeAccount = 'admin'
    state.activeOrganizationId = getBodyString(body, 'organizationId') || 'org-alsappan'
    return ok(session(state, 'admin'))
  }

  if (method === 'POST' && path === '/v1/auth/resident/login') {
    const body = await requestJson(route)
    state.activeAccount = 'resident'
    state.activeOrganizationId = 'org-alsappan'
    state.activeResidentId = getBodyString(body, 'residentId') || 'resident-ana'
    return ok(session(state, 'resident'))
  }

  if (method === 'POST' && path === '/v1/auth/refresh') {
    return ok(session(state, state.activeAccount === 'resident' ? 'resident' : 'admin'))
  }

  if (method === 'POST' && path === '/v1/auth/logout') {
    state.activeAccount = 'anonymous'
    return ok({})
  }

  if (method === 'GET' && path === '/v1/auth/me') {
    return ok(currentUser(state, state.activeAccount === 'resident' ? 'resident' : 'admin'))
  }

  if (method === 'POST' && path === '/v1/auth/switch-organization') {
    const body = await requestJson(route)
    state.activeOrganizationId = getBodyString(body, 'organizationId') || 'org-alsappan'
    return ok(session(state, 'admin'))
  }

  if (method === 'GET' && path === '/v1/dashboard') {
    return ok(dashboard(state))
  }

  if (method === 'GET' && path === '/v1/search') {
    return ok(search(state, url.searchParams.get('query') ?? ''))
  }

  if (method === 'GET' && path === '/v1/search/contract') {
    return ok({
      entityTypes: [
        { entityType: 'property', entityTypeLabel: 'Imoveis', readPermission: 'properties.read' },
        { entityType: 'payment', entityTypeLabel: 'Pagamentos', readPermission: 'payments.read' },
      ],
      matchedFields: [option('name', 'Nome'), option('address', 'Endereco')],
    })
  }

  const optionResponse = optionsFor(path)
  if (method === 'GET' && optionResponse) {
    return ok(optionResponse)
  }

  if (path.startsWith('/v1/properties')) {
    return entityRoutes(
      method,
      path,
      route,
      state,
      'properties',
      createPropertyFromRequest,
      updatePropertyFromRequest,
    )
  }

  if (path.startsWith('/v1/residents')) {
    return entityRoutes(
      method,
      path,
      route,
      state,
      'residents',
      createResidentFromRequest,
      updateResidentFromRequest,
    )
  }

  if (path.startsWith('/v1/contracts')) {
    return contractRoutes(method, path, route, state)
  }

  if (path.startsWith('/v1/documents')) {
    return documentRoutes(method, path, state)
  }

  if (path.startsWith('/v1/payments')) {
    return paymentRoutes(method, path, route, state)
  }

  if (path.startsWith('/v1/occurrences')) {
    return occurrenceRoutes(method, path, route, state)
  }

  if (path.startsWith('/v1/inspections')) {
    return inspectionRoutes(method, path, route, state)
  }

  if (path.startsWith('/v1/notifications')) {
    return notificationRoutes(method, path, state)
  }

  if (path.startsWith('/v1/audit')) {
    return auditRoutes(method, path, state)
  }

  if (path.startsWith('/v1/resident-portal')) {
    return residentPortalRoutes(method, path, route, state)
  }

  if (path.startsWith('/v1/settings')) {
    return settingsRoutes(method, path, route, state)
  }

  if (path.startsWith('/v1/administrators')) {
    return administratorRoutes(method, path, state)
  }

  return undefined
}

function entityRoutes(
  method: HttpMethod,
  path: string,
  route: Route,
  state: MockApiState,
  collectionKey: 'properties' | 'residents',
  createItem: (state: MockApiState, body: JsonRecord) => MockEntity,
  updateItem: (item: MockEntity, body: JsonRecord) => MockEntity,
) {
  const collection = state[collectionKey]
  const id = path.split('/')[3]

  if (method === 'GET' && path === `/v1/${collectionKey}`) {
    return ok(paged(tenantItems(collection, state)))
  }

  if (method === 'POST' && path === `/v1/${collectionKey}`) {
    return requestJson(route).then((body) => {
      const item = createItem(state, body)
      collection.push(item)
      return ok(item, 201)
    })
  }

  if (method === 'GET' && id) {
    return ok(findById(collection, state, id))
  }

  if (method === 'PUT' && id) {
    return requestJson(route).then((body) => {
      const index = collection.findIndex((item) => item.id === id)
      collection[index] = updateItem(collection[index], body)
      return ok(collection[index])
    })
  }

  if (method === 'DELETE' && id) {
    const item = findById(collection, state, id)
    item.isArchived = true
    return ok({}, 204)
  }

  if (method === 'POST' && path.endsWith('/restore')) {
    const item = findById(collection, state, id)
    item.isArchived = false
    return ok(item)
  }

  if (collectionKey === 'properties' && method === 'POST' && path.endsWith('/status')) {
    return requestJson(route).then((body) => {
      const item = findById(state.properties, state, id)
      item.status = propertyStatus(getBodyString(body, 'status') || 'available')
      return ok(item)
    })
  }

  return undefined
}

async function contractRoutes(method: HttpMethod, path: string, route: Route, state: MockApiState) {
  const id = path.split('/')[3]

  if (method === 'GET' && path === '/v1/contracts') {
    return ok(paged(tenantItems(state.contracts, state)))
  }

  if (method === 'POST' && path === '/v1/contracts') {
    const item = createContractFromRequest(state, await requestJson(route))
    state.contracts.push(item)
    return ok(item, 201)
  }

  if (method === 'GET' && id) {
    return ok(findById(state.contracts, state, id))
  }

  if (method === 'PUT' && id) {
    const existing = findById(state.contracts, state, id)
    Object.assign(existing, await requestJson(route), { updatedAt: now })
    return ok(existing)
  }

  if (method === 'DELETE' && id) {
    findById(state.contracts, state, id).isArchived = true
    return ok({}, 204)
  }

  if (method === 'POST' && path.endsWith('/activate')) {
    findById(state.contracts, state, id).status = label('active', 'Ativo', 'success')
    return ok(findById(state.contracts, state, id))
  }

  if (method === 'POST' && path.endsWith('/restore')) {
    findById(state.contracts, state, id).isArchived = false
    return ok(findById(state.contracts, state, id))
  }

  if (method === 'POST' && (path.endsWith('/terminate') || path.endsWith('/cancel'))) {
    const item = findById(state.contracts, state, id)
    item.status = path.endsWith('/terminate')
      ? label('terminated', 'Rescindido', 'danger')
      : label('cancelled', 'Cancelado', 'danger')
    return ok(item)
  }

  return undefined
}

function documentRoutes(method: HttpMethod, path: string, state: MockApiState) {
  const id = path.split('/')[3]

  if (method === 'GET' && path === '/v1/documents') {
    return ok(paged(tenantItems(state.documents, state)))
  }

  if (method === 'POST' && path === '/v1/documents') {
    const item = document(
      state.activeOrganizationId,
      `document-${state.documents.length + 1}`,
      'Contrato E2E assinado',
      'contract',
      [
        {
          entityId: 'contract-calabria',
          entityType: 'contract',
          label: 'Contrato E2E',
          route: '/contratos?contractId=contract-calabria',
        },
      ],
    )
    state.documents.push(item)
    return ok(item, 201)
  }

  if (method === 'GET' && path.endsWith('/download')) {
    const item = state.documents.find((documentItem) => documentItem.id === id)

    if (!item) {
      return undefined
    }

    if (item.organizationId !== state.activeOrganizationId) {
      return forbidden('Documento pertence a outra organizacao.')
    }

    return { body: 'mock document', status: 200 }
  }

  if (method === 'GET' && id) {
    return ok(findById(state.documents, state, id))
  }

  if (method === 'POST' && path.endsWith('/versions')) {
    return ok(findById(state.documents, state, id))
  }

  if (method === 'DELETE' && id) {
    findById(state.documents, state, id).isArchived = true
    return ok({}, 204)
  }

  if (method === 'POST' && path.endsWith('/restore')) {
    findById(state.documents, state, id).isArchived = false
    return ok(findById(state.documents, state, id))
  }

  return undefined
}

async function paymentRoutes(method: HttpMethod, path: string, route: Route, state: MockApiState) {
  const id = path.split('/')[3]

  if (method === 'GET' && path === '/v1/payments') {
    return ok(paged(tenantItems(state.payments, state)))
  }

  if (method === 'POST' && path === '/v1/payments') {
    const item = createPaymentFromRequest(state, await requestJson(route))
    state.payments.push(item)
    return ok(item, 201)
  }

  if (method === 'GET' && id) {
    return ok(findById(state.payments, state, id))
  }

  if (method === 'PUT' && id) {
    Object.assign(findById(state.payments, state, id), await requestJson(route), { updatedAt: now })
    return ok(findById(state.payments, state, id))
  }

  if (method === 'DELETE' && id) {
    findById(state.payments, state, id).isArchived = true
    return ok({}, 204)
  }

  if (method === 'POST' && path.endsWith('/restore')) {
    findById(state.payments, state, id).isArchived = false
    return ok(findById(state.payments, state, id))
  }

  if (method === 'POST' && path.endsWith('/transactions')) {
    const item = findById(state.payments, state, id)
    const body = await requestJson(route)
    const amountValue = getMoneyAmount(body.amount) || Number(body.amount) || 0
    item.transactions = [
      ...(Array.isArray(item.transactions) ? item.transactions : []),
      {
        amount: money(amountValue),
        bankReference: getBodyString(body, 'bankReference') || 'E2E-BANK',
        createdAt: now,
        id: `transaction-${Date.now()}`,
        isReversed: false,
        method: getBodyString(body, 'method') || 'pix',
        methodLabel: paymentMethodLabel(getBodyString(body, 'method') || 'pix'),
        notes: getBodyString(body, 'notes'),
        providerCode: getBodyString(body, 'providerCode') || 'mock-pix',
        providerReference: getBodyString(body, 'providerReference') || 'PIX-1',
        receiptDocumentId: getBodyString(body, 'receiptDocumentId') || 'document-contract',
        settledOn: getBodyString(body, 'settledOn') || '2026-06-27',
      },
    ]
    item.settledAmount = money(amountValue)
    item.balance = money(Math.max(0, getMoneyAmount(item.amount) - amountValue))
    item.status =
      item.balance.amount === 0
        ? label('paid', 'Pago', 'success')
        : label('partially-paid', 'Parcial', 'warning')
    item.receiptDocuments = [
      {
        documentId: 'document-contract',
        label: 'Comprovante',
        route: '/documentos/document-contract',
      },
    ]
    return ok(item)
  }

  if (method === 'POST' && path.endsWith('/instructions')) {
    const body = await requestJson(route)
    return ok(
      paymentInstruction(
        findById(state.payments, state, id),
        getBodyString(body, 'providerCode') || 'mock-pix',
      ),
    )
  }

  return undefined
}

async function occurrenceRoutes(
  method: HttpMethod,
  path: string,
  route: Route,
  state: MockApiState,
) {
  const id = path.split('/')[3]

  if (method === 'GET' && path === '/v1/occurrences') {
    return ok(paged(tenantItems(state.occurrences, state)))
  }

  if (method === 'POST' && path === '/v1/occurrences') {
    const item = createOccurrenceFromRequest(state, await requestJson(route))
    state.occurrences.push(item)
    return ok(item, 201)
  }

  if (method === 'GET' && id) {
    return ok(findById(state.occurrences, state, id))
  }

  if (method === 'PUT' && id) {
    Object.assign(findById(state.occurrences, state, id), await requestJson(route), {
      updatedAt: now,
    })
    return ok(findById(state.occurrences, state, id))
  }

  if (method === 'DELETE' && id) {
    findById(state.occurrences, state, id).isArchived = true
    return ok({}, 204)
  }

  const item = id ? findById(state.occurrences, state, id) : undefined
  if (!item) {
    return undefined
  }

  if (method === 'POST' && path.endsWith('/assign')) {
    item.assignedUser = {
      displayName: 'Paulo Daniel Carneiro',
      email: 'paulo.carneiro.dev@protonmail.com',
      id: 'admin-paulo',
    }
    item.status = label('assigned', 'Atribuida', 'warning')
    return ok(item)
  }

  if (method === 'POST' && path.endsWith('/priority')) {
    item.priority = label('urgent', 'Urgente', 'danger')
    return ok(item)
  }

  if (method === 'POST' && path.endsWith('/status')) {
    item.status = label('in-progress', 'Em andamento', 'info')
    return ok(item)
  }

  if (method === 'POST' && path.endsWith('/resolve')) {
    const body = await requestJson(route)
    item.status = label('resolved', 'Resolvida', 'success')
    item.isUnresolved = false
    item.resolutionNotes = getBodyString(body, 'resolutionNotes') || 'Conserto realizado.'
    item.resolvedAt = now
    return ok(item)
  }

  if (method === 'POST' && path.endsWith('/comments')) {
    const body = await requestJson(route)
    item.comments = [
      ...(Array.isArray(item.comments) ? item.comments : []),
      {
        authorDisplayName: 'Paulo Daniel Carneiro',
        authorUserId: 'admin-paulo',
        body: getBodyString(body, 'body') || 'Equipe avisada.',
        createdAt: now,
        id: `comment-${Date.now()}`,
        isInternal: Boolean(body.isInternal),
      },
    ]
    return ok(item)
  }

  if (method === 'POST' && path.endsWith('/attachments')) {
    item.attachments = [
      ...(Array.isArray(item.attachments) ? item.attachments : []),
      {
        createdAt: now,
        documentId: 'document-contract',
        label: 'Contrato assinado',
        route: '/documentos/document-contract',
      },
    ]
    return ok(item)
  }

  if (method === 'POST' && path.endsWith('/restore')) {
    item.isArchived = false
    return ok(item)
  }

  return undefined
}

async function inspectionRoutes(
  method: HttpMethod,
  path: string,
  route: Route,
  state: MockApiState,
) {
  const id = path.split('/')[3]

  if (method === 'GET' && path === '/v1/inspections') {
    return ok(paged(tenantItems(state.inspections, state)))
  }

  if (method === 'POST' && path === '/v1/inspections') {
    const item = createInspectionFromRequest(state, await requestJson(route))
    state.inspections.push(item)
    return ok(item, 201)
  }

  if (method === 'GET' && id) {
    return ok(findById(state.inspections, state, id))
  }

  if (method === 'PUT' && id) {
    Object.assign(findById(state.inspections, state, id), await requestJson(route), {
      updatedAt: now,
    })
    return ok(findById(state.inspections, state, id))
  }

  if (method === 'DELETE' && id) {
    findById(state.inspections, state, id).isArchived = true
    return ok({}, 204)
  }

  const item = id ? findById(state.inspections, state, id) : undefined
  if (!item) {
    return undefined
  }

  if (method === 'POST' && path.endsWith('/start')) {
    item.status = label('in-progress', 'Em andamento', 'info')
    item.startedAt = now
    return ok(item)
  }

  if (method === 'POST' && path.endsWith('/complete')) {
    const body = await requestJson(route)
    item.status = label('completed', 'Concluida', 'success')
    item.completedAt = now
    item.completionNotes = getBodyString(body, 'notes') || 'Relatorio concluido.'
    item.isPending = false
    return ok(item)
  }

  if (method === 'POST' && path.endsWith('/checklist-items')) {
    const body = await requestJson(route)
    item.checklistItems = [
      ...(Array.isArray(item.checklistItems) ? item.checklistItems : []),
      checklistItem(
        `inspection-item-${Date.now()}`,
        getBodyString(body, 'areaName') || 'Quarto',
        getBodyString(body, 'itemName') || 'Janela',
        getBodyString(body, 'conditionRating') || 'good',
        true,
      ),
    ]
    return ok(item)
  }

  if (method === 'POST' && path.endsWith('/document-links')) {
    item.linkedDocuments = [
      ...(Array.isArray(item.linkedDocuments) ? item.linkedDocuments : []),
      {
        documentId: 'document-contract',
        kind: label('report', 'Laudo', 'info'),
        label: 'Laudo final',
        route: '/documentos/document-contract',
      },
    ]
    return ok(item)
  }

  if (method === 'POST' && path.endsWith('/restore')) {
    item.isArchived = false
    return ok(item)
  }

  return undefined
}

function notificationRoutes(method: HttpMethod, path: string, state: MockApiState) {
  const id = path.split('/')[3]

  if (method === 'GET' && path === '/v1/notifications') {
    return ok(paged(tenantItems(state.notifications, state)))
  }

  if (method === 'GET' && path === '/v1/notifications/unread-count') {
    return ok({
      count: tenantItems(state.notifications, state).filter((item) => !item.isRead).length,
    })
  }

  if (method === 'POST' && path === '/v1/notifications/read-all') {
    let updatedCount = 0
    for (const item of tenantItems(state.notifications, state)) {
      if (!item.isRead) {
        item.isRead = true
        item.readState = label('read', 'Lida', 'success')
        item.readAt = now
        updatedCount += 1
      }
    }
    return ok({ updatedCount })
  }

  if (method === 'POST' && path.endsWith('/read') && id) {
    const item = findById(state.notifications, state, id)
    item.isRead = true
    item.readState = label('read', 'Lida', 'success')
    item.readAt = now
    return ok(item)
  }

  if (method === 'DELETE' && id) {
    findById(state.notifications, state, id).isArchived = true
    return ok({}, 204)
  }

  if (method === 'GET' && path === '/v1/notifications/preferences') {
    return ok({
      isPersisted: true,
      notice: 'Preferencias carregadas.',
      preferences: [
        {
          category: 'payments',
          categoryLabel: 'Pagamentos',
          channel: 'in-app',
          channelLabel: 'Aplicativo',
          isEnabled: true,
          isMandatory: false,
        },
      ],
    })
  }

  if (method === 'PUT' && path === '/v1/notifications/preferences') {
    return ok({
      isPersisted: true,
      notice: 'Salvo.',
      preferences: [
        {
          category: 'payments',
          categoryLabel: 'Pagamentos',
          channel: 'in-app',
          channelLabel: 'Aplicativo',
          isEnabled: true,
          isMandatory: false,
        },
      ],
    })
  }

  return undefined
}

function auditRoutes(method: HttpMethod, path: string, state: MockApiState) {
  const id = path.split('/')[3]

  if (method === 'GET' && path === '/v1/audit') {
    return ok(paged(tenantItems(state.auditEntries, state)))
  }

  if (method === 'GET' && id) {
    return ok(findById(state.auditEntries, state, id))
  }

  return undefined
}

async function residentPortalRoutes(
  method: HttpMethod,
  path: string,
  route: Route,
  state: MockApiState,
) {
  if (method === 'GET' && path === '/v1/resident-portal/summary') {
    return ok(residentPortalSummary(state))
  }

  if (method === 'GET' && /^\/v1\/resident-portal\/residents\/[^/]+\/summary$/.test(path)) {
    const residentId = decodeURIComponent(path.split('/')[4] ?? '')

    if (residentId !== state.activeResidentId) {
      return forbidden('Acesso negado para outro morador.')
    }

    return ok(residentPortalSummary(state, residentId))
  }

  if (method === 'GET' && path === '/v1/resident-portal/profile') {
    return ok(residentPortalSummary(state).profile)
  }

  if (method === 'GET' && path === '/v1/resident-portal/property') {
    return ok(residentPortalSummary(state).linkedProperty)
  }

  if (method === 'GET' && path === '/v1/resident-portal/contracts') {
    return ok(residentPortalSummary(state).contracts)
  }

  if (method === 'GET' && path === '/v1/resident-portal/payments') {
    return ok(residentPortalSummary(state).payments)
  }

  if (method === 'GET' && path === '/v1/resident-portal/documents') {
    return ok(residentPortalSummary(state).documents)
  }

  if (method === 'GET' && path === '/v1/resident-portal/occurrences') {
    return ok(residentPortalSummary(state).occurrences)
  }

  if (method === 'GET' && path === '/v1/resident-portal/inspections') {
    return ok(residentPortalSummary(state).inspections)
  }

  if (method === 'GET' && path === '/v1/resident-portal/notifications') {
    return ok(residentPortalSummary(state).notifications)
  }

  if (method === 'POST' && path === '/v1/resident-portal/occurrences') {
    const item = createOccurrenceFromRequest(state, await requestJson(route))
    state.occurrences.push(item)
    return ok(toPortalOccurrence(item), 201)
  }

  if (method === 'POST' && path === '/v1/resident-portal/documents') {
    const item = document(
      state.activeOrganizationId,
      `resident-document-${state.documents.length + 1}`,
      'Documento enviado pelo morador',
      'resident',
      [],
    )
    state.documents.push(item)
    return ok(toPortalDocument(item), 201)
  }

  if (method === 'POST' && /\/v1\/resident-portal\/payments\/[^/]+\/instructions$/.test(path)) {
    const id = path.split('/')[4]
    const body = await requestJson(route)
    return ok(
      paymentInstruction(
        findById(state.payments, state, id),
        getBodyString(body, 'providerCode') || 'mock-pix',
      ),
    )
  }

  return undefined
}

async function settingsRoutes(method: HttpMethod, path: string, route: Route, state: MockApiState) {
  if (method === 'GET' && path === '/v1/settings') {
    return ok(state.settings)
  }

  if (method === 'PUT' && path === '/v1/settings/branding') {
    const body = await requestJson(route)

    if (
      String(body.primaryColor ?? '').toLowerCase() === '#ffffff' &&
      String(body.primaryForegroundColor ?? '').toLowerCase() === '#ffffff'
    ) {
      return ok(
        {
          errors: { primaryForegroundColor: ['Contraste insuficiente para a cor primaria.'] },
          status: 400,
          title: 'Validacao falhou',
          type: 'https://httpstatuses.com/400',
        },
        400,
      )
    }

    state.settings.branding = {
      ...state.settings.branding,
      ...body,
      updatedAt: now,
    }
    syncActiveOrganizationBranding(state)
    return ok(state.settings.branding)
  }

  if (method === 'POST' && path === '/v1/settings/branding/reset') {
    state.settings.branding = createSettingsDashboard().branding
    syncActiveOrganizationBranding(state)
    return ok(state.settings.branding)
  }

  if (method === 'POST' && path === '/v1/settings/branding/logo') {
    state.settings.branding.logo = {
      contentType: 'image/png',
      fileName: 'logo.png',
      height: 128,
      sizeBytes: 1024,
      storageKey: 'logos/logo.png',
      width: 128,
    }
    syncActiveOrganizationBranding(state)
    return ok(state.settings.branding)
  }

  if (method === 'DELETE' && path === '/v1/settings/branding/logo') {
    delete state.settings.branding.logo
    syncActiveOrganizationBranding(state)
    return ok(state.settings.branding)
  }

  return ok(state.settings)
}

function administratorRoutes(method: HttpMethod, path: string, state: MockApiState) {
  if (method === 'GET' && path === '/v1/administrators') {
    return ok(paged(tenantItems(state.administrators, state)))
  }

  if (method === 'GET' && path === '/v1/administrators/role-options') {
    return ok([option('Administrador', 'Administrador'), option('Operador', 'Operador')])
  }

  return undefined
}

function optionsFor(path: string) {
  const optionMap: Record<string, unknown[]> = {
    '/v1/audit/category-options': [
      label('Mutation', 'Mutacao', 'info'),
      label('Security', 'Seguranca', 'warning'),
    ],
    '/v1/contracts/adjustment-index-options': [
      option('ipca', 'IPCA'),
      option('igpm', 'IGP-M'),
      option('fixed', 'Fixo'),
    ],
    '/v1/contracts/status-options': [
      label('draft', 'Rascunho'),
      label('active', 'Ativo', 'success'),
      label('archived', 'Arquivado', 'archived'),
    ],
    '/v1/documents/allowed-file-types': [
      option('application/pdf', 'PDF'),
      option('image/png', 'PNG'),
    ],
    '/v1/documents/category-options': [
      option('contract', 'Contrato'),
      option('payment-receipt', 'Comprovante'),
      option('resident', 'Morador'),
      option('other', 'Outro'),
    ],
    '/v1/documents/status-options': [
      label('active', 'Ativo', 'success'),
      label('archived', 'Arquivado', 'archived'),
    ],
    '/v1/inspections/condition-rating-options': [
      option('pending', 'Pendente'),
      option('good', 'Bom'),
      option('attention', 'Atencao'),
      option('damaged', 'Danificado'),
    ],
    '/v1/inspections/document-kind-options': [
      option('photo', 'Foto'),
      option('attachment', 'Anexo'),
      option('report', 'Laudo'),
    ],
    '/v1/inspections/status-options': [
      option('scheduled', 'Agendada'),
      option('in-progress', 'Em andamento'),
      option('completed', 'Concluida'),
    ],
    '/v1/inspections/type-options': [
      option('move-in', 'Entrada'),
      option('move-out', 'Saida'),
      option('periodic', 'Periodica'),
    ],
    '/v1/notifications/category-options': [
      option('payments', 'Pagamentos'),
      option('contracts', 'Contratos'),
    ],
    '/v1/notifications/channel-options': [
      option('in-app', 'Aplicativo'),
      option('email', 'E-mail'),
    ],
    '/v1/occurrences/priority-options': [
      option('low', 'Baixa'),
      option('medium', 'Media'),
      option('high', 'Alta'),
      option('urgent', 'Urgente'),
    ],
    '/v1/occurrences/status-options': [
      option('open', 'Aberta'),
      option('assigned', 'Atribuida'),
      option('in-progress', 'Em andamento'),
      option('resolved', 'Resolvida'),
    ],
    '/v1/occurrences/type-options': [
      option('maintenance', 'Manutencao'),
      option('complaint', 'Reclamacao'),
      option('request', 'Solicitacao'),
    ],
    '/v1/payments/method-options': [
      option('pix', 'Pix'),
      option('boleto', 'Boleto'),
      option('paypal', 'PayPal'),
      option('bank-transfer', 'Transferencia'),
    ],
    '/v1/payments/provider-options': [
      option('mock-pix', 'Pix mock'),
      option('mock-boleto', 'Boleto mock'),
      option('mock-paypal', 'PayPal mock'),
    ],
    '/v1/payments/reconciliation-status-options': [
      label('not-required', 'Nao requerida'),
      label('pending', 'Pendente'),
      label('matched', 'Conciliada'),
    ],
    '/v1/payments/status-options': [
      label('pending', 'Pendente'),
      label('paid', 'Pago', 'success'),
      label('overdue', 'Vencido', 'danger'),
    ],
    '/v1/residents/portal-status-options': [
      label('active', 'Ativo', 'success'),
      label('invited', 'Convidado'),
      label('disabled', 'Desativado'),
    ],
    '/v1/residents/status-options': [
      label('active', 'Ativo', 'success'),
      label('inactive', 'Inativo'),
      label('archived', 'Arquivado', 'archived'),
    ],
  }

  if (path === '/v1/residents/duplicate-warnings') {
    return []
  }

  return optionMap[path]
}

function createPropertyFromRequest(state: MockApiState, body: JsonRecord) {
  return {
    ...property(
      state.activeOrganizationId,
      `property-${state.properties.length + 1}`,
      getBodyString(body, 'name') || 'Apartamento Jardim E2E',
      getBodyString(body.status as JsonRecord, 'code') ||
        getBodyString(body, 'status') ||
        'available',
      getMoneyAmount(body.suggestedRent) || 2500,
      Number(body.garageSpaceCount ?? 0),
    ),
    address:
      body.address ?? property(state.activeOrganizationId, 'tmp', 'tmp', 'available', 0, 0).address,
    suggestedRent: body.suggestedRent ?? money(2500),
  }
}

function updatePropertyFromRequest(item: MockEntity, body: JsonRecord) {
  return { ...item, ...body, updatedAt: now }
}

function createResidentFromRequest(state: MockApiState, body: JsonRecord) {
  return {
    ...resident(
      state.activeOrganizationId,
      `resident-${state.residents.length + 1}`,
      getBodyString(body, 'fullName') || 'Maria E2E',
      getBodyString(body, 'email') || 'maria.e2e@example.com',
    ),
    ...body,
    portalStatus: label(getBodyString(body, 'portalStatus') || 'active', 'Ativo', 'success'),
    status: label(getBodyString(body, 'status') || 'active', 'Ativo', 'success'),
  }
}

function updateResidentFromRequest(item: MockEntity, body: JsonRecord) {
  return { ...item, ...body, updatedAt: now }
}

function createContractFromRequest(state: MockApiState, body: JsonRecord) {
  const propertyItem = findById(
    state.properties,
    state,
    getBodyString(body, 'propertyId') || 'prop-calabria',
  )
  const residentItem = findById(
    state.residents,
    state,
    getBodyString(body, 'primaryResidentId') || 'resident-joao',
  )
  return {
    ...contract(
      state.activeOrganizationId,
      `contract-${state.contracts.length + 1}`,
      propertyItem,
      residentItem,
    ),
    dueDay: Number(body.dueDay ?? 10),
    endDate: getBodyString(body, 'endDate'),
    monthlyRent: body.monthlyRent ?? money(2500),
    startDate: getBodyString(body, 'startDate') || '2026-07-01',
    status:
      getBodyString(body, 'lifecycleAction') === 'activate'
        ? label('active', 'Ativo', 'success')
        : label('draft', 'Rascunho', 'neutral'),
  }
}

function createPaymentFromRequest(state: MockApiState, body: JsonRecord) {
  const propertyItem = findById(
    state.properties,
    state,
    getBodyString(body, 'propertyId') || 'prop-calabria',
  )
  const residentItem = findById(
    state.residents,
    state,
    getBodyString(body, 'residentId') || 'resident-joao',
  )
  const contractItem = findById(
    state.contracts,
    state,
    getBodyString(body, 'contractId') || 'contract-calabria',
  )
  return {
    ...payment(
      state.activeOrganizationId,
      `payment-${state.payments.length + 1}`,
      getBodyString(body, 'title') || 'Pagamento E2E',
      propertyItem,
      residentItem,
      contractItem,
    ),
    amount: body.amount ?? money(1000),
    balance: body.amount ?? money(1000),
    dueDate: getBodyString(body, 'dueDate') || '2026-07-10',
    preferredMethod: getBodyString(body, 'preferredMethod') || 'pix',
    preferredMethodLabel: paymentMethodLabel(getBodyString(body, 'preferredMethod') || 'pix'),
  }
}

function createOccurrenceFromRequest(state: MockApiState, body: JsonRecord) {
  const defaultResidentId =
    state.activeAccount === 'resident' ? state.activeResidentId : 'resident-joao'
  const propertyItem = findById(
    state.properties,
    state,
    getBodyString(body, 'propertyId') || 'prop-calabria',
  )
  const residentItem = findById(
    state.residents,
    state,
    getBodyString(body, 'residentId') || defaultResidentId,
  )
  const contractItem = findById(
    state.contracts,
    state,
    getBodyString(body, 'contractId') || 'contract-calabria',
  )
  return {
    ...occurrence(
      state.activeOrganizationId,
      `occurrence-${state.occurrences.length + 1}`,
      getBodyString(body, 'title') || 'Ocorrencia E2E',
      propertyItem,
      residentItem,
      contractItem,
    ),
    description: getBodyString(body, 'description') || 'Criada pelo portal.',
    priority: label(getBodyString(body, 'priority') || 'medium', 'Media', 'warning'),
    status: label('open', 'Aberta', 'info'),
    type: label(getBodyString(body, 'type') || 'request', 'Solicitacao', 'info'),
  }
}

function createInspectionFromRequest(state: MockApiState, body: JsonRecord) {
  const propertyItem = findById(
    state.properties,
    state,
    getBodyString(body, 'propertyId') || 'prop-calabria',
  )
  const residentItem = findById(
    state.residents,
    state,
    getBodyString(body, 'residentId') || 'resident-joao',
  )
  const contractItem = findById(
    state.contracts,
    state,
    getBodyString(body, 'contractId') || 'contract-calabria',
  )
  return {
    ...inspection(
      state.activeOrganizationId,
      `inspection-${state.inspections.length + 1}`,
      getBodyString(body, 'title') || 'Vistoria E2E',
      propertyItem,
      residentItem,
      contractItem,
    ),
    scheduledAt: getBodyString(body, 'scheduledAt') || '2026-07-01T09:00:00.000Z',
    type: label(getBodyString(body, 'type') || 'periodic', 'Periodica', 'info'),
  }
}

function session(state: MockApiState, accountType: 'admin' | 'resident') {
  return {
    accessToken: `${accountType}-token`,
    expiresAt: '2026-06-28T10:00:00.000Z',
    refreshToken: `${accountType}-refresh-token`,
    tokenType: 'Bearer',
    user: currentUser(state, accountType),
  }
}

function currentUser(state: MockApiState, accountType: 'admin' | 'resident') {
  return {
    accountType,
    activeOrganizationId: state.activeOrganizationId,
    displayName: accountType === 'resident' ? 'Ana Portal' : 'Paulo Daniel Carneiro',
    email:
      accountType === 'resident' ? 'resident@example.com' : 'paulo.carneiro.dev@protonmail.com',
    id: accountType === 'resident' ? 'resident-user' : 'admin-paulo',
    organizations: state.organizations,
    permissions: accountType === 'resident' ? ['resident-portal.read'] : state.adminPermissions,
  }
}

function syncActiveOrganizationBranding(state: MockApiState) {
  const organization = state.organizations.find((item) => item.id === state.activeOrganizationId)

  if (organization) {
    organization.branding = { ...state.settings.branding }
  }
}

function getSummaryId(value: unknown) {
  return value && typeof value === 'object' && 'id' in value
    ? String((value as { id: unknown }).id)
    : undefined
}

function dashboard(state: MockApiState) {
  const properties = tenantItems(state.properties, state)
  const payments = tenantItems(state.payments, state)
  return {
    generatedAt: now,
    metrics: [
      {
        description: 'Imoveis ativos',
        displayValue: String(properties.length),
        isVisible: true,
        key: 'properties',
        label: 'Imoveis',
        metadata: {},
        route: '/imoveis',
        tone: 'info',
        value: properties.length,
      },
      {
        description: 'Pagamentos vencidos',
        displayValue: String(payments.filter((item) => item.isOverdue).length),
        isVisible: true,
        key: 'overdue-payments',
        label: 'Pagamentos vencidos',
        metadata: {},
        route: '/pagamentos?overdueOnly=true',
        tone: 'danger',
        value: payments.filter((item) => item.isOverdue).length,
      },
    ],
    recentActivity: {
      description: 'Atividades recentes',
      isVisible: true,
      items: tenantItems(state.auditEntries, state).map((item) => ({
        entityType: item.targetEntityType,
        entityTypeLabel: 'Imovel',
        eventType: item.action,
        eventTypeLabel: item.actionLabel,
        id: item.id,
        occurredAt: item.occurredAt,
        route: `/auditoria?entityType=${item.targetEntityType}&entityId=${item.targetEntityId}`,
        subjectDisplayName: item.targetDisplayName,
        summary: item.actionLabel,
      })),
      label: 'Atividade recente',
      route: '/timeline',
    },
  }
}

function search(state: MockApiState, query: string) {
  const lowerQuery = query.toLowerCase()
  const propertyResults = tenantItems(state.properties, state)
    .filter((item) => String(item.name).toLowerCase().includes(lowerQuery))
    .map((item) => ({
      entityType: 'property',
      entityTypeLabel: 'Imoveis',
      id: item.id,
      label: item.name,
      matchedField: 'Nome',
      route: `/imoveis?propertyId=${item.id}`,
      summary: 'Rua Calabria, 82',
    }))
  const paymentResults = tenantItems(state.payments, state)
    .filter((item) => String(item.title).toLowerCase().includes(lowerQuery))
    .map((item) => ({
      entityType: 'payment',
      entityTypeLabel: 'Pagamentos',
      id: item.id,
      label: item.title,
      matchedField: 'Titulo',
      route: `/pagamentos?paymentId=${item.id}`,
      summary: 'Vencimento 2026-06-20',
    }))

  return {
    groups: [
      {
        entityType: 'property',
        entityTypeLabel: 'Imoveis',
        results: propertyResults,
        route: '/imoveis',
      },
      {
        entityType: 'payment',
        entityTypeLabel: 'Pagamentos',
        results: paymentResults,
        route: '/pagamentos',
      },
    ].filter((group) => group.results.length > 0),
    query,
    totalItems: propertyResults.length + paymentResults.length,
  }
}

function residentPortalSummary(state: MockApiState, residentId = state.activeResidentId) {
  const propertyItem = findById(state.properties, state, 'prop-calabria')
  const residentItem = findById(state.residents, state, residentId)
  const residentContracts = tenantItems(state.contracts, state).filter((item) =>
    Array.isArray(item.residents)
      ? item.residents.some((residentSummary) => getSummaryId(residentSummary) === residentId)
      : getSummaryId(item.primaryResident) === residentId,
  )
  const residentPayments = tenantItems(state.payments, state).filter(
    (item) => item.resident?.id === residentId,
  )
  const residentOccurrences = tenantItems(state.occurrences, state).filter(
    (item) => item.resident?.id === residentId,
  )
  const residentInspections = tenantItems(state.inspections, state).filter(
    (item) => item.resident?.id === residentId,
  )

  return {
    contracts: residentContracts.map(toPortalContract),
    documents: tenantItems(state.documents, state).map(toPortalDocument),
    inspections: residentInspections.map(toPortalInspection),
    linkedProperty: toPortalProperty(propertyItem),
    notifications: tenantItems(state.notifications, state).map((item) => ({
      category: item.category,
      createdAt: item.createdAt,
      deepLink: item.deepLink,
      id: item.id,
      isRead: item.isRead,
      message: item.message,
      title: item.title,
    })),
    occurrences: residentOccurrences.map(toPortalOccurrence),
    payments: residentPayments.map(toPortalPayment),
    profile: {
      email: residentItem.email,
      fullName: residentItem.fullName,
      phone: residentItem.phone,
      portalStatus: residentItem.portalStatus,
      preferredName: residentItem.preferredName,
      residentId: residentItem.id,
      secondaryPhone: residentItem.secondaryPhone,
      status: residentItem.status,
    },
    settings: {
      allowDocumentUpload: true,
      allowOccurrenceCreation: true,
      allowProfileUpdateRequests: true,
      isEnabled: true,
    },
  }
}

function createSettingsDashboard() {
  return {
    branding: {
      accentColor: '#22c55e',
      accentForegroundColor: '#052e16',
      concurrencyToken: 'branding-token',
      displayName: 'Alsappan',
      logoAlt: 'Alsappan',
      primaryColor: '#1877f2',
      primaryForegroundColor: '#ffffff',
      supportEmail: 'suporte@alsappan.test',
      supportPhone: '+55 11 3333-0000',
      supportUrl: 'https://alsappan.test/suporte',
    },
    catalogs: [
      {
        catalogType: 'property-type',
        items: [
          {
            catalogType: 'property-type',
            code: 'house',
            id: 'catalog-house',
            isEnabled: true,
            isSystem: true,
            labels: { 'pt-BR': 'Casa' },
            sortOrder: 1,
          },
        ],
        label: 'Tipos de imovel',
      },
    ],
    localization: {
      concurrencyToken: 'localization-token',
      defaultLocale: 'pt-BR',
      enabledLocales: ['pt-BR', 'en-US'],
      fallbackLocale: 'pt-BR',
      supportedLocales: [
        {
          code: 'pt-BR',
          isDefault: true,
          isEnabled: true,
          isFallback: true,
          label: 'Portugues (Brasil)',
        },
        {
          code: 'en-US',
          isDefault: false,
          isEnabled: true,
          isFallback: false,
          label: 'English (US)',
        },
      ],
    },
    notifications: {
      availableCategories: [{ code: 'payments', isEnabled: true, label: 'Pagamentos' }],
      availableChannels: [{ code: 'in-app', isEnabled: true, label: 'Aplicativo' }],
      concurrencyToken: 'notification-token',
      emailEnabled: true,
      enabledCategories: ['payments'],
      enabledChannels: ['in-app'],
      inAppEnabled: true,
      whatsAppEnabled: false,
    },
    organization: {
      concurrencyToken: 'organization-token',
      contactEmail: 'contato@alsappan.test',
      contactPhone: '+55 11 3333-0000',
      contactWebsite: 'https://alsappan.test',
      createdAt: now,
      currencyCode: 'BRL',
      displayName: 'Alsappan Demo',
      name: 'Alsappan',
      organizationId: 'org-alsappan',
      slug: 'alsappan',
      timeZone: 'America/Sao_Paulo',
      updatedAt: now,
    },
    profilePreferences: {
      effectiveLocale: 'pt-BR',
      isPersisted: true,
      locale: 'pt-BR',
      notice: 'Meu locale',
      supportedLocales: [
        {
          code: 'pt-BR',
          isDefault: true,
          isEnabled: true,
          isFallback: true,
          label: 'Portugues (Brasil)',
        },
        {
          code: 'en-US',
          isDefault: false,
          isEnabled: true,
          isFallback: false,
          label: 'English (US)',
        },
      ],
    },
    residentPortal: {
      allowDocumentUpload: true,
      allowOccurrenceCreation: true,
      allowProfileUpdateRequests: true,
      concurrencyToken: 'resident-portal-token',
      isEnabled: true,
    },
    security: {
      concurrencyToken: 'security-token',
      mfaPolicy: 'disabled',
      passwordRules: {
        minimumLength: 8,
        requireDigit: true,
        requireLowercase: true,
        requireSymbol: false,
        requireUppercase: true,
      },
      sessionTimeoutMinutes: 60,
      supportedMfaPolicies: ['disabled', 'optional', 'required'],
    },
    tenantBehavior: {
      allowOrganizationSwitching: true,
      concurrencyToken: 'tenant-token',
      requireActiveOrganization: true,
      strictTenantIsolation: true,
    },
  }
}

function paymentInstruction(paymentItem: MockEntity, providerCode: string) {
  const kind = providerCode.includes('boleto')
    ? 'boleto'
    : providerCode.includes('paypal')
      ? 'paypal'
      : 'pix'
  return {
    amount: paymentItem.amount,
    approvalUrl:
      kind === 'paypal' ? 'https://paypal.test/checkout/mock-paypal-reference' : undefined,
    barcode: kind === 'boleto' ? '23793381286000000000630000000000000000123456' : undefined,
    chargeId: `${providerCode}-charge`,
    copyPasteCode: kind === 'pix' ? 'pix-copy-paste-e2e' : undefined,
    dueDate: String(paymentItem.dueDate),
    expiresAt: '2026-06-30T23:59:59.000Z',
    kind,
    linhaDigitavel:
      kind === 'boleto' ? '23793.38128 60000.000006 30000.000000 1 12345678901234' : undefined,
    metadata: { mock: 'true' },
    payerSummary: String(paymentItem.resident?.name ?? 'Joao da Silva'),
    paymentIntentId: kind === 'paypal' ? 'paypal-intent-e2e' : undefined,
    providerCode,
    providerReference: `${providerCode}-reference`,
    qrPayload: kind === 'pix' ? 'pix://mock/e2e' : undefined,
    status: 'generated',
  }
}

function toPortalProperty(item: MockEntity) {
  return {
    city: item.address?.city,
    complement: item.address?.complement,
    description: item.description,
    garageSpaceCount: item.garageSpaceCount,
    id: item.id,
    name: item.name,
    neighborhood: item.address?.neighborhood,
    number: item.address?.number,
    postalCode: item.address?.postalCode,
    stateCode: item.address?.stateCode,
    status: item.status,
    streetLine: item.address?.streetLine,
    suggestedRent: item.suggestedRent,
  }
}

function toPortalContract(item: MockEntity) {
  return {
    dueDay: item.dueDay,
    endDate: item.endDate,
    id: item.id,
    isArchived: item.isArchived,
    monthlyRent: item.monthlyRent,
    property: item.property,
    startDate: item.startDate,
    status: item.status,
  }
}

function toPortalPayment(item: MockEntity) {
  return {
    amount: item.amount,
    balance: item.balance,
    contract: item.contract,
    description: item.description,
    dueDate: item.dueDate,
    id: item.id,
    isOverdue: item.isOverdue,
    preferredMethod: item.preferredMethod,
    preferredMethodLabel: item.preferredMethodLabel,
    property: item.property,
    providerCode: item.providerCode,
    providerReference: item.providerReference,
    settledAmount: item.settledAmount,
    status: item.status,
    title: item.title,
  }
}

function toPortalDocument(item: MockEntity) {
  return {
    category: item.category,
    categoryLabel: item.categoryLabel,
    contentType: item.contentType,
    description: item.description,
    downloadRoute: item.downloadRoute,
    fileName: item.fileName,
    id: item.id,
    sizeBytes: item.sizeBytes,
    status: item.status,
    title: item.title,
    uploadedAt: item.uploadedAt,
  }
}

function toPortalOccurrence(item: MockEntity) {
  return {
    contract: item.contract,
    createdAt: item.createdAt,
    description: item.description,
    dueDate: item.dueDate,
    id: item.id,
    isUnresolved: item.isUnresolved,
    priority: item.priority,
    property: item.property,
    status: item.status,
    title: item.title,
    type: item.type,
  }
}

function toPortalInspection(item: MockEntity) {
  return {
    completedAt: item.completedAt,
    completedItems: item.progress?.completedItems,
    contract: item.contract,
    id: item.id,
    progressPercentage: item.progress?.percentage,
    property: item.property,
    scheduledAt: item.scheduledAt,
    status: item.status,
    title: item.title,
    totalItems: item.progress?.totalItems,
    type: item.type,
  }
}

function findById(collection: MockEntity[], state: MockApiState, id: string) {
  const item = collection.find(
    (candidate) => candidate.id === id && candidate.organizationId === state.activeOrganizationId,
  )

  if (!item) {
    throw new Error(`Mock item not found: ${id}`)
  }

  return item
}

function tenantItems<TItem extends MockEntity>(collection: TItem[], state: MockApiState) {
  return collection.filter((item) => item.organizationId === state.activeOrganizationId)
}

function paged<TItem>(items: TItem[]) {
  return {
    hasNextPage: false,
    hasPreviousPage: false,
    items,
    page: 1,
    pageSize: defaultPageSize,
    totalItems: items.length,
    totalPages: 1,
  }
}

function ok(body: unknown, status = 200) {
  return { body, status }
}

function forbidden(detail: string) {
  return ok(
    {
      detail,
      status: 403,
      title: 'Acesso negado',
      type: 'https://httpstatuses.com/403',
    },
    403,
  )
}

async function fulfill(route: Route, body: unknown, status = 200) {
  await route.fulfill({
    body: status === 204 ? undefined : typeof body === 'string' ? body : JSON.stringify(body),
    contentType: typeof body === 'string' ? 'text/plain' : 'application/json',
    status,
  })
}

async function notFound(route: Route, method: string, path: string) {
  await fulfill(
    route,
    {
      detail: `${method} ${path} is not mocked.`,
      status: 404,
      title: 'Mock route not found',
    },
    404,
  )
}

async function requestJson(route: Route): Promise<JsonRecord> {
  const postData = route.request().postData()

  if (postData) {
    try {
      const parsed = JSON.parse(postData) as unknown

      if (parsed && typeof parsed === 'object' && !Array.isArray(parsed)) {
        return parsed as JsonRecord
      }
    } catch {
      return {}
    }
  }

  try {
    const parsed = route.request().postDataJSON() as unknown

    if (parsed && typeof parsed === 'object' && !Array.isArray(parsed)) {
      return parsed as JsonRecord
    }

    return {}
  } catch {
    return {}
  }
}

function getBodyString(body: unknown, key: string) {
  if (!body || typeof body !== 'object' || Array.isArray(body)) {
    return undefined
  }

  const value = (body as JsonRecord)[key]
  return typeof value === 'string' && value.trim().length > 0 ? value : undefined
}

function getMoneyAmount(value: unknown) {
  if (value && typeof value === 'object' && !Array.isArray(value)) {
    const amount = (value as { amount?: unknown }).amount
    return typeof amount === 'number' ? amount : undefined
  }

  return undefined
}

function propertyStatus(status: string) {
  const labels: Record<string, { label: string; tone: string }> = {
    archived: { label: 'Arquivado', tone: 'archived' },
    available: { label: 'Disponivel', tone: 'success' },
    maintenance: { label: 'Manutencao', tone: 'warning' },
    rented: { label: 'Alugado', tone: 'info' },
  }
  const statusLabel = labels[status] ?? labels.available
  return label(status, statusLabel.label, statusLabel.tone)
}

function documentCategoryLabel(category: string) {
  const labels: Record<string, string> = {
    contract: 'Contrato',
    'payment-receipt': 'Comprovante',
    resident: 'Morador',
  }

  return labels[category] ?? 'Documento'
}

function paymentMethodLabel(method: string) {
  const labels: Record<string, string> = {
    boleto: 'Boleto',
    paypal: 'PayPal',
    pix: 'Pix',
  }

  return labels[method] ?? method
}
