import { useAuth } from 'react-oidc-context';
import { Link, Navigate } from 'react-router-dom';
import { ArrowRight, Landmark, ShieldCheck, Zap, Layers } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { ThemeToggle } from '@/components/layout/ThemeToggle';

const features = [
  {
    icon: Layers,
    title: 'Event-driven microservices',
    description: 'Seven independent services behind a gateway, coordinated over gRPC and RabbitMQ.',
  },
  {
    icon: ShieldCheck,
    title: 'OAuth2 / OIDC everywhere',
    description: 'Keycloak-issued JWTs validated at the gateway and every downstream service.',
  },
  {
    icon: Zap,
    title: 'Real transactions, real ledger',
    description: 'Deposits, withdrawals, and transfers post to an append-only ledger via domain events.',
  },
];

export function Landing() {
  const auth = useAuth();

  if (auth.isAuthenticated) return <Navigate to="/dashboard" replace />;

  return (
    <div className="min-h-screen bg-(--color-bg)">
      <header className="mx-auto flex max-w-5xl items-center justify-between px-6 py-6">
        <div className="flex items-center gap-2">
          <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-(--color-brand) text-white">
            <Landmark size={16} />
          </div>
          <span className="text-sm font-semibold tracking-tight text-(--color-text)">Atlas Bank</span>
        </div>
        <div className="flex items-center gap-2">
          <ThemeToggle />
          <Button variant="secondary" size="sm" onClick={() => auth.signinRedirect()}>
            Sign in
          </Button>
        </div>
      </header>

      <main className="mx-auto max-w-3xl px-6 pb-24 pt-16 text-center sm:pt-24">
        <span className="inline-flex items-center rounded-full bg-(--color-brand-soft) px-3 py-1 text-xs font-medium text-(--color-brand)">
          Portfolio project · not a real bank
        </span>
        <h1 className="mt-6 text-4xl font-semibold tracking-tight text-(--color-text) sm:text-5xl">
          Banking infrastructure,
          <br />
          built the way real banks build it.
        </h1>
        <p className="mx-auto mt-5 max-w-xl text-base text-(--color-text-muted)">
          Open an account, move money, issue a card, and pull a statement - all backed by
          a .NET microservices platform with a real API gateway, event bus, and ledger.
        </p>
        <div className="mt-8 flex items-center justify-center gap-3">
          <Link to="/register">
            <Button size="lg">
              Open an account <ArrowRight size={16} />
            </Button>
          </Link>
          <Button variant="secondary" size="lg" onClick={() => auth.signinRedirect()}>
            Sign in
          </Button>
        </div>

        <div className="mt-20 grid gap-4 text-left sm:grid-cols-3">
          {features.map(({ icon: Icon, title, description }) => (
            <div
              key={title}
              className="rounded-xl border border-(--color-border) bg-(--color-surface) p-5 shadow-(--shadow-card)"
            >
              <Icon size={18} className="text-(--color-brand)" />
              <p className="mt-3 text-sm font-semibold text-(--color-text)">{title}</p>
              <p className="mt-1.5 text-sm text-(--color-text-muted)">{description}</p>
            </div>
          ))}
        </div>
      </main>
    </div>
  );
}
