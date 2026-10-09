/** The API returns UTC timestamps; SQLite may drop the trailing "Z", so add it back before parsing. */
export const formatDate = (iso: string | null) =>
  iso ? new Date(/[zZ]|[+-]\d\d:\d\d$/.test(iso) ? iso : iso + 'Z').toLocaleString('es-AR', { dateStyle: 'short', timeStyle: 'medium' }) : '—'
