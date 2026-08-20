import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuthRoot } from '@/auth/AuthRoot';
import { TokenSync } from '@/auth/TokenSync';
import { ProtectedRoute } from '@/auth/ProtectedRoute';
import { ToastProvider } from '@/components/ui/Toast';
import { AppShell } from '@/components/layout/AppShell';
import { Landing } from '@/pages/Landing';
import { Register } from '@/pages/Register';
import { AuthCallback } from '@/pages/AuthCallback';
import { Dashboard } from '@/pages/Dashboard';
import { AccountDetail } from '@/pages/AccountDetail';
import { Transfer } from '@/pages/Transfer';
import { Cards } from '@/pages/Cards';
import { Statements } from '@/pages/Statements';
import { StatementDetail } from '@/pages/StatementDetail';
import { Profile } from '@/pages/Profile';
import { NotFound } from '@/pages/NotFound';

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: 1, staleTime: 30_000 } },
});

export default function App() {
  return (
    <BrowserRouter>
      <AuthRoot>
        <TokenSync />
        <QueryClientProvider client={queryClient}>
          <ToastProvider>
            <Routes>
              <Route path="/" element={<Landing />} />
              <Route path="/register" element={<Register />} />
              <Route path="/callback" element={<AuthCallback />} />

              <Route element={<ProtectedRoute />}>
                <Route element={<AppShell />}>
                  <Route path="/dashboard" element={<Dashboard />} />
                  <Route path="/accounts/:id" element={<AccountDetail />} />
                  <Route path="/transfer" element={<Transfer />} />
                  <Route path="/cards" element={<Cards />} />
                  <Route path="/statements" element={<Statements />} />
                  <Route path="/statements/:id" element={<StatementDetail />} />
                  <Route path="/profile" element={<Profile />} />
                </Route>
              </Route>

              <Route path="*" element={<NotFound />} />
            </Routes>
          </ToastProvider>
        </QueryClientProvider>
      </AuthRoot>
    </BrowserRouter>
  );
}
