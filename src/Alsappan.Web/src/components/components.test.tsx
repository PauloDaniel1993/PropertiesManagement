import { useState } from 'react'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import {
  DataTable,
  Dialog,
  ErrorState,
  FormField,
  PageHeader,
  Pagination,
  RowActions,
  SearchInput,
  StatusBadge,
  Tabs,
  TextInput,
} from './index'

describe('shared frontend primitives', () => {
  it('renders a page header with a primary action', async () => {
    const user = userEvent.setup()
    const onCreate = vi.fn()

    render(
      <PageHeader
        description="Cadastre e acompanhe imóveis operacionais."
        primaryAction={{ label: 'Novo imóvel', onClick: onCreate }}
        title="Imóveis"
      />,
    )

    expect(screen.getByRole('heading', { name: 'Imóveis' })).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Novo imóvel' }))

    expect(onCreate).toHaveBeenCalledTimes(1)
  })

  it('supports search, table row actions, badges, and pagination callbacks', async () => {
    const user = userEvent.setup()
    const onClear = vi.fn()
    const onArchive = vi.fn()
    const onPageChange = vi.fn()

    const rows = [
      {
        id: 'property-1',
        name: 'Casa Jardim',
        status: 'Disponível',
      },
    ]

    render(
      <>
        <SearchInput label="Buscar imóveis" onClear={onClear} placeholder="Buscar" value="Casa" />
        <DataTable
          columns={[
            {
              cell: (row) => row.name,
              header: 'Nome',
              id: 'name',
            },
            {
              cell: (row) => <StatusBadge label={row.status} tone="success" />,
              header: 'Status',
              id: 'status',
            },
          ]}
          getRowKey={(row) => row.id}
          rowActions={() => (
            <RowActions
              actions={[
                {
                  id: 'archive',
                  label: 'Arquivar',
                  onSelect: onArchive,
                },
              ]}
            />
          )}
          rows={rows}
        />
        <Pagination onPageChange={onPageChange} page={1} pageSize={10} totalItems={25} />
      </>,
    )

    expect(screen.getByRole('cell', { name: 'Casa Jardim' })).toBeInTheDocument()
    expect(screen.getByText('Disponível')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Limpar busca' }))
    await user.click(screen.getByRole('button', { name: 'Ações da linha' }))
    await user.click(screen.getByRole('menuitem', { name: 'Arquivar' }))
    await user.click(screen.getByRole('button', { name: 'Próxima' }))

    expect(onClear).toHaveBeenCalledTimes(1)
    expect(onArchive).toHaveBeenCalledTimes(1)
    expect(onPageChange).toHaveBeenCalledWith(2)
  })

  it('wires form labels, descriptions, and validation messages to controls', () => {
    render(
      <FormField
        description="Nome público usado na listagem."
        error="Campo obrigatório."
        id="name"
        label="Nome"
        required
      >
        {({ describedBy, id, isInvalid, isRequired }) => (
          <TextInput
            aria-describedby={describedBy}
            id={id}
            isInvalid={isInvalid}
            required={isRequired}
          />
        )}
      </FormField>,
    )

    const input = screen.getByLabelText(/Nome/)

    expect(input).toHaveAttribute('aria-describedby', 'name-description name-error')
    expect(input).toHaveAttribute('aria-invalid', 'true')
    expect(screen.getByRole('alert')).toHaveTextContent('Campo obrigatório.')
  })

  it('opens and dismisses dialogs with accessible labeling', async () => {
    const user = userEvent.setup()

    function DialogHarness() {
      const [isOpen, setIsOpen] = useState(false)

      return (
        <>
          <button onClick={() => setIsOpen(true)} type="button">
            Abrir
          </button>
          <Dialog isOpen={isOpen} onOpenChange={setIsOpen} title="Editar imóvel">
            <button type="button">Salvar</button>
          </Dialog>
        </>
      )
    }

    render(<DialogHarness />)

    await user.click(screen.getByRole('button', { name: 'Abrir' }))

    expect(screen.getByRole('dialog', { name: 'Editar imóvel' })).toBeInTheDocument()

    await user.keyboard('{Escape}')

    expect(screen.queryByRole('dialog', { name: 'Editar imóvel' })).not.toBeInTheDocument()
  })

  it('switches tab panels and renders retryable error states', async () => {
    const user = userEvent.setup()
    const onRetry = vi.fn()

    render(
      <>
        <Tabs
          tabs={[
            {
              content: <p>Dados cadastrais</p>,
              id: 'details',
              label: 'Dados',
            },
            {
              content: <p>Contratos vinculados</p>,
              id: 'contracts',
              label: 'Contratos',
            },
          ]}
        />
        <ErrorState description="A API recusou a consulta." onRetry={onRetry} />
      </>,
    )

    expect(screen.getByText('Dados cadastrais')).toBeInTheDocument()

    await user.click(screen.getByRole('tab', { name: 'Contratos' }))
    await user.click(screen.getByRole('button', { name: 'Tentar novamente' }))

    expect(screen.getByText('Contratos vinculados')).toBeInTheDocument()
    expect(onRetry).toHaveBeenCalledTimes(1)
  })
})
