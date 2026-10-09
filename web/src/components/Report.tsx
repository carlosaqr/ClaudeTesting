import { STATUS_LABEL, TYPE_LABEL, type CatalogReport, type ItemStatus, type ItemType } from '../api'

import { formatDate as fmt } from '../format'

function Bars({ rows, label }: { rows: { key: string; count: number }[]; label: (k: string) => string }) {
  const max = Math.max(1, ...rows.map(r => r.count))
  return (
    <ul className="bars">
      {rows.map(r => (
        <li key={r.key}>
          <span className="bar-label">{label(r.key)}</span>
          <span className="bar-track"><span className="bar-fill" style={{ width: `${(r.count / max) * 100}%` }} /></span>
          <span className="bar-value">{r.count}</span>
        </li>
      ))}
    </ul>
  )
}

export function Report({ report, flashIds }: { report: CatalogReport; flashIds: Set<number> }) {
  const status = (s: ItemStatus) => report.byStatus.find(x => x.key === s)?.count ?? 0
  return (
    <section className="report">
      <div className="tiles">
        <div className="tile"><span className="tile-value">{report.total}</span><span className="tile-label">Registros</span></div>
        <div className="tile s-Active"><span className="tile-value">{status('Active')}</span><span className="tile-label">Activos</span></div>
        <div className="tile s-InDevelopment"><span className="tile-value">{status('InDevelopment')}</span><span className="tile-label">En desarrollo</span></div>
        <div className="tile s-Deprecated"><span className="tile-value">{status('Deprecated')}</span><span className="tile-label">Deprecados</span></div>
      </div>

      <div className="panels">
        <div className="panel">
          <h3>Por tipo</h3>
          <Bars rows={report.byType} label={k => TYPE_LABEL[k as ItemType] ?? k} />
        </div>
        <div className="panel">
          <h3>Por equipo</h3>
          {report.byTeam.length ? <Bars rows={report.byTeam} label={k => k} /> : <p className="muted">Sin registros.</p>}
        </div>
        <div className="panel wide">
          <h3>Últimos cambios</h3>
          {report.recentlyUpdated.length ? (
            <ul className="recent">
              {report.recentlyUpdated.map(i => (
                <li key={i.id} className={flashIds.has(i.id) ? 'flash' : ''}>
                  <span><strong>{i.name}</strong> <span className={`pill s-${i.status}`}>{STATUS_LABEL[i.status]}</span></span>
                  <span className="muted">{fmt(i.updatedAt)}</span>
                </li>
              ))}
            </ul>
          ) : <p className="muted">Sin registros.</p>}
        </div>
      </div>
      <p className="muted small">Último cambio: {fmt(report.lastUpdatedAt)} · Reporte generado: {fmt(report.generatedAt)}</p>
    </section>
  )
}
