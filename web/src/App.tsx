import { useCallback, useEffect, useRef, useState } from 'react'
import { api, STATUS_LABEL, TYPE_LABEL, type CatalogItem, type CatalogItemInput, type CatalogReport, type ItemChange, type ItemFilters, type ItemStatus, type ItemType } from './api'
import { useCatalogHub, type HubStatus } from './useCatalogHub'
import { Report } from './components/Report'
import { ItemsTable } from './components/ItemsTable'
import { ItemForm } from './components/ItemForm'

const HUB_LABEL: Record<HubStatus, string> = {
  connecting: 'Conectando…', live: 'En vivo', reconnecting: 'Reconectando…', offline: 'Sin conexión',
}
const ACTION_LABEL = { created: 'agregado', updated: 'actualizado', deleted: 'borrado' } as const

export default function App() {
  const [tab, setTab] = useState<'report' | 'items'>('report')
  const [items, setItems] = useState<CatalogItem[]>([])
  const [report, setReport] = useState<CatalogReport | null>(null)
  const [filters, setFilters] = useState<ItemFilters>({ search: '', type: '', status: '' })
  const [editing, setEditing] = useState<CatalogItem | 'new' | null>(null)
  const [error, setError] = useState('')
  const [toast, setToast] = useState('')
  const [flashIds, setFlashIds] = useState<Set<number>>(new Set())
  const filtersRef = useRef(filters)
  filtersRef.current = filters

  const refresh = useCallback(async () => {
    try {
      const [list, rep] = await Promise.all([api.listItems(filtersRef.current), api.report()])
      setItems(list); setReport(rep); setError('')
    } catch {
      setError('No se pudo conectar con la API. ¿Está corriendo en http://localhost:5180?')
    }
  }, [])

  // Reload when filters change (search is debounced).
  useEffect(() => {
    const t = setTimeout(refresh, 250)
    return () => clearTimeout(t)
  }, [filters, refresh])

  const onChange = useCallback((change: ItemChange) => {
    refresh()
    const name = change.item?.name ?? `#${change.id}`
    setToast(`“${name}” ${ACTION_LABEL[change.action]}`)
    if (change.action !== 'deleted') {
      setFlashIds(s => new Set(s).add(change.id))
      setTimeout(() => setFlashIds(s => { const n = new Set(s); n.delete(change.id); return n }), 2500)
    }
  }, [refresh])

  const hub = useCatalogHub(onChange)

  // After a reconnect we may have missed events: reload everything.
  useEffect(() => { if (hub === 'live') refresh() }, [hub, refresh])

  useEffect(() => {
    if (!toast) return
    const t = setTimeout(() => setToast(''), 3500)
    return () => clearTimeout(t)
  }, [toast])

  async function save(input: CatalogItemInput) {
    if (editing === 'new') await api.createItem(input)
    else if (editing) await api.updateItem(editing.id, input)
    await refresh() // the hub event will also arrive; this keeps the UI correct if it's offline
  }

  async function remove(item: CatalogItem) {
    if (!confirm(`¿Borrar “${item.name}”? No se puede deshacer.`)) return
    try { await api.deleteItem(item.id) } catch { setError('No se pudo borrar el registro.') }
    await refresh()
  }

  return (
    <div className="app">
      <header>
        <div>
          <h1>Catálogo interno</h1>
          <p className="muted">Lo que el equipo ya construyó, con su estado en tiempo real.</p>
        </div>
        <span className={`live live-${hub}`} role="status"><span className="dot" />{HUB_LABEL[hub]}</span>
      </header>

      <nav className="tabs" role="tablist">
        <button role="tab" aria-selected={tab === 'report'} onClick={() => setTab('report')}>Reporte</button>
        <button role="tab" aria-selected={tab === 'items'} onClick={() => setTab('items')}>Registros{report ? ` (${report.total})` : ''}</button>
      </nav>

      {error && <p className="banner">{error}</p>}

      {tab === 'report' && (report ? <Report report={report} flashIds={flashIds} /> : !error && <p className="muted">Cargando…</p>)}

      {tab === 'items' && (
        <section>
          <div className="toolbar">
            <input type="search" placeholder="Buscar por nombre, descripción, equipo o tag…" value={filters.search}
              onChange={e => setFilters(f => ({ ...f, search: e.target.value }))} aria-label="Buscar" />
            <select value={filters.type} onChange={e => setFilters(f => ({ ...f, type: e.target.value as ItemType | '' }))} aria-label="Tipo">
              <option value="">Todos los tipos</option>
              {(Object.keys(TYPE_LABEL) as ItemType[]).map(t => <option key={t} value={t}>{TYPE_LABEL[t]}</option>)}
            </select>
            <select value={filters.status} onChange={e => setFilters(f => ({ ...f, status: e.target.value as ItemStatus | '' }))} aria-label="Estado">
              <option value="">Todos los estados</option>
              {(Object.keys(STATUS_LABEL) as ItemStatus[]).map(s => <option key={s} value={s}>{STATUS_LABEL[s]}</option>)}
            </select>
            <button onClick={() => setEditing('new')}>+ Nuevo</button>
          </div>
          <ItemsTable items={items} flashIds={flashIds} onEdit={setEditing} onDelete={remove} />
        </section>
      )}

      {editing && <ItemForm item={editing === 'new' ? null : editing} onSave={save} onClose={() => setEditing(null)} />}
      {toast && <div className="toast" role="status">{toast}</div>}
    </div>
  )
}
