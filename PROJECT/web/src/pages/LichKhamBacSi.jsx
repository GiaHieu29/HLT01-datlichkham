import { useState } from 'react'
import { fmtTime, isoDate, useApi, useLoad } from '../api'
import { Alert, StatusBadge } from '../components'

const addDays = (n) => { const d = new Date(); d.setDate(d.getDate() + n); return isoDate(d) }

// Nhóm trạng thái để lọc nhanh
const GROUPS = {
    ALL: { label: 'Tất cả', match: () => true },
    UPCOMING: { label: 'Chưa khám', match: s => ['Pending', 'Confirmed', 'CheckedIn'].includes(s) },
    IN_PROGRESS: { label: 'Đang khám', match: s => s === 'InProgress' },
    DONE: { label: 'Đã khám', match: s => s === 'Completed' },
    CLOSED: { label: 'Đã hủy / vắng', match: s => ['Cancelled', 'NoShow'].includes(s) },
}

const dayLabel = (iso) => {
    const s = new Date(iso + 'T00:00:00').toLocaleDateString('vi-VN', { weekday: 'long', day: '2-digit', month: '2-digit', year: 'numeric' })
    return s.charAt(0).toUpperCase() + s.slice(1)
}

/** UC-12: Lịch hẹn của bác sĩ theo khoảng ngày, nhóm theo từng ngày. */
export default function LichKhamBacSi() {
    const api = useApi()
    const [from, setFrom] = useState(isoDate())
    const [to, setTo] = useState(addDays(30))
    const [group, setGroup] = useState('ALL')
    const list = useLoad(() => api(`/api/doctor/schedule?from=${from}&to=${to}`), [from, to], 30000)

    const quick = (start, end) => { setFrom(start); setTo(end) }
    const rows = (list.data || []).filter(a => GROUPS[group].match(a.status))

    // Nhóm theo ngày khám (theo giờ máy)
    const byDay = rows.reduce((acc, a) => {
        const key = isoDate(new Date(a.startAt))
            ; (acc[key] ||= []).push(a)
        return acc
    }, {})
    const days = Object.keys(byDay).sort()
    const today = isoDate()

    return (
        <section>
            <h2>Lịch hẹn của tôi</h2>

            <div className="filters">
                <label>Từ ngày <input type="date" value={from} onChange={e => setFrom(e.target.value)} /></label>
                <label>Đến ngày <input type="date" value={to} onChange={e => setTo(e.target.value)} /></label>
                <label>Trạng thái
                    <select value={group} onChange={e => setGroup(e.target.value)}>
                        {Object.entries(GROUPS).map(([k, g]) => <option key={k} value={k}>{g.label}</option>)}
                    </select>
                </label>
                <div className="quick">
                    <button onClick={() => quick(isoDate(), isoDate())}>Hôm nay</button>
                    <button onClick={() => quick(isoDate(), addDays(7))}>7 ngày tới</button>
                    <button onClick={() => quick(isoDate(), addDays(30))}>30 ngày tới</button>
                    <button onClick={() => quick(addDays(-30), isoDate())}>30 ngày qua</button>
                </div>
            </div>

            <Alert>{list.error}</Alert>

            {list.data && (
                <p className="muted summary">
                    {rows.length === 0
                        ? 'Không có lịch hẹn nào trong khoảng ngày đã chọn.'
                        : `${rows.length} lịch hẹn trong ${days.length} ngày.`}
                </p>
            )}

            {days.map(day => (
                <div key={day} className="day-group">
                    <h3 className={day === today ? 'is-today' : ''}>
                        {dayLabel(day)}
                        {day === today && <span className="today-tag">Hôm nay</span>}
                        <span className="count">{byDay[day].length} lịch</span>
                    </h3>
                    <table>
                        <thead><tr><th>Giờ</th><th>Mã</th><th>Bệnh nhân</th><th>Lý do khám</th><th>Mức ưu tiên</th><th>Trạng thái</th></tr></thead>
                        <tbody>
                            {byDay[day].map(a => (
                                <tr key={a.id}>
                                    <td><strong>{fmtTime(a.startAt)}</strong></td>
                                    <td>#{a.id}</td>
                                    <td>{a.patientName}</td>
                                    <td className="wrap">{a.reason || <span className="muted">Không ghi</span>}</td>
                                    <td>{a.priorityName}</td>
                                    <td><StatusBadge status={a.status} text={a.statusText} /></td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            ))}
        </section>
    )
}