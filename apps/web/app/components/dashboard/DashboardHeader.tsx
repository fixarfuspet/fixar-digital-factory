import { stockAlerts } from "../../lib/dashboard-data";
import type { SessionUser } from "../../lib/auth/session";
import { LogoutButton } from "../auth/LogoutButton";

function initialsFromEmail(email: string): string {
  const name = email.split("@")[0] ?? "";
  const parts = name.split(/[._-]+/).filter(Boolean);
  const initials = parts.length >= 2 ? `${parts[0][0]}${parts[1][0]}` : name.slice(0, 2);
  return initials.toUpperCase() || "FX";
}

export function DashboardHeader({ user }: { user: SessionUser }) {
  const primaryRole = user.roles[0] ?? "Guest";

  return (
    <div className="flex flex-col gap-4 border-b border-white/10 pb-6 sm:flex-row sm:items-center sm:justify-between">
      <div>
        <p className="text-xs font-semibold tracking-[0.3em] text-emerald-400">FIXAR OS</p>
        <h2 className="mt-1 text-2xl font-bold text-white sm:text-3xl">Executive Dashboard</h2>
        <p className="mt-1 text-sm text-zinc-500">
          Factory 1 · Production Line A ·{" "}
          <span className="text-zinc-400">Live session · widget data is still a static demo</span>
        </p>
      </div>

      <div className="flex items-center gap-3">
        <button
          type="button"
          className="relative flex h-10 w-10 items-center justify-center rounded-full border border-white/10 bg-white/5 text-zinc-300 transition hover:border-emerald-400/40 hover:text-emerald-300"
          aria-label="Notifications"
        >
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8">
            <path d="M15 17h5l-1.4-1.4A2 2 0 0 1 18 14.2V11a6 6 0 1 0-12 0v3.2a2 2 0 0 1-.6 1.4L4 17h5" />
            <path d="M9 17a3 3 0 0 0 6 0" />
          </svg>
          {stockAlerts.length > 0 ? (
            <span className="absolute -right-0.5 -top-0.5 flex h-4 w-4 items-center justify-center rounded-full bg-red-500 text-[10px] font-bold text-white">
              {stockAlerts.length}
            </span>
          ) : null}
        </button>

        <div className="flex items-center gap-2.5 rounded-full border border-white/10 bg-white/5 py-1.5 pl-1.5 pr-4">
          <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-emerald-400/20 text-xs font-bold text-emerald-300">
            {initialsFromEmail(user.email)}
          </span>
          <div className="min-w-0 leading-tight">
            <p className="truncate text-xs font-semibold text-white">{user.email}</p>
            <p className="text-[11px] text-zinc-500">{primaryRole}</p>
          </div>
        </div>

        <LogoutButton />
      </div>
    </div>
  );
}
