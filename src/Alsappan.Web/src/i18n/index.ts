import i18next from 'i18next'
import { initReactI18next } from 'react-i18next'
import enUS from './locales/en-US.json'
import ptBR from './locales/pt-BR.json'

void i18next.use(initReactI18next).init({
  fallbackLng: 'pt-BR',
  interpolation: {
    escapeValue: false,
  },
  lng: 'pt-BR',
  resources: {
    'en-US': {
      translation: enUS,
    },
    'pt-BR': {
      translation: ptBR,
    },
  },
})

export { i18next }
