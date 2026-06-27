import { getModulePageComponent, implementedModuleRouteIds } from './modulePageRouteRegistry'

describe('module page route registry', () => {
  it('discovers implemented feature pages without root router edits', () => {
    expect(implementedModuleRouteIds).toEqual(['administrators', 'properties', 'residents'])
    expect(getModulePageComponent('administrators')).toEqual(expect.any(Function))
    expect(getModulePageComponent('properties')).toEqual(expect.any(Function))
    expect(getModulePageComponent('residents')).toEqual(expect.any(Function))
  })

  it('falls back to placeholders for menu items without implemented pages', () => {
    expect(getModulePageComponent('contracts')).toBeUndefined()
    expect(getModulePageComponent('settings')).toBeUndefined()
  })
})
