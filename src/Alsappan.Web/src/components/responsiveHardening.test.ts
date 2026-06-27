/// <reference types="node" />

import { readFileSync } from 'node:fs'
import administratorsSource from '../features/administrators/AdministratorsListPage.tsx?raw'
import auditSource from '../features/audit/AuditListPage.tsx?raw'
import contractsSource from '../features/contracts/ContractsListPage.tsx?raw'
import documentsSource from '../features/documents/DocumentsListPage.tsx?raw'
import inspectionsSource from '../features/inspections/InspectionsListPage.tsx?raw'
import notificationsSource from '../features/notifications/NotificationsListPage.tsx?raw'
import occurrencesSource from '../features/occurrences/OccurrencesListPage.tsx?raw'
import paymentsSource from '../features/payments/PaymentsListPage.tsx?raw'
import petsSource from '../features/pets/PetsListPage.tsx?raw'
import propertiesSource from '../features/properties/PropertiesListPage.tsx?raw'
import residentsSource from '../features/residents/ResidentsListPage.tsx?raw'
import utilityAccountsSource from '../features/utilityAccounts/UtilityAccountsListPage.tsx?raw'
import vehiclesSource from '../features/vehicles/VehiclesListPage.tsx?raw'

const indexCss = readFileSync(`${process.cwd()}/src/index.css`, 'utf8')

const adminModuleListPages = [
  ['administrators', administratorsSource],
  ['audit', auditSource],
  ['contracts', contractsSource],
  ['documents', documentsSource],
  ['inspections', inspectionsSource],
  ['notifications', notificationsSource],
  ['occurrences', occurrencesSource],
  ['payments', paymentsSource],
  ['pets', petsSource],
  ['properties', propertiesSource],
  ['residents', residentsSource],
  ['utilityAccounts', utilityAccountsSource],
  ['vehicles', vehiclesSource],
] as const

describe('responsive and overflow hardening coverage', () => {
  it('keeps desktop, tablet, and mobile layout guardrails in the shared stylesheet', () => {
    expect(indexCss.length).toBeGreaterThan(0)
    expect(indexCss).toMatch(
      /\.admin-app\s*{[\s\S]*display: grid;[\s\S]*grid-template-columns: var\(--shell-sidebar-width\) minmax\(0, 1fr\);/,
    )
    expect(indexCss).toMatch(
      /@media \(max-width: 880px\)[\s\S]*\.admin-app,\s*\.admin-app\[data-sidebar='collapsed'\]\s*{[\s\S]*grid-template-columns: 1fr;/,
    )
    expect(indexCss).toMatch(
      /@media \(max-width: 880px\)[\s\S]*\.als-data-table__table\s*{[\s\S]*min-width: 640px !important;/,
    )
    expect(indexCss).toMatch(
      /@media \(max-width: 520px\)[\s\S]*\.als-page-header__actions,\s*\.als-action-button\s*{[\s\S]*width: 100%;/,
    )
    expect(indexCss).toMatch(
      /@media \(max-width: 520px\)[\s\S]*\.als-filter-bar__actions,\s*\.als-pagination__controls\s*{[\s\S]*width: 100%;/,
    )
  })

  it('keeps long Portuguese labels and entity names from forcing layout overflow', () => {
    expect(indexCss.length).toBeGreaterThan(0)
    expect(indexCss).toMatch(
      /\.als-page-header__title,[\s\S]*\.als-data-table__header-cell\s*{[\s\S]*overflow-wrap: anywhere;/,
    )
    expect(indexCss).toMatch(/\.als-data-table__cell\s*{[\s\S]*max-width: 0;/)
    expect(indexCss).toMatch(
      /\.als-data-table__cell > \*,\s*\.als-data-table__header-cell > \*\s*{[\s\S]*min-width: 0;/,
    )
    expect(indexCss).toMatch(
      /@media \(max-width: 880px\)[\s\S]*\.als-detail-list\s*{[\s\S]*grid-template-columns: 1fr !important;/,
    )
    expect(indexCss).toMatch(
      /@media \(max-width: 880px\)[\s\S]*\.als-relationship-panel__item\s*{[\s\S]*flex-direction: column;/,
    )
  })

  it('applies shared responsive table primitives to every administrative module list page', () => {
    expect(adminModuleListPages).toHaveLength(13)

    for (const [moduleName, source] of adminModuleListPages) {
      expect(source, `${moduleName} should render through PageHeader`).toContain('PageHeader')
      expect(source, `${moduleName} should render through SearchInput`).toContain('SearchInput')
      expect(source, `${moduleName} should render through DataTable`).toContain('DataTable')
    }
  })
})
