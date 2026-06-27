import { expect, test } from '@playwright/test'
import {
  clickButton,
  clickRowAction,
  fillByLabel,
  openAdminModule,
  selectByLabel,
  setFileByLabel,
  signInAdmin,
  switchActiveOrganization,
  waitForTableRow,
} from './fixtures/actions'
import { installMockApi } from './fixtures/mockApi'

test.describe('admin module E2E workflows', () => {
  test.beforeEach(async ({ page }) => {
    await installMockApi(page)
    await signInAdmin(page)
  })

  test('creates a property, resident, contract, and linked document', async ({ page }) => {
    await openAdminModule(page, '/imoveis', 'Imoveis')
    await clickButton(page, /Adicionar imovel|Criar imovel/)
    await fillByLabel(page, /^Imovel/, 'Apartamento Jardim E2E')
    await fillByLabel(page, /Logradouro/, 'Rua das Flores')
    await fillByLabel(page, /Numero/, '101')
    await fillByLabel(page, /Bairro/, 'Jardim Paulista')
    await fillByLabel(page, /Cidade/, 'Sao Paulo')
    await fillByLabel(page, /UF/, 'SP')
    await fillByLabel(page, /Aluguel sugerido/, '2500')
    await page.getByLabel('Possui garagem').check()
    await fillByLabel(page, /Vagas de garagem/, '1')
    await fillByLabel(page, /Observacoes/, 'Sol da manha')
    await clickButton(page, 'Criar imovel')
    await waitForTableRow(page, 'Apartamento Jardim E2E')

    await openAdminModule(page, '/moradores', 'Moradores')
    await clickButton(page, /Adicionar morador|Criar morador/)
    await fillByLabel(page, /Nome completo/, 'Maria E2E')
    await fillByLabel(page, /E-mail/, 'maria.e2e@example.com')
    await clickButton(page, 'Verificar duplicidades')
    await clickButton(page, 'Criar morador')
    await waitForTableRow(page, 'Maria E2E')

    await openAdminModule(page, '/contratos', 'Contratos')
    await clickButton(page, /Adicionar contrato|Criar contrato/)
    await selectByLabel(page, /Imovel/, 'prop-calabria')
    await selectByLabel(page, /Responsavel principal/, 'resident-joao')
    await page.getByLabel(/Joao da Silva/).check()
    await fillByLabel(page, /Inicio/, '2026-07-01')
    await fillByLabel(page, /Fim/, '2027-06-30')
    await fillByLabel(page, /Aluguel mensal/, '2500')
    await page.getByLabel(/Dia de vencimento/).fill('10')
    await fillByLabel(page, /Caucao/, '2500')
    await fillByLabel(page, /^Observacoes$/, 'Contrato criado por E2E')
    await page.getByLabel(/Criar como contrato ativo/).check()
    await clickButton(page, 'Criar contrato')
    await waitForTableRow(page, 'Casa Calabria')

    await openAdminModule(page, '/documentos', 'Documentos')
    await clickButton(page, 'Enviar documento')
    await fillByLabel(page, /Titulo/, 'Contrato E2E assinado')
    await selectByLabel(page, /Categoria/, 'contract')
    await setFileByLabel(page, /Arquivo/, 'contrato-e2e.pdf')
    await selectByLabel(page, /Tipo do registro vinculado/, 'contract')
    await fillByLabel(page, /ID do registro vinculado/, 'contract-calabria')
    await fillByLabel(page, /Rotulo do vinculo/, 'Contrato E2E')
    await clickButton(page, 'Enviar documento')
    await waitForTableRow(page, 'Contrato E2E assinado')
  })

  test('creates and settles a payment with receipt and mocked provider instructions', async ({
    page,
  }) => {
    await openAdminModule(page, '/pagamentos', 'Pagamentos')
    await clickButton(page, /Adicionar pagamento|Criar pagamento/)
    await fillByLabel(page, /Titulo/, 'Pagamento E2E')
    await fillByLabel(page, /Valor/, '1800')
    await fillByLabel(page, /Vencimento/, '2026-07-10')
    await selectByLabel(page, /Metodo preferencial/, 'pix')
    await selectByLabel(page, /Status da conciliacao/, 'pending')
    await fillByLabel(page, /ID do contrato/, 'contract-calabria')
    await fillByLabel(page, /ID do imovel/, 'prop-calabria')
    await fillByLabel(page, /ID do morador/, 'resident-joao')
    await fillByLabel(page, /Descricao/, 'Mensalidade criada em E2E')
    await clickButton(page, 'Criar pagamento')
    await waitForTableRow(page, 'Pagamento E2E')

    await clickRowAction(page, /Receber Pagamento E2E/)
    const settlementDialog = page.getByRole('dialog', { name: /Registrar recebimento/ })
    await settlementDialog.getByLabel(/Valor/).fill('1800')
    await settlementDialog.getByLabel(/ID do recibo|Receipt document ID/).fill('document-contract')
    await settlementDialog.getByLabel(/Referencia do provedor/).fill('PIX-1')
    await settlementDialog.getByRole('button', { name: 'Registrar recebimento' }).click()
    await expect(page.getByRole('table')).toContainText('Pago')

    await clickRowAction(page, /Instrucoes Pagamento E2E/)
    const instructionDialog = page.getByRole('dialog', { name: /Instrucoes de pagamento/ })
    await expect(instructionDialog).toBeVisible()

    for (const [provider, expectedText] of [
      ['mock-pix', 'pix://mock/e2e'],
      ['mock-boleto', '23793.38128'],
      ['mock-paypal', 'paypal-intent-e2e'],
    ] as const) {
      await instructionDialog.getByLabel(/Provedor/).selectOption(provider)
      await instructionDialog.getByRole('button', { name: 'Gerar instrucoes' }).click()
      await expect(instructionDialog).toContainText(expectedText)
    }

    await instructionDialog.getByText('Fechar').click()
    await clickRowAction(page, /Ver detalhes Pagamento E2E/)
    await expect(page.getByText('Recebimentos')).toBeVisible()
    await expect(page.getByText('PIX-1')).toBeVisible()
    await page.getByRole('tab', { name: 'Relacionamentos' }).click()
    await expect(page.getByText('Comprovante')).toBeVisible()
  })

  test('creates, assigns, comments on, and resolves an occurrence', async ({ page }) => {
    await openAdminModule(page, '/ocorrencias', 'Ocorrencias')
    await clickButton(page, /Nova ocorrencia|Criar ocorrencia/)
    await fillByLabel(page, /Titulo/, 'Ocorrencia E2E')
    await selectByLabel(page, /Tipo/, 'maintenance')
    await selectByLabel(page, /Prioridade/, 'high')
    await fillByLabel(page, /Descricao/, 'Fluxo completo de ocorrencia')
    await selectByLabel(page, /Imovel/, 'prop-calabria')
    await selectByLabel(page, /Morador/, 'resident-joao')
    await selectByLabel(page, /Contrato/, 'contract-calabria')
    await selectByLabel(page, /Responsavel/, 'admin-paulo')
    await clickButton(page, 'Criar ocorrencia')
    await waitForTableRow(page, 'Ocorrencia E2E')

    await clickRowAction(page, /Ver detalhes Ocorrencia E2E/)
    await page.getByRole('tab', { name: 'Workflow' }).click()
    await selectByLabel(page, /Responsavel/, 'admin-paulo')
    await clickButton(page, 'Atribuir')
    await selectByLabel(page, /Prioridade/, 'urgent')
    await clickButton(page, 'Alterar prioridade')
    await selectByLabel(page, /^Status$/, 'in-progress')
    await clickButton(page, 'Alterar status')
    await fillByLabel(page, /Notas da resolucao/, 'Conserto realizado.')
    await clickButton(page, 'Resolver')
    await expect(page.getByText('Conserto realizado.')).toBeVisible()

    await page.getByRole('tab', { name: 'Comentarios' }).click()
    await page.getByLabel('Comentario', { exact: true }).fill('Equipe avisada.')
    await page.getByLabel('Comentario interno').check()
    await clickButton(page, 'Adicionar comentario')
    await expect(page.getByText('Equipe avisada.')).toBeVisible()
  })

  test('schedules, edits, completes, and views an inspection report', async ({ page }) => {
    await openAdminModule(page, '/vistorias', 'Vistorias')
    await clickButton(page, /Agendar vistoria/)
    await selectByLabel(page, /Tipo/, 'move-in')
    await fillByLabel(page, /Data agendada/, '2026-07-02T09:00')
    await selectByLabel(page, /Responsavel/, 'admin-paulo')
    await fillByLabel(page, /Titulo/, 'Vistoria E2E')
    await selectByLabel(page, /Imovel/, 'prop-calabria')
    await selectByLabel(page, /Contrato/, 'contract-calabria')
    await selectByLabel(page, /Morador/, 'resident-joao')
    await fillByLabel(page, /Papel da assinatura/, 'Morador')
    await fillByLabel(page, /Nome do assinante/, 'Joao da Silva')
    await clickButton(page, 'Agendar vistoria')
    await waitForTableRow(page, 'Vistoria E2E')

    await clickRowAction(page, /Editar Vistoria E2E/)
    await fillByLabel(page, /Titulo/, 'Vistoria E2E editada')
    await clickButton(page, 'Salvar alteracoes')
    await waitForTableRow(page, 'Vistoria E2E editada')

    await clickRowAction(page, /Ver detalhes Vistoria E2E editada/)
    await clickButton(page, 'Iniciar')
    await clickButton(page, 'Concluir')
    await expect(page.getByRole('heading', { name: /Relatorio concluido/ })).toBeVisible()
  })

  test('handles notification read state, deep links, audit filtering, and cross-organization isolation', async ({
    page,
  }) => {
    await openAdminModule(page, '/notificacoes', 'Notificacoes')
    await expect(page.getByText(/1 nao lidas/)).toBeVisible()
    await page.getByRole('searchbox', { name: 'Buscar notificacoes' }).fill('aluguel')
    await selectByLabel(page, 'Categoria', 'payments')
    await selectByLabel(page, 'Estado de leitura', 'false')
    await clickRowAction(page, /Marcar como lida Pagamento vencido/)
    await expect(page.getByText(/0 nao lidas/)).toBeVisible()
    await page.getByRole('link', { name: /Abrir origem Pagamento vencido/ }).click()
    await expect(page).toHaveURL(/\/pagamentos/)
    await signInAdmin(page)

    await openAdminModule(page, '/auditoria', 'Auditoria')
    await page.getByRole('searchbox', { name: 'Buscar auditoria' }).fill('property')
    await selectByLabel(page, 'Categoria', 'Mutation')
    await fillByLabel(page, 'Ator', 'Paulo')
    await clickRowAction(page, /Ver Imovel criado/)
    await expect(page.getByText('Campos alterados')).toBeVisible()
    await expect(page.getByText('trace-a')).toBeVisible()

    await openAdminModule(page, '/imoveis', 'Imoveis')
    await expect(page.getByRole('table')).toContainText('Casa Calabria')
    const globalSearch = page.getByRole('combobox', { name: 'Buscar' })
    await globalSearch.fill('Casa')
    await expect(page.getByRole('option', { name: /^Casa Calabria/ })).toBeVisible()
    await expect(page.getByRole('option', { name: /^Loft Moradas Prime/ })).not.toBeVisible()

    await switchActiveOrganization(page, 'org-moradas')
    await expect(page.getByRole('table')).toContainText('Loft Moradas Prime')
    await expect(page.getByRole('table')).not.toContainText('Casa Calabria')
    await globalSearch.fill('Loft')
    await expect(page.getByRole('option', { name: /^Loft Moradas Prime/ })).toBeVisible()
    await expect(page.getByRole('option', { name: /^Casa Calabria/ })).not.toBeVisible()
    await globalSearch.fill('Casa')
    await expect(page.getByText('Nenhum resultado permitido encontrado')).toBeVisible()

    const forbiddenDownloadStatus = await page.evaluate(async () => {
      const [{ ApiClient }, documents, activeStore, authStore, preferencesStore] =
        await Promise.all([
          import('/src/lib/api/client.ts'),
          import('/src/lib/api/documents.ts'),
          import('/src/stores/useActiveOrganizationStore.ts'),
          import('/src/stores/useAuthSessionStore.ts'),
          import('/src/stores/useAppPreferencesStore.ts'),
        ])
      const client = new ApiClient({
        fetchImpl: window.fetch.bind(window),
        getAccessToken: () => authStore.useAuthSessionStore.getState().accessToken,
        getLocale: () => preferencesStore.useAppPreferencesStore.getState().locale,
        getOrganizationId: () =>
          activeStore.useActiveOrganizationStore.getState().activeOrganization?.id,
      })

      try {
        await documents.downloadDocument(client, 'document-contract')
        return 200
      } catch (error) {
        const status = (error as { status?: unknown }).status

        return typeof status === 'number' ? status : 0
      }
    })
    expect(forbiddenDownloadStatus).toBe(403)

    await openAdminModule(page, '/dashboard', 'Dashboard')
    await expect(page.getByLabel(/Organiza..o ativa/)).toHaveValue('org-moradas')
    await expect(page.getByText('Atividade recente')).toBeVisible()
    await openAdminModule(page, '/auditoria', 'Auditoria')
    await expect(page.getByRole('table')).toContainText('Loft Moradas Prime')
    await openAdminModule(page, '/notificacoes', 'Notificacoes')
    await expect(page.getByRole('table')).toContainText('Contrato atualizado')
  })
})

test('blocks audit access when the active organization lacks audit permission', async ({
  page,
}) => {
  const state = await installMockApi(page)
  const permissionsWithoutAudit = [
    'dashboard.read',
    'properties.read',
    'contracts.read',
    'residents.read',
    'payments.read',
    'notifications.read',
  ]
  state.adminPermissions = permissionsWithoutAudit
  for (const organization of state.organizations) {
    organization.permissionCodes = permissionsWithoutAudit
  }

  await signInAdmin(page)
  await openAdminModule(page, '/auditoria', /Acesso n.o autorizado|Access not authorized/)
  await expect(page.getByText(/Acesso n.o autorizado|Access not authorized/)).toBeVisible()
})
