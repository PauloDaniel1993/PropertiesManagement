import { expect, test } from '@playwright/test'
import {
  clickButton,
  expectA11yClean,
  openAdminModule,
  signInAdmin,
  switchActiveOrganization,
} from './fixtures/actions'
import { installMockApi } from './fixtures/mockApi'

test.describe('admin shell, accessibility, and visual coverage', () => {
  test.beforeEach(async ({ page }) => {
    await installMockApi(page)
  })

  test('supports login, organization switching, navigation, locale switching, and logout', async ({
    page,
  }) => {
    await signInAdmin(page)
    await switchActiveOrganization(page, 'org-moradas')
    const adminNavigation = page.getByRole('navigation', {
      name: /Administra..o|Administration/i,
    })

    await adminNavigation.getByRole('link', { name: /Im.veis|Properties/ }).click()
    await expect(page).toHaveURL(/\/imoveis$/)
    await expect(page.getByRole('heading', { name: /Im.veis|Properties/ })).toBeVisible()
    await expect(page.getByRole('table')).toContainText('Loft Moradas Prime')
    await expect(page.getByRole('table')).not.toContainText('Casa Calabria')

    await adminNavigation.getByRole('link', { name: 'Dashboard' }).click()
    await expect(page).toHaveURL(/\/dashboard$/)

    await clickButton(page, /Alternar idioma|Toggle language/)
    await expect(adminNavigation.getByRole('link', { name: 'Properties' })).toBeVisible()
    await clickButton(page, /Alternar idioma|Toggle language/)
    await expect(adminNavigation.getByRole('link', { name: /Im.veis|Properties/ })).toBeVisible()

    await page.getByLabel(/Perfil|Profile/).click()
    await page.getByRole('button', { name: /Sair|Sign out/ }).click()
    await expect(page).toHaveURL(/\/login$/)
  })

  test('passes automated checks for sidebar, topbar, table, and dialog surfaces', async ({
    page,
  }) => {
    await signInAdmin(page)
    await expectA11yClean(page, '.admin-sidebar')
    await expectA11yClean(page, '.admin-topbar')

    await openAdminModule(page, '/imoveis', /Im.veis|Properties/)
    await expect(page.getByRole('table')).toBeVisible()
    await expectA11yClean(page, 'table')

    await clickButton(page, /Adicionar im.vel|Criar im.vel|Add property/)
    await expect(page.getByRole('dialog')).toBeVisible()
    await expectA11yClean(page, '[role="dialog"]')
  })

  test('captures representative authenticated shell and module screenshots', async ({ page }) => {
    await signInAdmin(page)
    await expect(page).toHaveScreenshot('admin-dashboard-shell.png', { fullPage: true })

    await openAdminModule(page, '/imoveis', /Im.veis|Properties/)
    await expect(page).toHaveScreenshot('admin-properties-module.png', { fullPage: true })

    await page.setViewportSize({ height: 900, width: 390 })
    await expect(
      page.getByRole('button', { name: /Abrir navega..o|Open navigation/ }),
    ).toBeVisible()
    await expect(page).toHaveScreenshot('admin-mobile-shell.png', { fullPage: true })
  })
})
