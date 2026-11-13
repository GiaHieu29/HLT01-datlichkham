import { useState } from 'react'
import { isoDate, tomorrow, useApi, useLoad } from '../api'
import { Alert, DepartmentSelect, DoctorSelect, Modal } from '../components'

/** MH-08: Quản trị – tài khoản, khoa, mức ưu tiên, ca làm việc (UC-15, UC-16, UC-17). */
export default function QuanTri() {
  const [tab, setTab] = useState('users')
  const tabs = [['users', 'Tài khoản'], ['departments', 'Khoa'], ['priorities', 'Mức ưu tiên'], ['schedules', 'Ca làm việc']]
  return (
    <section>
      <h2>Quản trị hệ thống</h2>
      <div className="tabs">
        {tabs.map(([k, label]) => <button key={k} className={tab === k ? 'active' : ''} onClick={() => setTab(k)}>{label}</button>)}
      </div>
      {tab === 'users' && <Users />}
      {tab === 'departments' && <Departments />}
      {tab === 'priorities' && <Priorities />}
      {tab === 'schedules' && <Schedules />}
    </section>
  )
}

function useAction(reload) {
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')
  const run = async (fn, ok) => {
    setError('')
    try { await fn(); setMessage(ok); reload?.() } catch (e) { setError(e.message) }
  }
  const alerts = <>
    <Alert type="success" onClose={() => setMessage('')}>{message}</Alert>
    <Alert onClose={() => setError('')}>{error}</Alert>
  </>
  return { run, alerts }
}

const ROLE_LABEL = { PATIENT: 'Bệnh nhân', STAFF: 'Nhân viên', DOCTOR: 'Bác sĩ', ADMIN: 'Quản trị viên' }
const STATUS_LABEL = { ACTIVE: 'Đang hoạt động', LOCKED: 'Đã khóa' }

function Users() {
  const api = useApi()
  const users = useLoad(() => api('/api/admin/users'), [])
  const { run, alerts } = useAction(users.reload)
  const [edit, setEdit] = useState({}) // id -> { role, departmentId }

  return (
    <>
      {alerts}
      <p className="muted">Người tự đăng ký trên trang đăng nhập sẽ là bệnh nhân. Khi đổi sang bác sĩ hoặc nhân viên, chọn thêm khoa làm việc.</p>
      <table>
        <thead><tr><th>Mã</th><th>Họ tên</th><th>Email</th><th>Vai trò</th><th>Khoa</th><th>Trạng thái</th><th></th></tr></thead>
        <tbody>
          {(users.data || []).map(u => {
            const e = edit[u.id] || { role: u.role, departmentId: null }
            const changed = e.role !== u.role
            const needDept = changed && ['STAFF', 'DOCTOR'].includes(e.role)
            return (
              <tr key={u.id}>
                <td>{u.id}</td><td>{u.fullName}</td><td>{u.email}</td>
                <td>
                  <select value={e.role} onChange={ev => setEdit({ ...edit, [u.id]: { ...e, role: ev.target.value } })}>
                    {Object.entries(ROLE_LABEL).map(([code, label]) => <option key={code} value={code}>{label}</option>)}
                  </select>
                </td>
                <td>
                  {needDept
                    ? <DepartmentSelect value={e.departmentId} onChange={d => setEdit({ ...edit, [u.id]: { ...e, departmentId: d } })} />
                    : <span className="muted">{['STAFF', 'DOCTOR'].includes(u.role) ? 'Giữ nguyên' : '–'}</span>}
                </td>
                <td>
                  <span className={`badge ${u.status === 'ACTIVE' ? 's-completed' : 's-cancelled'}`}>
                    {STATUS_LABEL[u.status] || u.status}
                  </span>
                </td>
                <td className="actions">
                  <button className={changed ? 'primary' : ''} disabled={!changed || (needDept && !e.departmentId)}
                    onClick={() => run(() => api(`/api/admin/users/${u.id}/role`, { method: 'PUT', body: e }),
                      `Đã đổi vai trò của ${u.fullName} thành ${ROLE_LABEL[e.role]}.`)}>Lưu vai trò</button>
                  {u.status === 'ACTIVE'
                    ? <button className="danger" onClick={() => run(() => api(`/api/admin/users/${u.id}/lock`, { method: 'POST' }), `Đã khóa tài khoản ${u.fullName}.`)}>Khóa</button>
                    : <button onClick={() => run(() => api(`/api/admin/users/${u.id}/unlock`, { method: 'POST' }), `Đã mở khóa tài khoản ${u.fullName}.`)}>Mở khóa</button>}
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </>
  )
}

function Departments() {
  const api = useApi()
  const list = useLoad(() => api('/api/departments'), [])
  const { run, alerts } = useAction(list.reload)
  const [form, setForm] = useState({ name: '', location: '' })
  return (
    <>
      {alerts}
      <div className="filters">
        <input placeholder="Tên khoa" value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} />
        <input placeholder="Vị trí" value={form.location} onChange={e => setForm({ ...form, location: e.target.value })} />
        <button className="primary" onClick={() => run(() => api('/api/admin/departments', { method: 'POST', body: form }), 'Đã thêm khoa.')}>Thêm khoa</button>
      </div>
      <table><thead><tr><th>ID</th><th>Tên khoa</th><th>Vị trí</th></tr></thead>
        <tbody>{(list.data || []).map(d => <tr key={d.id}><td>{d.id}</td><td>{d.name}</td><td>{d.location}</td></tr>)}</tbody>
      </table>
    </>
  )
}

/** Tạo mã nội bộ từ tên tiếng Việt: "Người cao tuổi" -> "NGUOI_CAO_TUOI" (người dùng không cần nhập mã). */
const toCode = name => name.normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/đ/gi, 'D')
  .toUpperCase().replace(/[^A-Z0-9]+/g, '_').replace(/^_|_$/g, '').slice(0, 20)

function Priorities() {
  const api = useApi()
  const list = useLoad(() => api('/api/priority-levels'), [])
  const { run, alerts } = useAction(list.reload)
  const empty = { name: '', levelOrder: 5, description: '' }
  const [form, setForm] = useState(empty)
  const [editing, setEditing] = useState(null)   // dòng đang sửa: { id, code, name, levelOrder, description }
  const [deleting, setDeleting] = useState(null) // mức đang chờ xác nhận xóa

  const add = () => run(async () => {
    await api('/api/admin/priority-levels', { method: 'POST', body: { ...form, code: toCode(form.name) } })
    setForm(empty)
  }, `Đã thêm mức ưu tiên «${form.name}».`)

  const save = () => run(async () => {
    await api(`/api/admin/priority-levels/${editing.id}`, { method: 'PUT', body: editing })
    setEditing(null)
  }, `Đã cập nhật mức ưu tiên «${editing.name}».`)

  const remove = () => {
    const p = deleting
    setDeleting(null)
    run(() => api(`/api/admin/priority-levels/${p.id}`, { method: 'DELETE' }), `Đã xóa mức ưu tiên «${p.name}».`)
  }

  const set = (k, v) => setEditing({ ...editing, [k]: v })

  return (
    <>
      {alerts}
      <p className="muted">Số thứ tự càng nhỏ thì bệnh nhân được gọi vào khám càng sớm. Mức «Bình thường» là mức mặc định khi đặt lịch nên không xóa được.</p>
      <div className="filters">
        <label>Tên mức ưu tiên <input placeholder="Ví dụ: Người cao tuổi" value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} /></label>
        <label>Thứ tự gọi <input type="number" min={1} style={{ width: 100 }} value={form.levelOrder} onChange={e => setForm({ ...form, levelOrder: Number(e.target.value) })} /></label>
        <label>Mô tả <input placeholder="Áp dụng cho ai" value={form.description} onChange={e => setForm({ ...form, description: e.target.value })} /></label>
        <button className="primary" disabled={!form.name.trim()} onClick={add}>Thêm mức ưu tiên</button>
      </div>
      <table>
        <thead><tr><th>Thứ tự gọi</th><th>Tên mức ưu tiên</th><th>Mô tả</th><th></th></tr></thead>
        <tbody>{(list.data || []).map(p => editing?.id === p.id ? (
          <tr key={p.id}>
            <td><input type="number" min={1} style={{ width: 80 }} value={editing.levelOrder} onChange={e => set('levelOrder', Number(e.target.value))} /></td>
            <td><input value={editing.name} onChange={e => set('name', e.target.value)} /></td>
            <td><input value={editing.description || ''} onChange={e => set('description', e.target.value)} /></td>
            <td className="actions">
              <button className="primary" disabled={!editing.name.trim() || editing.levelOrder < 1} onClick={save}>Lưu</button>
              <button onClick={() => setEditing(null)}>Hủy</button>
            </td>
          </tr>
        ) : (
          <tr key={p.id}>
            <td><span className={`badge p-${Math.min(p.levelOrder, 4)}`}>{p.levelOrder}</span></td>
            <td><strong>{p.name}</strong></td>
            <td>{p.description}</td>
            <td className="actions">
              <button onClick={() => setEditing({ ...p })}>Sửa</button>
              <button className="danger" disabled={p.code === 'NORMAL'} title={p.code === 'NORMAL' ? 'Mức mặc định, không xóa được' : ''}
                onClick={() => setDeleting(p)}>Xóa</button>
            </td>
          </tr>
        ))}</tbody>
      </table>

      {deleting && (
        <Modal title="Xóa mức ưu tiên" onClose={() => setDeleting(null)}>
          <p>Bạn chắc chắn muốn xóa mức <strong>«{deleting.name}»</strong>?</p>
          <p className="muted">Nếu đã có lịch hẹn dùng mức này thì hệ thống sẽ không cho xóa. Khi đó hãy đổi tên hoặc thứ tự thay vì xóa.</p>
          <div className="actions" style={{ display: 'flex', gap: 8, justifyContent: 'flex-end', marginTop: 16 }}>
            <button onClick={() => setDeleting(null)}>Hủy</button>
            <button className="danger" onClick={remove}>Xóa</button>
          </div>
        </Modal>
      )}
    </>
  )
}

function Schedules() {
  const api = useApi()
  const [departmentId, setDepartmentId] = useState(null)
  const [form, setForm] = useState({ doctorId: null, workDate: tomorrow(), startTime: '07:30', endTime: '11:30', slotMinutes: 30 })
  const list = useLoad(() => {
    if (!form.doctorId) return Promise.resolve([])
    const to = new Date(); to.setDate(to.getDate() + 14) // xem 14 ngày tới
    return api(`/api/admin/schedules?doctorId=${form.doctorId}&from=${isoDate()}&to=${isoDate(to)}`)
  }, [form.doctorId])
  const { run, alerts } = useAction(list.reload)

  const create = () => run(() => api('/api/admin/schedules', {
    method: 'POST',
    body: { ...form, startTime: form.startTime + ':00', endTime: form.endTime + ':00', maxPatients: null },
  }), 'Đã tạo ca làm việc và sinh khung giờ.')

  return (
    <>
      {alerts}
      <div className="filters">
        <label>Khoa <DepartmentSelect value={departmentId} onChange={setDepartmentId} /></label>
        <label>Bác sĩ <DoctorSelect departmentId={departmentId} value={form.doctorId} onChange={v => setForm({ ...form, doctorId: v })} /></label>
        <label>Ngày <input type="date" value={form.workDate} onChange={e => setForm({ ...form, workDate: e.target.value })} /></label>
        <label>Từ <input type="time" value={form.startTime} onChange={e => setForm({ ...form, startTime: e.target.value })} /></label>
        <label>Đến <input type="time" value={form.endTime} onChange={e => setForm({ ...form, endTime: e.target.value })} /></label>
        <label>Phút/lượt <input type="number" min={5} max={240} style={{ width: 80 }} value={form.slotMinutes} onChange={e => setForm({ ...form, slotMinutes: Number(e.target.value) })} /></label>
        <button className="primary" disabled={!form.doctorId} onClick={create}>Tạo ca</button>
      </div>
      <table><thead><tr><th>Ngày</th><th>Giờ</th><th>Phút/lượt</th><th>Số khung giờ</th></tr></thead>
        <tbody>{(list.data || []).map(s => <tr key={s.id}><td>{s.workDate}</td><td>{s.startTime.slice(0, 5)} – {s.endTime.slice(0, 5)}</td><td>{s.slotMinutes}</td><td>{s.slotCount}</td></tr>)}</tbody>
      </table>
    </>
  )
}