import { useState } from 'react'
import { useLocation } from 'react-router-dom'
import { fmtDateTime, useApi, useLoad } from '../api'
import { Alert, StatusBadge } from '../components'

/** MH-03: Lịch hẹn của tôi và số thứ tự (UC-03, UC-05, UC-06). */
export default function LichCuaToi() {
    const api = useApi()
    const location = useLocation()
    const [message, setMessage] = useState(location.state?.message || '')
    const [error, setError] = useState('')
    const appts = useLoad(() => api('/api/appointments/mine'), [], 10000)      // tự làm mới 10 giây
    const notis = useLoad(() => api('/api/notifications'), [], 10000)

    async function cancel(a) {
        if (!window.confirm(`Hủy lịch khám lúc ${fmtDateTime(a.startAt)}?`)) return
        try {
            await api(`/api/appointments/${a.id}/cancel`, { method: 'POST', body: { reason: 'Bệnh nhân tự hủy' } })
            setMessage('Đã hủy lịch hẹn.'); appts.reload(); notis.reload()
        } catch (e) { setError(e.message) }
    }

    const waiting = (appts.data || []).filter(a => a.ticketNo && ['CheckedIn', 'InProgress'].includes(a.status))

    return (
        <section>
            <h2>Lịch hẹn của tôi</h2>
            <Alert type="success" onClose={() => setMessage('')}>{message}</Alert>
            <Alert onClose={() => setError('')}>{error || appts.error}</Alert>

            {waiting.map(a => (
                <div key={a.id} className="ticket-card">
                    <div>Số thứ tự của bạn</div>
                    <div className="ticket-no">{a.ticketNo}</div>
                    <div>{a.status === 'InProgress' ? 'Đang được gọi vào khám – mời bạn vào phòng!' : `Còn ${a.peopleAhead ?? 0} người đang chờ trước bạn`}</div>
                    <div className="muted">{a.doctorName} · {a.departmentName}</div>
                </div>
            ))}

            <table>
                <thead><tr><th>Mã</th><th>Ngày giờ</th><th>Bác sĩ</th><th>Khoa</th><th>Trạng thái</th><th>Ưu tiên</th><th></th></tr></thead>
                <tbody>
                    {(appts.data || []).length === 0 && <tr><td colSpan={7} className="muted">Bạn chưa có lịch hẹn nào.</td></tr>}
                    {(appts.data || []).map(a => (
                        <tr key={a.id}>
                            <td>#{a.id}</td>
                            <td>{fmtDateTime(a.startAt)}</td>
                            <td>{a.doctorName}</td>
                            <td>{a.departmentName}</td>
                            <td><StatusBadge status={a.status} text={a.statusText} /></td>
                            <td>{a.priorityName}</td>
                            <td>{a.canCancel && <button className="danger" onClick={() => cancel(a)}>Hủy lịch</button>}</td>
                        </tr>
                    ))}
                </tbody>
            </table>

            <h3>Thông báo</h3>
            <ul className="notis">
                {(notis.data || []).length === 0 && <li className="muted">Chưa có thông báo.</li>}
                {(notis.data || []).map(n => <li key={n.id}>{n.content}</li>)}
            </ul>
        </section>
    )
}