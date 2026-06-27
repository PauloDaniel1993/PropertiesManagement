import { expect, type Locator, type Page } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'

export async function signInAdmin(page: Page) {
  await page.goto('/login')
  await expect(page.getByRole('heading', { name: /Entrar|Sign in/ })).toBeVisible()
  await page.locator('input[name="email"]').fill('paulo.carneiro.dev@protonmail.com')
  await page.locator('input[name="password"]').fill('Password123!')
  await page.getByRole('button', { name: /Entrar|Sign in/ }).click()
  await expect(page).toHaveURL(/\/dashboard$/)
  await expect(page.getByRole('navigation', { name: /administrativa|admin/i })).toBeVisible()
}

export async function signInResident(page: Page) {
  await page.goto('/resident-login')
  await expect(page.getByRole('heading', { name: /Entrar|Sign in/ })).toBeVisible()
  await page.locator('input[name="email"]').fill('resident@example.com')
  await page.locator('input[name="password"]').fill('Password123!')
  await page.getByRole('button', { name: /Entrar|Sign in/ }).click()
  await expect(page).toHaveURL(/\/portal$/)
  await expect(
    page.getByRole('navigation', { name: /Navegacao do morador|Resident navigation|menu/i }),
  ).toBeVisible()
}

export async function switchActiveOrganization(page: Page, organizationId: string) {
  const switchResponse = page.waitForResponse(
    (response) =>
      response.url().includes('/v1/auth/switch-organization') &&
      response.request().method() === 'POST',
  )

  const organizationSwitcher = page.getByLabel(/Organiza..o ativa|Active organization/)
  await organizationSwitcher.selectOption(organizationId)
  await switchResponse
  await expect(organizationSwitcher).toHaveValue(organizationId)
}

export async function navigateInApp(page: Page, path: string) {
  await page.evaluate((nextPath) => {
    window.history.pushState({}, '', nextPath)
    window.dispatchEvent(new PopStateEvent('popstate'))
  }, path)
}

export async function expectA11yClean(page: Page, include?: string) {
  const builder = new AxeBuilder({ page })

  if (include) {
    builder.include(include)
  }

  const results = await builder.analyze()
  const violations = results.violations.map((violation) => ({
    id: violation.id,
    impact: violation.impact,
    nodes: violation.nodes.map((node) => node.target.join(' ')),
  }))

  expect(violations).toEqual([])
}

export async function clickButton(page: Page, name: RegExp | string) {
  await page.getByRole('button', { name }).last().click()
}

export async function clickRowAction(page: Page, name: RegExp | string) {
  await page.getByLabel(name).click()
}

export async function fillByLabel(page: Page | Locator, label: RegExp | string, value: string) {
  const field = page.getByLabel(label).last()
  await field.fill(value)
}

export async function selectByLabel(page: Page | Locator, label: RegExp | string, value: string) {
  const field = page.getByLabel(label).last()
  await field.selectOption(value)
}

export async function setFileByLabel(page: Page | Locator, label: RegExp | string, name: string) {
  await page
    .getByLabel(label)
    .last()
    .setInputFiles({
      buffer: Buffer.from(`mock ${name}`),
      mimeType: 'application/pdf',
      name,
    })
}

function escapeRegExp(value: string) {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
}

function looseTextMatcher(value: RegExp | string) {
  if (value instanceof RegExp) {
    return value
  }

  const pattern = escapeRegExp(value)
    .replace(/[aA]/g, '[aAáÁàÀâÂãÃ]')
    .replace(/[eE]/g, '[eEéÉêÊ]')
    .replace(/[iI]/g, '[iIíÍ]')
    .replace(/[oO]/g, '[oOóÓôÔõÕ]')
    .replace(/[uU]/g, '[uUúÚ]')
    .replace(/[cC]/g, '[cCçÇ]')

  return new RegExp(`^${pattern}$`, 'i')
}

export async function openAdminModule(page: Page, path: string, heading: RegExp | string) {
  await navigateInApp(page, path)
  await expect(page).toHaveURL(new RegExp(`${escapeRegExp(path)}$`))
  await expect(page.getByRole('heading', { name: looseTextMatcher(heading) })).toBeVisible()
}

export async function waitForTableRow(page: Page, text: RegExp | string) {
  await expect(page.getByRole('table')).toContainText(text)
}
