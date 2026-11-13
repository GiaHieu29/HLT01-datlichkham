import { useNavigate, useParams } from 'react-router-dom'
import { useEffect, useState } from 'react'
import { API_URL } from '../config'
import { DepartmentSelect } from '../components'

/** MH-07: Màn hình hiển thị số thứ tự tại phòng chờ – công khai, không có thông tin cá nhân. */
export default function ManHinhSo() {
  const { departmentId } = useParams()
  const navigate = useNavigate()
  const [data, setData] = useState(null)

  useEffect(() => {
    if (!departmentId) return
    const load = () => fetch(`${API_URL}/api/queues/display?departmentId=${departmentId}`)
      .then(r => r.ok ? r.json() : null).then(setData).catch(() => {})
    load()
    const id = setInterval(load, 5000) // làm mới mỗi 5 giây
    return () => clearInterval(id)
  }, [departmentId])

  if (!departmentId) {
    return (
      <div className="login">
        <h1>Màn hình số thứ tự</h1>
        <label>Chọn khoa <DepartmentSelect value={null} onChange={id => id && navigate(`/display/${id}`)} /></label>
      </div>
    )
  }

  return (
    <div className="display">
      <h1>{data?.departmentName || 'Đang tải…'}</h1>
      <div className="display-label">ĐANG GỌI</div>
      <div className="display-calling">
        {(data?.calling || []).length === 0 && <div className="display-empty">—</div>}
        {(data?.calling || []).map(c => (
          <div key={c.ticketNo} className="display-number">
            <span>{c.ticketNo}</span>
            <small>{c.doctorName}</small>
          </div>
        ))}
      </div>
      <div className="display-next">
        Số tiếp theo: {(data?.nextNumbers || []).join(' · ') || '—'}
        <span> | Đang chờ: {data?.waitingCount ?? 0} người</span>
      </div>
    </div>
  )
}