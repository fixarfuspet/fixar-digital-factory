"use client";
import { useState } from "react";

type Props = {
  open: boolean;
  station: number | null;
  onClose: () => void;
};

export default function AssignmentModal({
  open,
  station,
  onClose,
}: Props) {
  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-sm">
      <div className="w-full max-w-2xl rounded-3xl border border-white/10 bg-[#0F1115] p-8 shadow-2xl">

        <div className="mb-8 flex items-center justify-between">
          <div>
            <p className="text-sm text-emerald-400 font-bold">
              FIXAR OS
            </p>

            <h2 className="mt-1 text-3xl font-black text-white">
              İstasyon {station} İş Atama
            </h2>
          </div>

          <button
            onClick={onClose}
            className="rounded-xl bg-zinc-800 px-4 py-2 text-white hover:bg-zinc-700"
          >
            Kapat
          </button>
        </div>

        <div className="grid gap-5">

          <Field label="Müşteri">
            <select className="w-full rounded-xl border border-white/10 bg-black/30 p-3 text-white">
              <option>Seçiniz...</option>
            </select>
          </Field>

          <Field label="Sipariş">
            <select className="w-full rounded-xl border border-white/10 bg-black/30 p-3 text-white">
              <option>Seçiniz...</option>
            </select>
          </Field>

          <Field label="Kalıp">
            <select className="w-full rounded-xl border border-white/10 bg-black/30 p-3 text-white">
              <option>Seçiniz...</option>
            </select>
          </Field>

          <Field label="Operatör">
            <select className="w-full rounded-xl border border-white/10 bg-black/30 p-3 text-white">
              <option>Mahmut</option>
              <option>Erdem</option>
              <option>Ramazan</option>
            </select>
          </Field>

        </div>

        <div className="mt-8 flex justify-end gap-4">
          <button
            onClick={onClose}
            className="rounded-xl bg-zinc-700 px-6 py-3 font-bold text-white"
          >
            İptal
          </button>

          <button
            className="rounded-xl bg-emerald-500 px-6 py-3 font-bold text-black hover:bg-emerald-400"
          >
            İşi Başlat
          </button>
        </div>

      </div>
    </div>
  );
}

function Field({
  label,
  children,
}: {
  label: string;
  children: React.ReactNode;
}) {
  return (
    <div>
      <label className="mb-2 block text-sm font-bold text-zinc-300">
        {label}
      </label>
      {children}
    </div>
  );
}