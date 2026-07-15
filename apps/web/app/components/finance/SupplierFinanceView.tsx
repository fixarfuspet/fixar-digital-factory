"use client";
import Link from "next/link";
import { useEffect, useState } from "react";
const nav=[["/supplier-payables","Tedarikçi Borçları"],["/supplier-payments","Tedarikçi Ödemeleri"],["/supplier-ledger","Tedarikçi Cari"],["/cheque-endorsements","Çek Ciroları"]];
export function SupplierFinanceView({title,endpoint}:{title:string;endpoint:string}){
 const[rows,setRows]=useState<Record<string,unknown>[]>([]),[message,setMessage]=useState("");
 useEffect(()=>{fetch(`/api/backend/v1/${endpoint}`).then(async r=>{if(r.status===403){setMessage("Bu işlem için tedarikçi finans yetkiniz bulunmuyor.");return}const j=await r.json();setRows(j.data??[])}).catch(()=>setMessage("Veriler alınamadı."))},[endpoint]);
 const keys=rows.length?Object.keys(rows[0]).filter(k=>!["id","supplierId","customerChequeId","supplierPaymentId"].includes(k)).slice(0,12):[];
 return <main className="min-h-screen bg-zinc-950 p-6 text-white"><div className="mx-auto max-w-7xl space-y-5"><h1 className="text-3xl font-black">{title}</h1><nav className="flex flex-wrap gap-2">{nav.map(x=><Link className="rounded-xl border border-white/10 bg-white/5 px-4 py-2" href={x[0]} key={x[0]}>{x[1]}</Link>)}</nav>{message&&<p className="rounded-xl bg-red-500/10 p-3">{message}</p>}<div className="overflow-auto rounded-2xl border border-white/10 bg-white/5 p-4"><table className="w-full min-w-[900px] text-left text-sm"><thead><tr>{keys.map(k=><th className="p-2" key={k}>{label(k)}</th>)}</tr></thead><tbody>{rows.map((r,i)=><tr className="border-t border-white/10" key={String(r.id??i)}>{keys.map(k=><td className="p-2" key={k}>{value(r[k])}</td>)}</tr>)}</tbody></table>{!rows.length&&!message&&<p className="p-4 text-zinc-400">Kayıt bulunamadı.</p>}</div></div></main>;
}
function value(v:unknown){if(v==null)return"-";if(typeof v==="boolean")return v?"Evet":"Hayır";if(typeof v==="string"&&/^\d{4}-\d{2}-\d{2}T/.test(v))return new Date(v).toLocaleDateString("tr-TR");return String(v)}
function label(x:string){return x.replace(/([A-Z])/g," $1").replace(/^./,c=>c.toUpperCase())}
