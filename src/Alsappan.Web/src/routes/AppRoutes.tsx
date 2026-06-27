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
} from './guards'
import { AdminShell } from '../shell/AdminShell'
import { getModulePageComponent } from './modulePageRouteRegistry'

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

export function AppRoutes() {
  return (
    <AuthSessionBootstrap>
      <Routes>
        <Route element={<IdentityLoginPage />} path="/login" />
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
