import { useEffect, useRef, useState, type FormEvent } from 'react'
import { ApiError, STATUS_LABEL, TYPE_LABEL, type CatalogItem, type CatalogItemInput, type ItemStatus, type ItemType } from '../api'

const EMPTY: CatalogItemInput = {
  name: '', type: 'Feature', status: 'Active', description: '', ownerTeam: '', contact: '',
  repoUrl: null, docsUrl: null, tags: [],
}

interface Props {
  item: CatalogItem | null // null = new record
  onSave: (input: CatalogItemInput) => Promise<void>
  onClose: () => void
}

export function ItemForm({ item, onSave, onClose }: Props) {
  const dialog = useRef<HTMLDialogElement>(null)
  const [form, setForm] = useState<CatalogItemInput>(item ?? EMPTY)
  const [tags, setTags] = useState((item?.tags ?? []).join(', '))
  const [errors, setErrors] = useState<Record<string, string[]>>({})
  const [message, setMessage] = useState('')
  const [saving, setSaving] = useState(false)

  useEffect(() => { dialog.current?.showModal() }, [])

  const set = <K extends keyof CatalogItemInput>(k: K, v: CatalogItemInput[K]) => setForm(f => ({ ...f, [k]: v }))
  const fieldError = (k: string) => {
    const key = Object.keys(errors).find(e => e.toLowerCase() === k.toLowerCase())
    return key ? <span className="field-error">{errors[key].join(' ')}</span> : null
  }

  async function submit(e: FormEvent) {
    e.preventDefault()
    setSaving(true); setErrors({}); setMessage('')
    try {
      await onSave({
        ...form,
        repoUrl: form.repoUrl?.trim() || null,
        docsUrl: form.docsUrl?.trim() || null,
        tags: tags.split(',').map(t => t.trim()).filter(Boolean),
      })
      onClose()
    } catch (err) {
      if (err instanceof ApiError) { setErrors(err.fields); setMessage(err.status === 404 ? 'El registro ya no existe.' : err.message) }
      else setMessage('No se pudo conectar con la API.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <dialog ref={dialog} onClose={onClose} className="modal">
      <form onSubmit={submit}>
        <h2>{item ? 'Editar registro' : 'Nuevo registro'}</h2>
        <label>Nombre *<input value={form.name} onChange={e => set('name', e.target.value)} required minLength={2} maxLength={120} autoFocus />{fieldError('name')}</label>
        <div className="row">
          <label>Tipo *
            <select value={form.type} onChange={e => set('type', e.target.value as ItemType)}>
              {(Object.keys(TYPE_LABEL) as ItemType[]).map(t => <option key={t} value={t}>{TYPE_LABEL[t]}</option>)}
            </select>
          </label>
          <label>Estado *
            <select value={form.status} onChange={e => set('status', e.target.value as ItemStatus)}>
              {(Object.keys(STATUS_LABEL) as ItemStatus[]).map(s => <option key={s} value={s}>{STATUS_LABEL[s]}</option>)}
            </select>
          </label>
        </div>
        <label>Descripción *<textarea rows={3} value={form.description} onChange={e => set('description', e.target.value)} required minLength={5} maxLength={1000} />{fieldError('description')}</label>
        <div className="row">
          <label>Equipo / proyecto *<input value={form.ownerTeam} onChange={e => set('ownerTeam', e.target.value)} required minLength={2} />{fieldError('ownerTeam')}</label>
          <label>Contacto *<input value={form.contact} onChange={e => set('contact', e.target.value)} required minLength={2} placeholder="Persona o canal" />{fieldError('contact')}</label>
        </div>
        <div className="row">
          <label>Repo (URL)<input type="url" value={form.repoUrl ?? ''} onChange={e => set('repoUrl', e.target.value)} placeholder="https://" />{fieldError('repoUrl')}</label>
          <label>Docs (URL)<input type="url" value={form.docsUrl ?? ''} onChange={e => set('docsUrl', e.target.value)} placeholder="https://" />{fieldError('docsUrl')}</label>
        </div>
        <label>Tags (separados por coma)<input value={tags} onChange={e => setTags(e.target.value)} placeholder="python, auth, reportes" />{fieldError('tags')}</label>
        {message && <p className="form-error">{message}</p>}
        <div className="actions">
          <button type="button" className="ghost" onClick={() => dialog.current?.close()}>Cancelar</button>
          <button type="submit" disabled={saving}>{saving ? 'Guardando…' : 'Guardar'}</button>
        </div>
      </form>
    </dialog>
  )
}
