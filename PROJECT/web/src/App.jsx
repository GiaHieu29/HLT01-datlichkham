import { useEffect, useState } from 'react'
import { NavLink, Navigate, Route, Routes, useLocation } from 'react-router-dom'
import { useAuth } from 'react-oidc-context'
import { useApi } from './api'
import { Icon, Logo } from './components'
import DatLich from './pages/DatLich'
import LichCuaToi from './pages/LichCuaToi'
import ChoXacNhan from './pages/ChoXacNhan'
import DieuPhoiHangDoi from './pages/DieuPhoiHangDoi'
import GoiBenhNhan from './pages/GoiBenhNhan'
import LichKhamBacSi from './pages/LichKhamBacSi'
import ManHinhSo from './pages/ManHinhSo'
import QuanTri from './pages/QuanTri'

// Mỗi vai trò chỉ thấy các màn hình thuộc quyền của mình (mục 3.6.1)
const MENU = {
  PATIENT: [['/dat-lich', 'Đặt lịch khám', 'calendar'], ['/lich-cua-toi', 'Lịch hẹn của tôi', 'list']],
  STAFF: [['/cho-xac-nhan', 'Lịch chờ xác nhận', 'inbox'], ['/hang-doi', 'Điều phối hàng đợi', 'queue']],
  DOCTOR: [['/goi-benh-nhan', 'Gọi bệnh nhân', 'stethoscope'], ['/lich-kham', 'Lịch hẹn của tôi', 'calendar']],
  ADMIN: [['/quan-tri', 'Quản trị hệ thống', 'settings']],
}

const ROLE_TEXT = { PATIENT: 'Bệnh nhân', STAFF: 'Nhân viên tiếp đón', DOCTOR: 'Bác sĩ', ADMIN: 'Quản trị viên' }

const initials = name => (name || '?').trim().split(/\s+/).slice(-2).map(w => w[0]).join('').toUpperCase()

function Brand({ light = false }) {
  return (
    <div className={`brand${light ? ' light' : ''}`}>
      <Logo />
      <div>
        <strong>Phòng khám HLT</strong>
        <span>Đặt lịch và hàng đợi</span>
      </div>
    </div>
  )
}

export default function App() {
  const location = useLocation()
  // MH-07: màn hình số thứ tự công khai, không cần đăng nhập
  if (location.pathname.startsWith('/display')) {
    return (
      <Routes>
        <Route path="/display" element={<ManHinhSo />} />
        <Route path="/display/:departmentId" element={<ManHinhSo />} />
      </Routes>
    )
  }
  return <SecuredApp />
}

function SecuredApp() {
  const auth = useAuth()
  const api = useApi()
  const [me, setMe] = useState(null)
  const [error, setError] = useState('')

  useEffect(() => {
    if (auth.isAuthenticated) api('/api/me').then(setMe).catch(e => setError(e.message))
  }, [auth.isAuthenticated, api])

  if (auth.isLoading) return <div className="center">Đang tải…</div>
  if (auth.error) return <LoginScreen error={`Không kết nối được máy chủ đăng nhập (${auth.error.message}). Kiểm tra Keycloak đã chạy chưa rồi thử lại.`} />
  if (!auth.isAuthenticated) return <LoginScreen />

  if (error) {
    return (
      <div className="center">
        <p className="error">Không lấy được thông tin tài khoản: {error}</p>
        <p className="muted">Kiểm tra API (cổng 5080) đã chạy chưa, rồi tải lại trang.</p>
        <button onClick={() => auth.signoutRedirect()}>Đăng xuất</button>
      </div>
    )
  }
  if (!me) return <div className="center">Đang lấy thông tin tài khoản…</div>

  const menu = MENU[me.role] || []
  return (
    <div className="shell">
      <aside className="sidebar">
        <Brand light />
        <nav className="nav" aria-label="Chức năng">
          {menu.map(([to, label, icon]) => (
            <NavLink key={to} to={to} className="nav-item"><Icon name={icon} />{label}</NavLink>
          ))}
          <a className="nav-item subtle" href="/display" target="_blank" rel="noreferrer">
            <Icon name="monitor" />Màn hình số thứ tự
          </a>
        </nav>
        <div className="sidebar-user">
          <div className="avatar" aria-hidden="true">{initials(me.fullName)}</div>
          <div className="who">
            <strong>{me.fullName}</strong>
            <span>{ROLE_TEXT[me.role] || me.role}</span>
          </div>
          <button className="icon-btn" title="Đăng xuất" aria-label="Đăng xuất" onClick={() => auth.signoutRedirect()}>
            <Icon name="logout" />
          </button>
        </div>
      </aside>

      <main className="content">
        <Routes>
          {me.role === 'PATIENT' && <>
            <Route path="/dat-lich" element={<DatLich />} />
            <Route path="/lich-cua-toi" element={<LichCuaToi />} />
          </>}
          {me.role === 'STAFF' && <>
            <Route path="/cho-xac-nhan" element={<ChoXacNhan me={me} />} />
            <Route path="/hang-doi" element={<DieuPhoiHangDoi me={me} />} />
          </>}
          {me.role === 'DOCTOR' && <>
            <Route path="/goi-benh-nhan" element={<GoiBenhNhan me={me} />} />
            <Route path="/lich-kham" element={<LichKhamBacSi />} />
          </>}
          {me.role === 'ADMIN' && <Route path="/quan-tri" element={<QuanTri />} />}
          <Route path="*" element={<Navigate to={menu[0]?.[0] || '/'} replace />} />
        </Routes>
      </main>
    </div>
  )
}

/** MH-01: Đăng nhập. Bên trái giới thiệu bằng tấm phiếu số thứ tự mẫu, bên phải là nút đăng nhập. */
function LoginScreen({ error }) {
  const auth = useAuth()
  return (
    <div className="auth">
      <section className="auth-brand">
        <Brand light />
        <div className="auth-copy">
          <h1>Đặt lịch trước, đến nơi là vào khám .</h1>
          <p>Chọn bác sĩ và giờ khám trực tuyến, nhận số thứ tự khi check-in và theo dõi lượt gọi ngay trên điện thoại.</p>
        </div>
      </section>

      <section className="auth-panel">
        <div className="auth-box">
          <h2>Đăng nhập</h2>
          <p className="muted">Dùng tài khoản do phòng khám cấp. Bệnh nhân chưa có tài khoản có thể tạo mới ở trang đăng nhập.</p>
          {error && <div className="alert error" role="alert"><span>{error}</span></div>}
          <button className="primary big block" onClick={() => auth.signinRedirect()}>Đăng nhập</button>
          <a className="auth-link" href="/display"><Icon name="monitor" size={18} />Mở màn hình số thứ tự tại phòng chờ</a>
        </div>
      </section>
    </div>
  )
}