import { getModulePageComponent, implementedModuleRouteIds } from './modulePageRouteRegistry'

describe('module page route registry', () => {
  it('discovers implemented feature pages without root router edits', () => {
    expect(implementedModuleRouteIds).toEqual([
      'administrators',
      'audit',
      'notifications',
      'properties',
      'residents',
      'timeline',
    ])
    expect(getModulePageComponent('administrators')).toEqual(expect.any(Function))
    expect(getModulePageComponent('audit')).toEqual(expect.any(Function))
    expect(getModulePageComponent('notifications')).toEqual(expect.any(Function))
    expect(getModulePageComponent('properties')).toEqual(expect.any(Function))
    expect(getModulePageComponent('residents')).toEqual(expect.any(Function))
    expect(getModulePageComponent('timeline')).toEqual(expect.any(Function))
  })

  it('falls back to placeholders for menu items without implemented pages', () => {
    expect(getModulePageComponent('contracts')).toBeUndefined()
    expect(getModulePageComponent('settings')).toBeUndefined()
  })
})
