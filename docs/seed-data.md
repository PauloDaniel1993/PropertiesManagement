# Dados de seed

Os seeds iniciais mantem o ambiente local utilizavel e preservam os limites de organizacao. Todos os dados abaixo sao apenas para desenvolvimento, testes e demonstracoes locais.

## Organizacao

- Slug: `alsappan`
- Nome: `Alsappan`
- Localidade padrao: `pt-BR`
- Moeda padrao: `BRL`

## Credenciais locais

Use somente em ambientes locais:

- Administrador: `admin@alsappan.local` / `alsappan`
- Operador: `operador@alsappan.local` / `alsappan`
- Moradora: `ana.moradora@alsappan.local` / `alsappan`

## Papeis

- `Administrador`: acesso administrativo completo.
- `Gestor`: gestao operacional com acesso ampliado.
- `Operador`: operacao diaria dos modulos principais.
- `Leitura`: acesso somente leitura aos modulos permitidos.
- `Morador`: acesso ao portal do morador para registros vinculados.

## Catalogos

- Status de imoveis: disponivel, reservado, alugado, manutencao, inativo e arquivado.
- Tipos de imoveis: apartamento, casa, sala comercial, terreno e outros.
- Status de contratos: rascunho, ativo, encerrando, encerrado, rescindido, cancelado e arquivado.
- Status de pagamentos: pendente, em atraso, parcialmente pago, pago, cancelado, em disputa e arquivado.
- Tipos de contas de consumo: energia, agua, gas, internet, condominio, IPTU, seguro e outros.
- Prioridades de ocorrencia: baixa, media, alta e urgente.
- Status de ocorrencia: aberta, atribuida, em andamento, aguardando, resolvida, cancelada e arquivada.
- Status de vistoria: agendada, em andamento, concluida, cancelada e arquivada.

## Cenario de demonstracao

O contributor `tenant-demo-data@2026.06.27` cria um cenario conectado na organizacao `Alsappan`:

- 2 imoveis: um apartamento alugado e uma casa em manutencao.
- 2 moradores, incluindo uma moradora com conta de portal ativa.
- 1 contrato ativo com moradores, aluguel, caucao e reajuste.
- 2 cobrancas de aluguel, incluindo uma quitada via Pix mock e uma pendente com boleto mock.
- 2 contas de consumo, com conta de energia aberta e taxa de condominio quitada.
- 6 documentos ligados a contrato, pagamento, conta de consumo, pet, ocorrencia e vistoria.
- 1 pet autorizado com documento de vacinacao.
- 1 veiculo autorizado com vaga de garagem.
- 1 ocorrencia em andamento com comentario e anexo.
- 1 vistoria concluida com checklist, assinaturas e relatorio.
- Notificacoes, timeline e auditoria preenchidas com eventos de negocio e um evento de login.

Os registros usam identificadores deterministicos, entao o seed pode ser executado novamente sem duplicar o cenario.
