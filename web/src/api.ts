export type ItemType = 'Feature' | 'Service' | 'Tool'
export type ItemStatus = 'Active' | 'InDevelopment' | 'Deprecated'

export interface CatalogItem {
  id: number
  name: string
  type: ItemType
  status: ItemStatus
  description: string
  ownerTeam: string
  contact: string
  repoUrl: string | null
  docsUrl: string | null
  tags: string[]
  createdAt: string
  updatedAt: string
}

export type CatalogItemInput = Omit<CatalogItem, 'id' | 'createdAt' | 'updatedAt'>

export interface CountBy { key: string; count: number }

export interface CatalogReport {
  total: number
  byStatus: CountBy[]
  byType: CountBy[]
  byTeam: CountBy[]
  recentlyUpdated: CatalogItem[]
  lastUpdatedAt: string | null
  generatedAt: string
}

export interface ItemChange { action: 'created' | 'updated' | 'deleted'; id: number; item: CatalogItem | null }

export const TYPE_LABEL: Record<ItemType, string> = { Feature: 'Funcionalidad', Service: 'Servicio', Tool: 'Herramienta' }
export const STATUS_LABEL: Record<ItemStatus, string> = { Active: 'Activo', InDevelopment: 'En desarrollo', Deprecated: 'Deprecado' }

export const API_BASE = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/$/, '')

/** Thrown for non-2xx responses; `fields` holds ASP.NET validation errors when present. */
export class ApiError extends Error {
  constructor(message: string, public status: number, public fields: Record<string, string[]> = {}) {
    super(message)
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(API_BASE + path, {
    ...init,
    headers: { 'Content-Type': 'application/json', ...init?.headers },
  })
  if (!res.ok) {
    const body = await res.json().catch(() => null)
    throw new ApiError(body?.title ?? `Error ${res.status}`, res.status, body?.errors ?? {})
  }
  return res.status === 204 ? (undefined as T) : res.json()
}

export interface ItemFilters { search?: string; type?: ItemType | ''; status?: ItemStatus | '' }

export const api = {
  listItems: (f: ItemFilters = {}) => {
    const p = new URLSearchParams()
    if (f.search?.trim()) p.set('search', f.search.trim())
    if (f.type) p.set('type', f.type)
    if (f.status) p.set('status', f.status)
    const qs = p.toString()
    return request<CatalogItem[]>(`/api/items${qs ? `?${qs}` : ''}`)
  },
  createItem: (input: CatalogItemInput) =>
    request<CatalogItem>('/api/items', { method: 'POST', body: JSON.stringify(input) }),
  updateItem: (id: number, input: CatalogItemInput) =>
    request<CatalogItem>(`/api/items/${id}`, { method: 'PUT', body: JSON.stringify(input) }),
  deleteItem: (id: number) => request<void>(`/api/items/${id}`, { method: 'DELETE' }),
  report: () => request<CatalogReport>('/api/report'),
}
