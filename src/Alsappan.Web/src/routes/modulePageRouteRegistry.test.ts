import { getModulePageComponent, implementedModuleRouteIds } from './modulePageRouteRegistry'

describe('module page route registry', () => {
  it('discovers implemented feature pages without root router edits', () => {
    expect(implementedModuleRouteIds).toEqual([
      'administrators',
      'audit',
      'contracts',
      'documents',
      'inspections',
      'notifications',
      'payments',
      'pets',
      'properties',
      'residents',
      'timeline',
      'utilityAccounts',
      'vehicles',
    ])
    expect(getModulePageComponent('administrators')).toEqual(expect.any(Function))
    expect(getModulePageComponent('audit')).toEqual(expect.any(Function))
    expect(getModulePageComponent('contracts')).toEqual(expect.any(Function))
    expect(getModulePageComponent('documents')).toEqual(expect.any(Function))
    expect(getModulePageComponent('inspections')).toEqual(expect.any(Function))
    expect(getModulePageComponent('notifications')).toEqual(expect.any(Function))
    expect(getModulePageComponent('payments')).toEqual(expect.any(Function))
    expect(getModulePageComponent('pets')).toEqual(expect.any(Function))
    expect(getModulePageComponent('properties')).toEqual(expect.any(Function))
    expect(getModulePageComponent('residents')).toEqual(expect.any(Function))
    expect(getModulePageComponent('timeline')).toEqual(expect.any(Function))
    expect(getModulePageComponent('utilityAccounts')).toEqual(expect.any(Function))
    expect(getModulePageComponent('vehicles')).toEqual(expect.any(Function))
  })

  it('falls back to placeholders for menu items without implemented pages', () => {
    expect(getModulePageComponent('settings')).toBeUndefined()
  })
})
