import { useApi, useLoad } from './api'

/** Bộ biểu tượng nét mảnh dùng chung (SVG, không cần thư viện). */
const ICONS = {
  calendar: 'M7 3v3M17 3v3M4 9h16M5 5h14a1 1 0 0 1 1 1v13a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1V6a1 1 0 0 1 1-1Zm4 9h2m4 0h2m-8 3h2',
  list: 'M9 6h11M9 12h11M9 18h11M4.5 6h.01M4.5 12h.01M4.5 18h.01',
  inbox: 'M4 13h4l1.5 3h5L16 13h4M5 5h14l1 8v5a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1v-5l1-8Z',
  queue: 'M4 5h16M4 10h10M4 15h7M4 20h4M17 13v7m-3-3 3 3 3-3',
  stethoscope: 'M6 3v6a4 4 0 0 0 8 0V3M10 13v2a5 5 0 0 0 10 0v-2m0 0a2 2 0 1 0 0-4 2 2 0 0 0 0 4Z',
  settings: 'M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6Zm7.4-3a7.4 7.4 0 0 0-.1-1.2l2-1.6-2-3.4-2.4 1a7.5 7.5 0 0 0-2-1.2L14.5 3h-5l-.4 2.6a7.5 7.5 0 0 0-2 1.2l-2.4-1-2 3.4 2 1.6a7.4 7.4 0 0 0 0 2.4l-2 1.6 2 3.4 2.4-1a7.5 7.5 0 0 0 2 1.2l.4 2.6h5l.4-2.6a7.5 7.5 0 0 0 2-1.2l2.4 1 2-3.4-2-1.6c.1-.4.1-.8.1-1.2Z',
  monitor: 'M3 5h18v11H3zM8 20h8M12 16v4',
  logout: 'M15 4h3a1 1 0 0 1 1 1v14a1 1 0 0 1-1 1h-3M10 16l4-4-4-4M14 12H4',
}

export function Icon({ name, size = 20 }) {
  return (
    <svg className="icon" width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor"
      strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d={ICONS[name]} />
    </svg>
  )
}

/** Logo: chữ thập y tế đặt trong ô vuông bo góc. */
export function Logo() {
  return (
    <svg className="logo" width="36" height="36" viewBox="0 0 36 36" aria-hidden="true">
      <rect width="36" height="36" rx="9" fill="currentColor" />
      <path d="M15 9h6v6h6v6h-6v6h-6v-6H9v-6h6z" fill="var(--logo-cross, #fff)" />
    </svg>
  )
}

/** Nhãn trạng thái: chấm màu + chữ. */
export function StatusBadge({ status, text }) {
  return <span className={`badge s-${String(status).toLowerCase()}`}>{text || status}</span>
}

/** Hộp thông báo lỗi / thành công. */
export function Alert({ type = 'error', children, onClose }) {
  if (!children) return null
  return (
    <div className={`alert ${type}`} role={type === 'error' ? 'alert' : 'status'}>
      <span>{children}</span>
      {onClose && <button className="link" onClick={onClose} aria-label="Đóng">×</button>}
    </div>
  )
}

/** Danh sách thả xuống chọn khoa. */
export function DepartmentSelect({ value, onChange, allowAll = false }) {
  const api = useApi()
  const { data } = useLoad(() => api('/api/departments'), [])
  return (
    <select value={value ?? ''} onChange={e => onChange(e.target.value ? Number(e.target.value) : null)}>
      {allowAll ? <option value="">Tất cả khoa</option> : <option value="">Chọn khoa</option>}
      {(data || []).map(d => <option key={d.id} value={d.id}>{d.name}</option>)}
    </select>
  )
}

/** Danh sách thả xuống chọn bác sĩ theo khoa. */
export function DoctorSelect({ departmentId, value, onChange }) {
  const api = useApi()
  const { data } = useLoad(
    () => departmentId ? api(`/api/doctors?departmentId=${departmentId}`) : Promise.resolve([]),
    [departmentId])
  return (
    <select value={value ?? ''} onChange={e => onChange(e.target.value ? Number(e.target.value) : null)}>
      <option value="">Chọn bác sĩ</option>
      {(data || []).map(d => <option key={d.id} value={d.id}>BS. {d.fullName}{d.specialty ? ` – ${d.specialty}` : ''}</option>)}
    </select>
  )
}

/** Hộp thoại đơn giản. */
export function Modal({ title, children, onClose }) {
  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal" role="dialog" aria-modal="true" aria-label={title} onClick={e => e.stopPropagation()}>
        <div className="modal-head"><strong>{title}</strong><button className="link" onClick={onClose} aria-label="Đóng">×</button></div>
        {children}
      </div>
    </div>
  )
}