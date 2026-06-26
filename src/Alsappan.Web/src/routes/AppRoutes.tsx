import { Navigate, Route, Routes, useLocation } from 'react-router-dom'
import { useEffect } from 'react'
import { defaultAuthenticatedRoute } from '../navigation/menuContract'
import { LoginPage } from '../pages/LoginPage'
import { ModulePlaceholderPage } from '../pages/ModulePlaceholderPage'
import { modulePageRoutes } from '../pages/modulePageRoutes'
import { AdminShell } from '../shell/AdminShell'
import { useAuthSessionStore } from '../stores/useAuthSessionStore'

function ProtectedRoute() {
  const location = useLocation()
  const isAuthenticated = useAuthSessionStore((state) => state.isAuthenticated)
  const setIntendedPath = useAuthSessionStore((state) => state.setIntendedPath)

  useEffect(() => {
    if (!isAuthenticated) {
      setIntendedPath(`${location.pathname}${location.search}`)
    }
  }, [isAuthenticated, location.pathname, location.search, setIntendedPath])

  if (!isAuthenticated) {
    return <Navigate replace to="/login" />
  }

  return <AdminShell />
}

export function AppRoutes() {
  return (
    <Routes>
      <Route element={<LoginPage />} path="/login" />
      <Route element={<ProtectedRoute />}>
        <Route index element={<Navigate replace to={defaultAuthenticatedRoute} />} />
        {modulePageRoutes.map((route) => (
          <Route
            element={<ModulePlaceholderPage item={route.item} />}
            key={route.item.id}
            path={route.pathSegment}
          />
        ))}
      </Route>
      <Route element={<Navigate replace to={defaultAuthenticatedRoute} />} path="*" />
    </Routes>
  )
}
