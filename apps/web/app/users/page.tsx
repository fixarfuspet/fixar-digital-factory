"use client";import{safeResponseJson}from"../lib/api/client";

import { useEffect, useState } from "react";

type User = { id: string; email: string; firstName: string; lastName: string; isActive: boolean; roles: string[] };
type ApiResponse<T> = { data: T; message?: string };

export default function UsersPage() {
  const [users, setUsers] = useState<User[]>([]);
  const [roles, setRoles] = useState<string[]>([]);
  const [message, setMessage] = useState("");

  async function load() {
    const [userResponse, roleResponse] = await Promise.all([fetch("/api/backend/api/v1/users"), fetch("/api/backend/api/v1/users/roles")]);
    if (userResponse.status === 403) { setMessage("Bu ekran için kullanıcı yönetimi yetkiniz bulunmuyor."); return; }
    if (userResponse.status === 401) { window.location.href = "/"; return; }
    setUsers(((await safeResponseJson(userResponse)) as ApiResponse<User[]>).data ?? []);
    setRoles(((await safeResponseJson(roleResponse)) as ApiResponse<string[]>).data ?? []);
  }

  useEffect(() => { void load(); }, []);

  async function save(user: User) {
    const response = await fetch(`/api/backend/api/v1/users/${user.id}/access`, {
      method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ isActive: user.isActive, roles: user.roles }),
    });
    setMessage(response.ok ? "Kullanıcı erişimi güncellendi." : response.status === 403 ? "Bu işlem için yetkiniz bulunmuyor." : "Güncelleme başarısız.");
  }

  return <main className="min-h-screen bg-zinc-950 p-6 text-white"><div className="mx-auto max-w-6xl space-y-5"><h1 className="text-3xl font-black">Kullanıcı ve Rol Yönetimi</h1>{message && <p className="rounded-xl bg-white/10 p-3">{message}</p>}<div className="space-y-3">{users.map((user, index) => <section key={user.id} className="rounded-2xl border border-white/10 bg-white/5 p-4"><div className="flex flex-wrap items-center gap-4"><div className="min-w-64"><b>{user.firstName} {user.lastName}</b><p className="text-sm text-zinc-400">{user.email}</p></div><label><input type="checkbox" checked={user.isActive} onChange={event => setUsers(current => current.map((item, i) => i === index ? { ...item, isActive: event.target.checked } : item))}/> Aktif</label><select multiple className="min-h-24 rounded-xl bg-zinc-900 p-2" value={user.roles} onChange={event => { const selected = Array.from(event.target.selectedOptions, option => option.value); setUsers(current => current.map((item, i) => i === index ? { ...item, roles: selected } : item)); }}>{roles.map(role => <option key={role}>{role}</option>)}</select><button className="rounded-xl bg-amber-400 px-4 py-2 font-bold text-black" onClick={() => void save(user)}>Kaydet</button></div></section>)}</div></div></main>;
}
