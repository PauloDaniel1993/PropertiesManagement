import { normalizeAppLocale, type AppLocale } from '../../i18n'
import type { AdministratorStatus } from '../../lib/api/administrators'

const administratorCopies = {
  'en-US': {
    form: {
      cancel: 'Cancel',
      displayName: 'Display name',
      displayNameRequired: 'Enter the administrator name.',
      email: 'E-mail',
      emailInvalid: 'Enter a valid e-mail.',
      modeTitle: {
        create: 'Invite administrator',
        edit: 'Edit administrator',
      },
      roleRequired: 'Select at least one role.',
      roles: 'Roles',
      save: 'Save administrator',
      submitCreate: 'Send invite',
      submitEdit: 'Save changes',
      temporaryPassword: 'Temporary password',
      temporaryPasswordHint: 'Optional. Leave blank to let the invitation flow set access.',
      temporaryPasswordMin: 'Use at least 8 characters.',
      validationTitle: 'Review the highlighted fields',
    },
    list: {
      activeFilters: (count: number) => `${count} active filters`,
      allRoles: 'All roles',
      allStatuses: 'All statuses',
      archive: 'Archive',
      clearFilters: 'Clear filters',
      columns: {
        account: 'Account',
        lastAccess: 'Last access',
        roles: 'Roles',
        status: 'Status',
      },
      deactivate: 'Deactivate',
      edit: 'Edit',
      empty: 'No administrators found.',
      error: 'Could not load administrators.',
      filters: 'Filters',
      invite: 'Invite administrator',
      loading: 'Loading administrators...',
      pageDescription:
        'Manage administrative users, invitations, role assignments, and account status.',
      pageTitle: 'Administrators',
      reactivate: 'Reactivate',
      rowActions: 'Administrator actions',
      search: 'Search administrators',
      searchPlaceholder: 'Search by name or e-mail',
      status: {
        active: 'Active',
        archived: 'Archived',
        inactive: 'Inactive',
        invited: 'Invited',
        locked: 'Locked',
      } satisfies Record<AdministratorStatus, string>,
    },
    pagination: {
      next: 'Next',
      page: (page: number, totalPages: number) => `Page ${page} of ${totalPages}`,
      previous: 'Previous',
      range: (first: number, last: number, total: number) => `Showing ${first}-${last} of ${total}`,
    },
  },
  'pt-BR': {
    form: {
      cancel: 'Cancelar',
      displayName: 'Nome de exibição',
      displayNameRequired: 'Informe o nome do administrador.',
      email: 'E-mail',
      emailInvalid: 'Informe um e-mail válido.',
      modeTitle: {
        create: 'Convidar administrador',
        edit: 'Editar administrador',
      },
      roleRequired: 'Selecione ao menos um perfil.',
      roles: 'Perfis',
      save: 'Salvar administrador',
      submitCreate: 'Enviar convite',
      submitEdit: 'Salvar alterações',
      temporaryPassword: 'Senha temporária',
      temporaryPasswordHint: 'Opcional. Deixe em branco para o convite definir o acesso.',
      temporaryPasswordMin: 'Use pelo menos 8 caracteres.',
      validationTitle: 'Revise os campos destacados',
    },
    list: {
      activeFilters: (count: number) => `${count} filtros ativos`,
      allRoles: 'Todos os perfis',
      allStatuses: 'Todos os status',
      archive: 'Arquivar',
      clearFilters: 'Limpar filtros',
      columns: {
        account: 'Conta',
        lastAccess: 'Último acesso',
        roles: 'Perfis',
        status: 'Status',
      },
      deactivate: 'Desativar',
      edit: 'Editar',
      empty: 'Nenhum administrador encontrado.',
      error: 'Não foi possível carregar os administradores.',
      filters: 'Filtros',
      invite: 'Convidar administrador',
      loading: 'Carregando administradores...',
      pageDescription:
        'Gerencie usuários administrativos, convites, perfis de acesso e status da conta.',
      pageTitle: 'Administradores',
      reactivate: 'Reativar',
      rowActions: 'Ações do administrador',
      search: 'Buscar administradores',
      searchPlaceholder: 'Busque por nome ou e-mail',
      status: {
        active: 'Ativo',
        archived: 'Arquivado',
        inactive: 'Inativo',
        invited: 'Convidado',
        locked: 'Bloqueado',
      } satisfies Record<AdministratorStatus, string>,
    },
    pagination: {
      next: 'Próxima',
      page: (page: number, totalPages: number) => `Página ${page} de ${totalPages}`,
      previous: 'Anterior',
      range: (first: number, last: number, total: number) =>
        `Exibindo ${first}-${last} de ${total}`,
    },
  },
} as const

export function getAdministratorCopy(locale: AppLocale) {
  return administratorCopies[normalizeAppLocale(locale)]
}
