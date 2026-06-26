# Alsappan Domain Glossary

Code identifiers use English names. User-facing labels default to Brazilian Portuguese (`pt-BR`) and are translated through locale resources.

| UI term (`pt-BR`) | Code identifier | Route slug | Notes |
| --- | --- | --- | --- |
| Dashboard | Dashboard | `/dashboard` | Operational overview and shortcuts. |
| Imoveis | Properties | `/imoveis` | Rentable property units; UI label uses `Imóveis`. |
| Contratos | Contracts | `/contratos` | Lease agreements linking properties and residents. |
| Moradores | Residents | `/moradores` | Residents and tenants, including portal account links. |
| Administradores | Administrators | `/administradores` | Administrative users, roles, invitations, and status. |
| Pagamentos | Payments | `/pagamentos` | Charges, settlement transactions, receipts, and balances. |
| Contas de Consumo | UtilityAccounts | `/contas-de-consumo` | Utilities such as water, electricity, gas, internet, fees, and taxes. |
| Documentos | Documents | `/documentos` | Files, versions, metadata, and links to business records. |
| Pets | Pets | `/pets` | Pet registry linked to residents, properties, and contracts. |
| Veiculos | Vehicles | `/veiculos` | Vehicle and parking registry; UI label uses `Veículos`. |
| Ocorrencias | Occurrences | `/ocorrencias` | Incidents, maintenance, complaints, comments, and workflow. |
| Vistorias | Inspections | `/vistorias` | Scheduled inspections, checklists, photos, and report-ready data. |
| Notificacoes | Notifications | `/notificacoes` | In-app notification center and preferences. |
| Timeline | Timeline | `/timeline` | Meaningful business activity feed. |
| Auditoria | Audit | `/auditoria` | Immutable security and mutation audit records. |
| Configuracoes | Settings | `/configuracoes` | Organization, localization, catalogs, security, and notification settings. |

## Shared Terms

| UI term (`pt-BR`) | Code identifier | Notes |
| --- | --- | --- |
| Organizacao | Organization | Tenant boundary for business data. UI label uses `Organização`. |
| Organizacao ativa | ActiveOrganization | Selected tenant context for authenticated requests. |
| Morador | Resident | Portal user visibility is tied to resident records. |
| Permissao | Permission | Granular backend-enforced capability. UI label uses `Permissão`. |
| Papel | Role | Named permission bundle such as Administrador, Gestor, Operador, Leitura, and Morador. |
| Arquivado | Archived | Soft-deleted state for records kept for history. |
| Disponivel | Available | Property status; UI label uses `Disponível`. |
| Alugado | Rented | Property status. |
| Valor sugerido | SuggestedRent | Money value with amount and ISO currency code. |
| Garagem | Garage | Structured count plus optional space identifiers. |
| Criado em / por | CreatedAt / CreatedByUserId | Audit metadata on mutable records. |
| Atualizado em / por | UpdatedAt / UpdatedByUserId | Audit metadata on mutable records. |
| Excluido em / por | DeletedAt / DeletedByUserId | Soft-delete metadata. UI label uses `Excluído`. |
| Versao da linha | RowVersion | Optimistic concurrency token. UI label uses `Versão da linha`. |

## Naming Rules

- Backend namespaces and database-facing code use English domain names.
- API routes use stable ASCII slugs, including Portuguese slugs where the product route is already recognizable to users.
- Locale keys use English structural names such as `navigation.properties`, not translated terms.
- Canonical statuses and event names are stored as codes; display text is resolved by locale.
- Portuguese strings with accents appear only in user-facing resources, docs, tests, and seeded localized labels.
