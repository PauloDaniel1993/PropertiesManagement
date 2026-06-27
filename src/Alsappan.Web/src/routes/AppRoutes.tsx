import { Navigate, Route, Routes } from 'react-router-dom'
import { defaultAuthenticatedRoute } from '../navigation/menuContract'
import { IdentityLoginPage, AuthSessionBootstrap } from '../features/identity'
import { AdministratorsPage } from '../pages/AdministratorsPage'
import { ModulePlaceholderPage } from '../pages/ModulePlaceholderPage'
import { modulePageRoutes } from '../pages/modulePageRoutes'
import { RequireActiveOrganization, RequireAdminRoute, RequireAuthenticated } from './guards'
import { AdminShell } from '../shell/AdminShell'

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
          {modulePageRoutes.map((route) => (
            <Route
              element={
                route.item.id === 'administrators' ? (
                  <AdministratorsPage />
                ) : (
                  <ModulePlaceholderPage item={route.item} />
                )
              }
              key={route.item.id}
              path={route.pathSegment}
            />
          ))}
        </Route>
        <Route element={<Navigate replace to={defaultAuthenticatedRoute} />} path="*" />
      </Routes>
    </AuthSessionBootstrap>
  )
}
