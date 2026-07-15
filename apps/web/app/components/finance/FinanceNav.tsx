import Link from"next/link";
export function FinanceNav(){return <nav className="flex flex-wrap gap-2">{[["/customer-receivables","Müşteri Alacakları"],["/customer-collections","Tahsilatlar"],["/customer-ledger","Cari Hesap"]].map(x=><Link key={x[0]} href={x[0]} className="rounded-xl border border-white/10 bg-white/5 px-4 py-2 font-bold hover:bg-white/10">{x[1]}</Link>)}</nav>}
