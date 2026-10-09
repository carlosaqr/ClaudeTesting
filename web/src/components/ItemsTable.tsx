import { STATUS_LABEL, TYPE_LABEL, type CatalogItem } from '../api'
import { formatDate } from '../format'

interface Props {
  items: CatalogItem[]
  flashIds: Set<number>
  onEdit: (item: CatalogItem) => void
  onDelete: (item: CatalogItem) => void
}

export function ItemsTable({ items, flashIds, onEdit, onDelete }: Props) {
  if (!items.length) return <p className="empty">No hay registros con esos filtros.</p>
  return (
    <div className="table-wrap">
      <table>
        <thead>
          <tr><th>Nombre</th><th>Tipo</th><th>Estado</th><th>Equipo</th><th>Contacto</th><th>Actualizado</th><th aria-label="Acciones" /></tr>
        </thead>
        <tbody>
          {items.map(i => (
            <tr key={i.id} className={flashIds.has(i.id) ? 'flash' : ''}>
              <td>
                <strong>{i.name}</strong>
                <div className="muted small">{i.description}</div>
                {i.tags.length > 0 && <div className="tags">{i.tags.map(t => <span key={t} className="tag">{t}</span>)}</div>}
                <div className="links">
                  {i.repoUrl && <a href={i.repoUrl} target="_blank" rel="noreferrer">Repo ↗</a>}
                  {i.docsUrl && <a href={i.docsUrl} target="_blank" rel="noreferrer">Docs ↗</a>}
                </div>
              </td>
              <td>{TYPE_LABEL[i.type]}</td>
              <td><span className={`pill s-${i.status}`}>{STATUS_LABEL[i.status]}</span></td>
              <td>{i.ownerTeam}</td>
              <td>{i.contact}</td>
              <td className="nowrap">{formatDate(i.updatedAt)}</td>
              <td className="nowrap">
                <button className="ghost small" onClick={() => onEdit(i)}>Editar</button>
                <button className="danger small" onClick={() => onDelete(i)}>Borrar</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
