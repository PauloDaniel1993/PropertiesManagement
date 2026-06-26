import { render, screen } from '@testing-library/react'
import { App } from './App'
import { AppProviders } from './app/AppProviders'

describe('App foundation', () => {
  it('renders the default Portuguese product foundation', async () => {
    render(
      <AppProviders>
        <App />
      </AppProviders>,
    )

    expect(
      await screen.findByRole('heading', {
        name: /Alsappan Gestao de Imoveis/i,
      }),
    ).toBeInTheDocument()
  })
})
