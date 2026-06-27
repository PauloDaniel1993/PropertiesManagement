export const adminRouteIds = [
  'dashboard',
  'properties',
  'contracts',
  'residents',
  'administrators',
  'payments',
  'utilityAccounts',
  'documents',
  'pets',
  'vehicles',
  'occurrences',
  'inspections',
  'notifications',
  'timeline',
  'audit',
  'settings',
] as const

export type AdminRouteId = (typeof adminRouteIds)[number]

export const menuIconIds = [
  'layout-dashboard',
  'house',
  'file-text',
  'users',
  'shield-check',
  'circle-dollar-sign',
  'receipt-text',
  'folder-open',
  'paw-print',
  'car-front',
  'circle-alert',
  'clipboard-check',
  'bell',
  'chart-no-axes-combined',
  'search-check',
  'settings',
] as const

export type MenuIconId = (typeof menuIconIds)[number]

export type MenuItemPageCopy = Readonly<{
  titleKey: string
  descriptionKey: string
  emptyTitleKey: string
  emptyDescriptionKey: string
}>

export type AdminMenuItemContract = Readonly<{
  id: AdminRouteId
  path: `/${string}`
  iconId: MenuIconId
  menuLabelKey: string
  page: MenuItemPageCopy
  requiredPermission: `${string}.read`
}>

const pageCopy = (id: AdminRouteId): MenuItemPageCopy => ({
  descriptionKey: `pages.${id}.description`,
  emptyDescriptionKey: `pages.${id}.empty.description`,
  emptyTitleKey: `pages.${id}.empty.title`,
  titleKey: `pages.${id}.title`,
})

const adminMenuItemDefinitions = [
  {
    iconId: 'layout-dashboard',
    id: 'dashboard',
    path: '/dashboard',
    requiredPermission: 'dashboard.read',
  },
  {
    iconId: 'house',
    id: 'properties',
    path: '/imoveis',
    requiredPermission: 'properties.read',
  },
  {
    iconId: 'file-text',
    id: 'contracts',
    path: '/contratos',
    requiredPermission: 'contracts.read',
  },
  {
    iconId: 'users',
    id: 'residents',
    path: '/moradores',
    requiredPermission: 'residents.read',
  },
  {
    iconId: 'shield-check',
    id: 'administrators',
    path: '/administradores',
    requiredPermission: 'administrators.read',
  },
  {
    iconId: 'circle-dollar-sign',
    id: 'payments',
    path: '/pagamentos',
    requiredPermission: 'payments.read',
  },
  {
    iconId: 'receipt-text',
    id: 'utilityAccounts',
    path: '/contas-de-consumo',
    requiredPermission: 'utility-accounts.read',
  },
  {
    iconId: 'folder-open',
    id: 'documents',
    path: '/documentos',
    requiredPermission: 'documents.read',
  },
  {
    iconId: 'paw-print',
    id: 'pets',
    path: '/pets',
    requiredPermission: 'pets.read',
  },
  {
    iconId: 'car-front',
    id: 'vehicles',
    path: '/veiculos',
    requiredPermission: 'vehicles.read',
  },
  {
    iconId: 'circle-alert',
    id: 'occurrences',
    path: '/ocorrencias',
    requiredPermission: 'occurrences.read',
  },
  {
    iconId: 'clipboard-check',
    id: 'inspections',
    path: '/vistorias',
    requiredPermission: 'inspections.read',
  },
  {
    iconId: 'bell',
    id: 'notifications',
    path: '/notificacoes',
    requiredPermission: 'notifications.read',
  },
  {
    iconId: 'chart-no-axes-combined',
    id: 'timeline',
    path: '/timeline',
    requiredPermission: 'timeline.read',
  },
  {
    iconId: 'search-check',
    id: 'audit',
    path: '/auditoria',
    requiredPermission: 'audit.read',
  },
  {
    iconId: 'settings',
    id: 'settings',
    path: '/configuracoes',
    requiredPermission: 'settings.read',
  },
] as const satisfies readonly Omit<AdminMenuItemContract, 'menuLabelKey' | 'page'>[]

export const adminMenuItems = adminMenuItemDefinitions.map((item): AdminMenuItemContract => ({
  ...item,
  menuLabelKey: `navigation.items.${item.id}.label`,
  page: pageCopy(item.id),
}))

export const defaultAuthenticatedRoute = '/dashboard'

export const adminRoutePaths = adminMenuItems.map((item) => item.path)

export function findAdminMenuItemByPath(pathname: string) {
  const normalizedPathname = pathname.length > 1 ? pathname.replace(/\/+$/, '') : pathname

  return adminMenuItems.find((item) => item.path === normalizedPathname)
}
