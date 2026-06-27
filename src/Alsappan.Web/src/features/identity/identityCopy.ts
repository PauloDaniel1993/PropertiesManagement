import { normalizeAppLocale, type AppLocale } from '../../i18n'
import type { AccountType } from '../../lib/api/identity'

const identityCopies = {
  'en-US': {
    bootstrap: {
      loading: 'Restoring session',
      loadingDescription: 'We are checking your credentials and active organization.',
    },
    guards: {
      adminOnlyDescription: 'This area is reserved for administrative users.',
      adminOnlyTitle: 'Administrative access required',
      forbiddenDescription: 'Your active organization does not grant access to this action.',
      forbiddenTitle: 'Access not authorized',
      missingOrganizationDescription:
        'Choose an active organization before opening tenant-scoped pages.',
      missingOrganizationTitle: 'Active organization required',
      residentOnlyDescription: 'This area is reserved for resident portal users.',
      residentOnlyTitle: 'Resident access required',
    },
    login: {
      accountType: {
        admin: 'Administrator',
        resident: 'Resident',
      } satisfies Record<AccountType, string>,
      brandName: 'Alsappan',
      email: 'E-mail',
      emailInvalid: 'Enter a valid e-mail.',
      genericError: 'Could not sign in. Check your credentials and try again.',
      organization: 'Organization',
      organizationPlaceholder: 'Continue without selecting',
      password: 'Password',
      passwordRequired: 'Enter your password.',
      required: 'This field is required.',
      submit: 'Sign in',
      subtitle:
        'Access your organization with a secure session, permissions, and active organization context.',
      title: 'Sign in',
    },
    organizationSwitcher: {
      error: 'Could not switch organization.',
      label: 'Active organization',
      singleOrganization: 'Only one organization available',
      switching: 'Switching organization',
    },
  },
  'pt-BR': {
    bootstrap: {
      loading: 'Restaurando sessão',
      loadingDescription: 'Estamos validando suas credenciais e a organização ativa.',
    },
    guards: {
      adminOnlyDescription: 'Esta área é reservada para usuários administrativos.',
      adminOnlyTitle: 'Acesso administrativo obrigatório',
      forbiddenDescription: 'Sua organização ativa não libera acesso a esta ação.',
      forbiddenTitle: 'Acesso não autorizado',
      missingOrganizationDescription:
        'Selecione uma organização ativa antes de abrir páginas com dados do cliente.',
      missingOrganizationTitle: 'Organização ativa obrigatória',
      residentOnlyDescription: 'Esta área é reservada para usuários do portal do morador.',
      residentOnlyTitle: 'Acesso de morador obrigatório',
    },
    login: {
      accountType: {
        admin: 'Administrador',
        resident: 'Morador',
      } satisfies Record<AccountType, string>,
      brandName: 'Alsappan',
      email: 'E-mail',
      emailInvalid: 'Informe um e-mail válido.',
      genericError: 'Não foi possível entrar. Confira seus dados e tente novamente.',
      organization: 'Organização',
      organizationPlaceholder: 'Continuar sem selecionar',
      password: 'Senha',
      passwordRequired: 'Informe sua senha.',
      required: 'Este campo é obrigatório.',
      submit: 'Entrar',
      subtitle:
        'Acesse sua organização com sessão segura, permissões e contexto ativo de atendimento.',
      title: 'Entrar',
    },
    organizationSwitcher: {
      error: 'Não foi possível trocar a organização.',
      label: 'Organização ativa',
      singleOrganization: 'Apenas uma organização disponível',
      switching: 'Trocando organização',
    },
  },
} as const

export function getIdentityCopy(locale: AppLocale) {
  return identityCopies[normalizeAppLocale(locale)]
}
