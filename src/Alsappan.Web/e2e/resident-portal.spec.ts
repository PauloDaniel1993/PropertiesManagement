import { expect, test, type Page } from '@playwright/test'
import {
  clickButton,
  fillByLabel,
  navigateInApp,
  openAdminModule,
  selectByLabel,
  signInAdmin,
  signInResident,
} from './fixtures/actions'
import { installMockApi } from './fixtures/mockApi'

function residentNavigation(page: Page) {
  return page.getByRole('navigation', {
    name: /Navegacao do morador|Resident navigation|menu/i,
  })
}

test.describe('resident portal E2E flows', () => {
  test.beforeEach(async ({ page }) => {
    await installMockApi(page)
    await signInResident(page)
  })

  test('logs in with resident-scoped visibility and blocks admin data access', async ({ page }) => {
    await expect(page.getByRole('heading', { name: /Portal do morador/ })).toBeVisible()
    await expect(page.locator('#main-content').getByText('Ana Portal')).toBeVisible()
    await residentNavigation(page)
      .getByRole('link', { name: /^Imovel$/ })
      .click()
    await expect(page).toHaveURL(/\/portal\/imovel$/)
    await expect(page.getByText('Casa Calabria')).toBeVisible()
    await expect(page.getByText('Loft Moradas Prime')).not.toBeVisible()

    const crossResidentResponse = await page.evaluate(async () => {
      const response = await fetch('/v1/resident-portal/residents/resident-moradas/summary', {
        credentials: 'include',
        headers: { Accept: 'application/json' },
      })
      const body = (await response.json()) as { detail?: string; title?: string }

      return { body, status: response.status }
    })

    expect(crossResidentResponse.status).toBe(403)
    expect(crossResidentResponse.body.detail).toMatch(/outro morador/)

    await navigateInApp(page, '/moradores')
    await expect(
      page.getByText(/Acesso administrativo obrigat.rio|Admin access required/),
    ).toBeVisible()
  })

  test('lets a resident create an occurrence', async ({ page }) => {
    await residentNavigation(page)
      .getByRole('link', { name: /^Ocorrencias$/ })
      .click()
    await expect(page).toHaveURL(/\/portal\/ocorrencias$/)
    await fillByLabel(page, /Titulo/, 'Solicitacao do portal E2E')
    await selectByLabel(page, /Contrato/, 'contract-calabria')
    await selectByLabel(page, /Tipo/, 'request')
    await selectByLabel(page, /Prioridade/, 'medium')
    await fillByLabel(page, /Descreva o que aconteceu/, 'A torneira do banheiro esta pingando.')
    await clickButton(page, 'Criar ocorrencia')
    await expect(page.getByRole('table')).toContainText('Solicitacao do portal E2E')
  })

  test('shows mocked boleto, Pix, and PayPal instructions in resident payments', async ({
    page,
  }) => {
    await residentNavigation(page)
      .getByRole('link', { name: /^Pagamentos$/ })
      .click()
    await expect(page).toHaveURL(/\/portal\/pagamentos$/)
    await page
      .getByRole('row', { name: /Aluguel junho - Casa Calabria/ })
      .getByRole('button', { name: 'Gerar instrucoes' })
      .click()
    const instructionsPanel = page.locator('.als-detail-section', {
      has: page.getByRole('heading', { name: 'Instrucoes de pagamento' }),
    })
    await expect(instructionsPanel).toBeVisible()

    for (const [provider, expectedText] of [
      ['mock-pix', 'pix://mock/e2e'],
      ['mock-boleto', '23793.38128'],
      ['mock-paypal', 'paypal-intent-e2e'],
    ] as const) {
      await selectByLabel(instructionsPanel, /Provedor/, provider)
      await instructionsPanel.getByRole('button', { name: 'Gerar instrucoes' }).click()
      await expect(instructionsPanel.getByText(expectedText)).toBeVisible()
    }
  })

  test('keeps the resident portal on the default white-label presentation', async ({ page }) => {
    const brand = page.locator('.admin-sidebar__brand')
    await expect(brand.getByText('Alsappan', { exact: true })).toBeVisible()
    await expect(brand.getByText('Portal do morador', { exact: true })).toBeVisible()
    await expect(page.getByText('Alsappan Demo')).toBeVisible()
  })
})

test.describe('white-label branding settings', () => {
  test.beforeEach(async ({ page }) => {
    await installMockApi(page)
    await signInAdmin(page)
  })

  test('validates contrast and saves organization branding changes', async ({ page }) => {
    await openAdminModule(page, '/configuracoes', 'Configuracoes')
    await expect(page.getByText('Marca e suporte')).toBeVisible()
    await expect(page.getByLabel('Nome da marca')).toHaveValue('Alsappan')

    await page.getByLabel('Cor primaria hex', { exact: true }).fill('#ffffff')
    await page.getByLabel('Texto da cor primaria hex', { exact: true }).fill('#ffffff')
    await page.getByRole('button', { name: 'Salvar' }).last().click()
    await expect(page.getByText(/Contraste insuficiente/)).toBeVisible()

    await page.getByLabel('Nome da marca').fill('Moradas E2E')
    await page.getByLabel('Cor primaria hex', { exact: true }).fill('#0f766e')
    await page.getByLabel('Texto da cor primaria hex', { exact: true }).fill('#ffffff')
    await page.getByLabel('Cor de destaque hex', { exact: true }).fill('#f59e0b')
    await page.getByLabel('Texto do destaque hex', { exact: true }).fill('#111827')
    await page.getByRole('button', { name: 'Salvar' }).last().click()
    await expect(page.getByLabel('Nome da marca')).toHaveValue('Moradas E2E')

    const adminBrand = page.locator('.admin-sidebar__brand')
    await expect(adminBrand.getByText('Moradas E2E', { exact: true })).toBeVisible()
    await expect(adminBrand.getByText(/Gest.o de im.veis/)).toBeVisible()

    await signInResident(page)
    const brand = page.locator('.admin-sidebar__brand')
    await expect(brand.getByText('Moradas E2E', { exact: true })).toBeVisible()
    await expect(brand.getByText('Portal do morador', { exact: true })).toBeVisible()
  })
})
