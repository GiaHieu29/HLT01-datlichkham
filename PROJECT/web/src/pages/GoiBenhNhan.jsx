import { useState } from 'react'
import { fmtTime, useApi, useLoad } from '../api'
import { Alert, StatusBadge } from '../components'

/** MH-06: Bác sĩ gọi bệnh nhân vào khám (UC-12, UC-13, UC-14). */
export default function GoiBenhNhan({ me }) {
    const api = useApi()
    const [message, setMessage] = useState('')
    const [error, setError] = useState('')
    const current = useLoad(() => api('/api/queues/current'), [], 5000)
    const queue = useLoad(() => me.departmentId ? api(`/api/queues?departmentId=${me.departmentId}`) : Promise.resolve([]), [], 5000)
    const today = useLoad(() => api('/api/doctor/appointments'), [], 30000)

    const myTitle = `BS. ${me.fullName}`
    const waiting = (queue.data || []).filter(t => t.status === 'Waiting' && t.doctorName === myTitle)

    const reloadAll = () => { current.reload(); queue.reload(); today.reload() }
    const act = async (fn) => { setError(''); try { await fn(); reloadAll() } catch (e) { setError(e.message) } }

    const callNext = () => act(async () => {
        const t = await api('/api/queues/call-next', { method: 'POST' })
        setMessage(t ? `Đang gọi số ${t.ticketNo} – ${t.patientName}` : 'Không còn bệnh nhân nào đang chờ.')
    })

    return (
        <section>
            <h2>Gọi bệnh nhân vào khám</h2>
            <Alert type="success" onClose={() => setMessage('')}>{message}</Alert>
            <Alert onClose={() => setError('')}>{error}</Alert>

            <div className="grid-2">
                <div>
                    <h3>Đang khám</h3>
                    {(current.data || []).length === 0 && <p className="muted">Chưa gọi bệnh nhân nào.</p>}
                    {(current.data || []).map(t => (
                        <div key={t.id} className="card">
                            <div className="ticket-no small">Số {t.ticketNo}</div>
                            <p><strong>{t.patientName}</strong> · {t.priorityName}</p>
                            <p>Lý do khám: {t.reason || '—'}</p>
                            <button className="primary" onClick={() => act(() => api(`/api/queue-tickets/${t.id}/complete`, { method: 'POST' }))}>Hoàn tất</button>
                            <button onClick={() => act(() => api(`/api/queue-tickets/${t.id}/skip`, { method: 'POST' }))}>Bỏ qua (vắng)</button>
                        </div>
                    ))}
                    <button className="primary big" onClick={callNext}>Gọi bệnh nhân tiếp theo</button>
                </div>

                <div>
                    <h3>Danh sách chờ ({waiting.length})</h3>
                    <ol className="waiting">
                        {waiting.slice(0, 8).map(t => <li key={t.id}><strong>{t.ticketNo}</strong> · {t.patientName} <span className={`badge p-${t.priorityOrder}`}>{t.priorityName}</span></li>)}
                        {waiting.length === 0 && <li className="muted">Không có ai đang chờ.</li>}
                    </ol>
                </div>
            </div>

            <h3>Lịch làm việc hôm nay</h3>
            <table>
                <thead><tr><th>Giờ</th><th>Bệnh nhân</th><th>Lý do</th><th>Trạng thái</th></tr></thead>
                <tbody>
                    {(today.data || []).map(a => (
                        <tr key={a.id}><td>{fmtTime(a.startAt)}</td><td>{a.patientName}</td><td className="wrap">{a.reason}</td>
                            <td><StatusBadge status={a.status} text={a.statusText} /></td></tr>
                    ))}
                    {(today.data || []).length === 0 && <tr><td colSpan={4} className="muted">Hôm nay chưa có lịch hẹn.</td></tr>}
                </tbody>
            </table>
        </section>
    )
}