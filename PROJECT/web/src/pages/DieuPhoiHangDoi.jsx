import { useState } from 'react'
import { fmtTime, useApi, useLoad } from '../api'
import { Alert, DepartmentSelect, StatusBadge } from '../components'

const minutesSince = s => Math.max(0, Math.round((Date.now() - new Date(s)) / 60000))

/** MH-05: Điều phối hàng đợi – check-in và theo dõi hàng đợi của khoa (UC-10, UC-11). */
export default function DieuPhoiHangDoi({ me }) {
    const api = useApi()
    const [departmentId, setDepartmentId] = useState(me.departmentId)
    const [keyword, setKeyword] = useState('')
    const [results, setResults] = useState(null)
    const [message, setMessage] = useState('')
    const [error, setError] = useState('')

    // Truy vấn định kỳ 5 giây (polling) – mục 4.3.5
    const queue = useLoad(
        () => departmentId ? api(`/api/queues?departmentId=${departmentId}`) : Promise.resolve([]),
        [departmentId], 5000)

    async function search(e) {
        e?.preventDefault(); setError('')
        try {
            const q = new URLSearchParams({ keyword })
            if (departmentId) q.set('departmentId', departmentId)
            setResults(await api(`/api/staff/appointments/search?${q}`))
        } catch (err) { setError(err.message) }
    }

    async function checkIn(a) {
        setError('')
        try {
            const t = await api(`/api/appointments/${a.id}/check-in`, { method: 'POST' })
            setMessage(`Đã check-in ${a.patientName}. Số thứ tự: ${t.ticketNo}`)
            queue.reload(); search()
        } catch (err) { setError(err.message) }
    }

    async function noShow(a) {
        if (!window.confirm(`Đánh dấu ${a.patientName} vắng mặt?`)) return
        try {
            await api(`/api/appointments/${a.id}/no-show`, { method: 'POST', body: { reason: 'Không đến khám' } })
            setMessage(`Đã đánh dấu vắng mặt lịch #${a.id}.`); queue.reload(); search()
        } catch (err) { setError(err.message) }
    }

    return (
        <section>
            <h2>Điều phối hàng đợi</h2>
            <div className="filters">
                <label>Khoa <DepartmentSelect value={departmentId} onChange={setDepartmentId} /></label>
                {departmentId && <a href={`/display/${departmentId}`} target="_blank" rel="noreferrer">Mở màn hình số thứ tự ↗</a>}
            </div>

            <form className="filters" onSubmit={search}>
                <label>Tìm lịch hẹn hôm nay
                    <input value={keyword} onChange={e => setKeyword(e.target.value)} placeholder="Mã lịch hẹn hoặc tên bệnh nhân" />
                </label>
                <button className="primary">Tìm</button>
            </form>

            <Alert type="success" onClose={() => setMessage('')}>{message}</Alert>
            <Alert onClose={() => setError('')}>{error}</Alert>

            {results && (
                <table>
                    <thead><tr><th>Mã</th><th>Bệnh nhân</th><th>Giờ hẹn</th><th>Bác sĩ</th><th>Ưu tiên</th><th>Trạng thái</th><th></th></tr></thead>
                    <tbody>
                        {results.length === 0 && <tr><td colSpan={7} className="muted">Không tìm thấy lịch hẹn hôm nay.</td></tr>}
                        {results.map(a => (
                            <tr key={a.id}>
                                <td>#{a.id}</td><td>{a.patientName}</td><td>{fmtTime(a.startAt)}</td><td>{a.doctorName}</td>
                                <td>{a.priorityName}</td><td><StatusBadge status={a.status} text={a.statusText} /></td>
                                <td className="actions">{a.status === 'Confirmed' && <>
                                    <button className="primary" onClick={() => checkIn(a)}>Check-in</button>
                                    <button onClick={() => noShow(a)}>Vắng mặt</button>
                                </>}</td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            )}

            <h3>Hàng đợi hôm nay {queue.data && <span className="muted">({queue.data.filter(t => t.status === 'Waiting').length} người đang chờ)</span>}</h3>
            <table>
                <thead><tr><th>Số</th><th>Bệnh nhân</th><th>Bác sĩ</th><th>Mức ưu tiên</th><th>Trạng thái</th><th>Vào hàng lúc</th><th>Đã chờ</th></tr></thead>
                <tbody>
                    {(queue.data || []).length === 0 && <tr><td colSpan={7} className="muted">Chưa có ai trong hàng đợi.</td></tr>}
                    {(queue.data || []).map(t => (
                        <tr key={t.id} className={t.status === 'Called' ? 'row-selected' : ''}>
                            <td><strong>{t.ticketNo}</strong></td><td>{t.patientName}</td><td>{t.doctorName}</td>
                            <td><span className={`badge p-${t.priorityOrder}`}>{t.priorityName}</span></td>
                            <td><StatusBadge status={t.status} text={t.statusText} /></td>
                            <td>{fmtTime(t.enqueuedAt)}</td>
                            <td>{t.status === 'Waiting' ? `${minutesSince(t.enqueuedAt)} phút` : ''}</td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </section>
    )
}