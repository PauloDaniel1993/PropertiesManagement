import { Navigate, Route, Routes } from 'react-router-dom'
import { defaultAuthenticatedRoute } from '../navigation/menuContract'
import { IdentityLoginPage, AuthSessionBootstrap } from '../features/identity'
import { ModulePlaceholderPage } from '../pages/ModulePlaceholderPage'
import { modulePageRoutes } from '../pages/modulePageRoutes'
import {
  RequireActiveOrganization,
  RequireAdminRoute,
  RequireAuthenticated,
  RequirePermission,
  RequireResidentRoute,
} from './guards'
import { AdminShell } from '../shell/AdminShell'
import { getModulePageComponent } from './modulePageRouteRegistry'
import {
  ResidentPortalContractsPage,
  ResidentPortalDocumentsPage,
  ResidentPortalInspectionsPage,
  ResidentPortalNotificationsPage,
  ResidentPortalOccurrencesPage,
  ResidentPortalOverviewPage,
  ResidentPortalPaymentsPage,
  ResidentPortalProfilePage,
  ResidentPortalPropertyPage,
  ResidentPortalShell,
} from '../features/residentPortal'

function ProtectedRoute() {
  return (
    <RequireAuthenticated>
      <RequireAdminRoute>
        <RequireActiveOrganization>
          <AdminShell />
        </RequireActiveOrganization>
      </RequireAdminRoute>
    </RequireAuthenticated>
  )
}

function ResidentPortalRoute() {
  return (
    <RequireAuthenticated redirectTo="/resident-login">
      <RequireResidentRoute>
        <RequireActiveOrganization>
          <ResidentPortalShell />
        </RequireActiveOrganization>
      </RequireResidentRoute>
    </RequireAuthenticated>
  )
}

export function AppRoutes() {
  return (
    <AuthSessionBootstrap>
      <Routes>
        <Route element={<IdentityLoginPage />} path="/login" />
        <Route
          element={<IdentityLoginPage accountType="resident" defaultRedirectPath="/portal" />}
          path="/resident-login"
        />
        <Route element={<ResidentPortalRoute />} path="/portal/*">
          <Route index element={<ResidentPortalOverviewPage />} />
          <Route element={<ResidentPortalProfilePage />} path="perfil" />
          <Route element={<ResidentPortalPropertyPage />} path="imovel" />
          <Route element={<ResidentPortalContractsPage />} path="contratos" />
          <Route element={<ResidentPortalPaymentsPage />} path="pagamentos" />
          <Route element={<ResidentPortalDocumentsPage />} path="documentos" />
          <Route element={<ResidentPortalOccurrencesPage />} path="ocorrencias" />
          <Route element={<ResidentPortalInspectionsPage />} path="vistorias" />
          <Route element={<ResidentPortalNotificationsPage />} path="notificacoes" />
          <Route element={<Navigate replace to="/portal" />} path="*" />
        </Route>
        <Route element={<ProtectedRoute />}>
          <Route index element={<Navigate replace to={defaultAuthenticatedRoute} />} />
          {modulePageRoutes.map((route) => {
            const ModulePage = getModulePageComponent(route.item.id)

            return (
              <Route
                element={
                  <RequirePermission permissions={[route.item.requiredPermission]}>
                    {ModulePage ? <ModulePage /> : <ModulePlaceholderPage item={route.item} />}
                  </RequirePermission>
                }
                key={route.item.id}
                path={route.pathSegment}
              />
            )
          })}
        </Route>
        <Route element={<Navigate replace to={defaultAuthenticatedRoute} />} path="*" />
      </Routes>
    </AuthSessionBootstrap>
  )
}
