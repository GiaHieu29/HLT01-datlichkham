import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { fmtTime, tomorrow, useApi } from '../api'
import { Alert, DepartmentSelect, DoctorSelect } from '../components'

const SLOT_TEXT = {
    AVAILABLE: 'Còn trống', BOOKED: 'Đã đầy', HELD: 'Người khác đang giữ',
    MINE: 'Đang chọn', BLOCKED: 'Không nhận lịch', PAST: 'Đã qua',
}

/** MH-02: Tìm và đặt lịch khám (UC-01, UC-02). */
export default function DatLich() {
    const api = useApi()
    const navigate = useNavigate()
    const [departmentId, setDepartmentId] = useState(null)
    const [doctorId, setDoctorId] = useState(null)
    const [date, setDate] = useState(tomorrow())
    const [session, setSession] = useState('ALL')
    const [slots, setSlots] = useState(null)
    const [selected, setSelected] = useState(null)
    const [reason, setReason] = useState('')
    const [error, setError] = useState('')
    const [busy, setBusy] = useState(false)

    async function search() {
        setError(''); setSelected(null)
        if (!doctorId) { setError('Vui lòng chọn khoa và bác sĩ.'); return }
        try { setSlots(await api(`/api/slots?doctorId=${doctorId}&date=${date}`)) }
        catch (e) { setError(e.message) }
    }

    async function choose(slot) {
        setError('')
        try {
            await api(`/api/slots/${slot.id}/hold`, { method: 'POST' }) // giữ chỗ tạm vài phút
            setSelected(slot)
            setSlots(list => list.map(s => s.id === slot.id ? { ...s, status: 'MINE' }
                : s.status === 'MINE' ? { ...s, status: 'AVAILABLE' } : s))
        } catch (e) { setError(e.message); search() }
    }

    async function book() {
        if (!selected) { setError('Vui lòng chọn một khung giờ.'); return }
        setBusy(true); setError('')
        try {
            await api('/api/appointments', { method: 'POST', body: { slotId: selected.id, reason } })
            navigate('/lich-cua-toi', { state: { message: 'Đặt lịch thành công! Lịch đang chờ nhân viên xác nhận.' } })
        } catch (e) {
            // 409: khung giờ vừa bị người khác đặt -> báo xung đột và tải lại (luồng 4a)
            setError(e.status === 409 ? `Xung đột: ${e.message}` : e.message)
            search()
        } finally { setBusy(false) }
    }

    const visible = (slots || []).filter(s => {
        const h = new Date(s.startAt).getHours()
        return session === 'ALL' || (session === 'AM' ? h < 12 : h >= 12)
    })

    return (
        <section>
            <h2>Tìm và đặt lịch khám</h2>
            <div className="filters">
                <label>Khoa <DepartmentSelect value={departmentId} onChange={v => { setDepartmentId(v); setDoctorId(null) }} /></label>
                <label>Bác sĩ <DoctorSelect departmentId={departmentId} value={doctorId} onChange={setDoctorId} /></label>
                <label>Ngày khám <input type="date" value={date} onChange={e => setDate(e.target.value)} /></label>
                <label>Buổi
                    <select value={session} onChange={e => setSession(e.target.value)}>
                        <option value="ALL">Tất cả</option><option value="AM">Sáng</option><option value="PM">Chiều</option>
                    </select>
                </label>
                <button className="primary" onClick={search}>Tìm khung giờ</button>
            </div>
            <p className="muted">Mỗi khung giờ chỉ nhận một lịch hẹn. Khi bấm «Chọn», hệ thống giữ chỗ tạm cho bạn vài phút.</p>

            <Alert onClose={() => setError('')}>{error}</Alert>

            {slots && (
                <table>
                    <thead><tr><th>Giờ khám</th><th>Trạng thái</th><th>Thao tác</th></tr></thead>
                    <tbody>
                        {visible.length === 0 && <tr><td colSpan={3} className="muted">Không có khung giờ.</td></tr>}
                        {visible.map(s => (
                            <tr key={s.id} className={s.status === 'MINE' ? 'row-selected' : ''}>
                                <td>{fmtTime(s.startAt)} – {fmtTime(s.endAt)}</td>
                                <td><span className={`badge slot-${s.status.toLowerCase()}`}>{SLOT_TEXT[s.status]}</span></td>
                                <td>{(s.status === 'AVAILABLE' || s.status === 'MINE') &&
                                    <button onClick={() => choose(s)}>{s.status === 'MINE' ? 'Đã chọn' : 'Chọn'}</button>}</td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            )}

            {selected && (
                <div className="card">
                    <p>Khung giờ đã chọn: <strong>{fmtTime(selected.startAt)}</strong> ngày {date}</p>
                    <label className="block">Lý do khám
                        <textarea rows={3} maxLength={500} value={reason} onChange={e => setReason(e.target.value)}
                            placeholder="Ví dụ: Đau đầu, chóng mặt hai ngày" />
                    </label>
                    <button className="primary" disabled={busy} onClick={book}>{busy ? 'Đang đặt…' : 'Đặt lịch'}</button>
                    <button onClick={() => setSelected(null)}>Hủy</button>
                </div>
            )}
        </section>
    )
}