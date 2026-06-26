import enUS from '../i18n/locales/en-US.json'
import ptBR from '../i18n/locales/pt-BR.json'
import {
  adminMenuItems,
  adminRoutePaths,
  defaultAuthenticatedRoute,
  findAdminMenuItemByPath,
} from './menuContract'

const expectedPaths = [
  '/dashboard',
  '/imoveis',
  '/contratos',
  '/moradores',
  '/administradores',
  '/pagamentos',
  '/contas-de-consumo',
  '/documentos',
  '/pets',
  '/veiculos',
  '/ocorrencias',
  '/vistorias',
  '/notificacoes',
  '/timeline',
  '/auditoria',
  '/configuracoes',
]

function translationValue(resource: unknown, key: string) {
  return key.split('.').reduce<unknown>((current, segment) => {
    if (current && typeof current === 'object' && segment in current) {
      return (current as Record<string, unknown>)[segment]
    }

    return undefined
  }, resource)
}

describe('admin menu contract', () => {
  it('defines a stable ASCII route for every visible menu item', () => {
    expect(adminRoutePaths).toEqual(expectedPaths)
    expect(defaultAuthenticatedRoute).toBe('/dashboard')

    for (const path of adminRoutePaths) {
      expect(path).toMatch(/^\/[a-z0-9/-]+$/)
      expect(path).not.toContain('//')
    }
  })

  it('exposes localized labels and default page copy in pt-BR and en-US', () => {
    for (const item of adminMenuItems) {
      const keys = [
        item.menuLabelKey,
        item.page.titleKey,
        item.page.descriptionKey,
        item.page.emptyTitleKey,
        item.page.emptyDescriptionKey,
      ]

      for (const key of keys) {
        expect(translationValue(ptBR, key)).toEqual(expect.any(String))
        expect(translationValue(enUS, key)).toEqual(expect.any(String))
      }
    }
  })

  it('resolves active menu items from normalized paths', () => {
    expect(findAdminMenuItemByPath('/imoveis/')?.id).toBe('properties')
    expect(findAdminMenuItemByPath('/configuracoes')?.id).toBe('settings')
    expect(findAdminMenuItemByPath('/desconhecido')).toBeUndefined()
  })
})
