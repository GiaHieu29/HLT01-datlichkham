import { useState } from 'react'
import { fmtDateTime, fmtTime, isoDate, useApi, useLoad } from '../api'
import { Alert, DepartmentSelect, Modal, StatusBadge } from '../components'

/** MH-04: Danh sách lịch chờ xác nhận (UC-07, UC-08, UC-09). */
export default function ChoXacNhan({ me }) {
    const api = useApi()
    const [departmentId, setDepartmentId] = useState(me.departmentId)
    const [date, setDate] = useState('')
    const [status, setStatus] = useState('Pending')
    const [priorityOf, setPriorityOf] = useState({})       // id lịch -> id mức ưu tiên đang chọn
    const [message, setMessage] = useState('')
    const [error, setError] = useState('')
    const [rejecting, setRejecting] = useState(null)
    const [rescheduling, setRescheduling] = useState(null)

    const priorities = useLoad(() => api('/api/priority-levels'), [])
    const list = useLoad(() => {
        const q = new URLSearchParams()
        if (status) q.set('status', status)
        if (departmentId) q.set('departmentId', departmentId)
        if (date) q.set('date', date)
        return api(`/api/staff/appointments?${q}`)
    }, [status, departmentId, date])

    const run = async (fn, ok) => {
        setError('')
        try { await fn(); setMessage(ok); list.reload() } catch (e) { setError(e.message) }
    }

    const confirm = a => run(
        () => api(`/api/appointments/${a.id}/confirm`, { method: 'POST', body: { priorityId: Number(priorityOf[a.id] ?? a.priorityId) } }),
        `Đã xác nhận lịch #${a.id} và gửi thông báo cho bệnh nhân.`)

    return (
        <section>
            <h2>Danh sách lịch hẹn</h2>
            <div className="filters">
                <label>Khoa <DepartmentSelect value={departmentId} onChange={setDepartmentId} allowAll /></label>
                <label>Ngày <input type="date" value={date} onChange={e => setDate(e.target.value)} /></label>
                <label>Trạng thái
                    <select value={status} onChange={e => setStatus(e.target.value)}>
                        <option value="Pending">Chờ xác nhận</option>
                        <option value="Confirmed">Đã xác nhận</option>
                        <option value="CheckedIn">Đã check-in</option>
                        <option value="Cancelled">Đã hủy</option>
                        <option value="">Tất cả</option>
                    </select>
                </label>
                <button onClick={list.reload}>Làm mới</button>
            </div>

            <Alert type="success" onClose={() => setMessage('')}>{message}</Alert>
            <Alert onClose={() => setError('')}>{error || list.error}</Alert>

            <table>
                <thead><tr><th>Mã</th><th>Bệnh nhân</th><th>Bác sĩ</th><th>Ngày giờ</th><th>Lý do</th><th>Đặt lúc</th><th>Trạng thái</th><th>Mức ưu tiên</th><th>Thao tác</th></tr></thead>
                <tbody>
                    {(list.data || []).length === 0 && <tr><td colSpan={9} className="muted">Không có lịch hẹn.</td></tr>}
                    {(list.data || []).map(a => (
                        <tr key={a.id}>
                            <td>#{a.id}</td>
                            <td>{a.patientName}</td>
                            <td>{a.doctorName}</td>
                            <td>{fmtDateTime(a.startAt)}</td>
                            <td className="wrap">{a.reason}</td>
                            <td>{fmtDateTime(a.createdAt)}</td>
                            <td><StatusBadge status={a.status} text={a.statusText} /></td>
                            <td>
                                {a.status === 'Pending'
                                    ? <select value={priorityOf[a.id] ?? a.priorityId} onChange={e => setPriorityOf({ ...priorityOf, [a.id]: e.target.value })}>
                                        {(priorities.data || []).map(p => <option key={p.id} value={p.id}>{p.name}</option>)}
                                    </select>
                                    : a.priorityName}
                            </td>
                            <td className="actions">
                                {a.status === 'Pending' && <>
                                    <button className="primary" onClick={() => confirm(a)}>Xác nhận</button>
                                    <button className="danger" onClick={() => setRejecting(a)}>Từ chối</button>
                                </>}
                                {a.status === 'Confirmed' && <>
                                    <button onClick={() => setRescheduling(a)}>Đổi lịch</button>
                                    <button className="danger" onClick={() => run(
                                        () => api(`/api/appointments/${a.id}/cancel`, { method: 'POST', body: { reason: 'Nhân viên hủy' } }),
                                        `Đã hủy lịch #${a.id}.`)}>Hủy</button>
                                </>}
                            </td>
                        </tr>
                    ))}
                </tbody>
            </table>

            {rejecting && <RejectDialog appointment={rejecting} onClose={() => setRejecting(null)}
                onDone={(reason) => {
                    setRejecting(null); run(
                        () => api(`/api/appointments/${rejecting.id}/reject`, { method: 'POST', body: { reason } }),
                        `Đã từ chối lịch #${rejecting.id}.`)
                }} />}

            {rescheduling && <RescheduleDialog appointment={rescheduling} onClose={() => setRescheduling(null)}
                onDone={() => { setRescheduling(null); setMessage(`Đã đổi lịch #${rescheduling.id} và thông báo cho bệnh nhân.`); list.reload() }} />}
        </section>
    )
}

function RejectDialog({ appointment, onClose, onDone }) {
    const [reason, setReason] = useState('')
    return (
        <Modal title={`Từ chối lịch #${appointment.id}`} onClose={onClose}>
            <label className="block">Lý do (bắt buộc)
                <textarea rows={3} value={reason} onChange={e => setReason(e.target.value)} />
            </label>
            <button className="danger" disabled={!reason.trim()} onClick={() => onDone(reason)}>Từ chối</button>
        </Modal>
    )
}

/** Hộp thoại chọn khung giờ mới còn trống (cùng bác sĩ hoặc bác sĩ khác trong khoa). */
function RescheduleDialog({ appointment, onClose, onDone }) {
    const api = useApi()
    const [date, setDate] = useState(isoDate(new Date(appointment.startAt)))
    const [doctorId, setDoctorId] = useState(null)
    const [error, setError] = useState('')
    const doctors = useLoad(() => api(`/api/doctors?departmentId=${appointment.departmentId}`), [])
    const currentDoctor = (doctors.data || []).find(d => `BS. ${d.fullName}` === appointment.doctorName)
    const effectiveDoctor = doctorId ?? currentDoctor?.id
    const slots = useLoad(
        () => effectiveDoctor ? api(`/api/slots?doctorId=${effectiveDoctor}&date=${date}`) : Promise.resolve([]),
        [effectiveDoctor, date])

    async function pick(slot) {
        try {
            await api(`/api/appointments/${appointment.id}/reschedule`, { method: 'POST', body: { newSlotId: slot.id, reason: '' } })
            onDone()
        } catch (e) { setError(e.message); slots.reload() } // luồng 3a: giờ mới vừa bị đặt
    }

    return (
        <Modal title={`Đổi lịch #${appointment.id} – ${appointment.patientName}`} onClose={onClose}>
            <p className="muted">Lịch hiện tại: {fmtDateTime(appointment.startAt)} · {appointment.doctorName}</p>
            <div className="filters">
                <label>Bác sĩ
                    <select value={effectiveDoctor ?? ''} onChange={e => setDoctorId(Number(e.target.value))}>
                        {(doctors.data || []).map(d => <option key={d.id} value={d.id}>BS. {d.fullName}</option>)}
                    </select>
                </label>
                <label>Ngày <input type="date" value={date} onChange={e => setDate(e.target.value)} /></label>
            </div>
            <Alert onClose={() => setError('')}>{error}</Alert>
            <div className="slot-grid">
                {(slots.data || []).filter(s => s.status === 'AVAILABLE').map(s =>
                    <button key={s.id} onClick={() => pick(s)}>{fmtTime(s.startAt)}</button>)}
                {(slots.data || []).filter(s => s.status === 'AVAILABLE').length === 0 && <span className="muted">Không còn khung giờ trống.</span>}
            </div>
        </Modal>
    )
}