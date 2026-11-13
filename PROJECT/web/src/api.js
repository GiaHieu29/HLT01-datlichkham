import { useCallback, useEffect, useRef, useState } from 'react'
import { useAuth } from 'react-oidc-context'
import { API_URL } from './config'

/** Hook gọi API: tự gắn token "Bearer" và đổi lỗi HTTP thành thông báo tiếng Việt. */
export function useApi() {
  const auth = useAuth()
  const token = auth.user?.access_token

  return useCallback(async (path, { method = 'GET', body } = {}) => {
    const headers = { 'Content-Type': 'application/json' }
    if (token) headers.Authorization = `Bearer ${token}`

    const res = await fetch(API_URL + path, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
    })
    if (res.status === 204) return null

    const text = await res.text()
    let data = null
    try { data = text ? JSON.parse(text) : null } catch { data = text }

    if (!res.ok) {
      if (res.status === 401) throw new Error('Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại.')
      if (res.status === 403 && !data?.detail) throw new Error('Bạn không có quyền thực hiện thao tác này.')
      const err = new Error(data?.detail || data?.title || `Lỗi ${res.status}`)
      err.status = res.status
      throw err
    }
    return data
  }, [token])
}

/** Tải dữ liệu một lần (và tự tải lại mỗi `intervalMs` nếu có) – dùng cho màn hình hàng đợi. */
export function useLoad(loader, deps = [], intervalMs = 0) {
  const [data, setData] = useState(null)
  const [error, setError] = useState('')
  const [tick, setTick] = useState(0)
  // Luôn dùng loader mới nhất (token có thể được làm mới trong lúc đang tự tải lại)
  const loaderRef = useRef(loader)
  loaderRef.current = loader

  useEffect(() => {
    let alive = true
    const run = () => loaderRef.current()
      .then(d => { if (alive) { setData(d); setError('') } })
      .catch(e => { if (alive) setError(e.message) })
    run()
    const id = intervalMs ? setInterval(run, intervalMs) : null
    return () => { alive = false; if (id) clearInterval(id) }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [...deps, tick])

  return { data, error, reload: () => setTick(t => t + 1), setData }
}

// ---- Tiện ích hiển thị ----
export const fmtTime = s => new Date(s).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })
export const fmtDate = s => new Date(s).toLocaleDateString('vi-VN')
export const fmtDateTime = s => `${fmtTime(s)} ${fmtDate(s)}`

/** Ngày theo giờ máy, dạng yyyy-MM-dd (định dạng mà API nhận). */
export function isoDate(d = new Date()) {
  const pad = n => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`
}
export const tomorrow = () => { const d = new Date(); d.setDate(d.getDate() + 1); return isoDate(d) }