// TC-02: gửi NHIỀU yêu cầu đặt cùng một khung giờ CÙNG LÚC -> chỉ đúng 1 yêu cầu thành công (201), còn lại 409.
// Cách chạy:  node tools/concurrency-test.mjs <slotId> [soLuong]
// Yêu cầu: API (5080) và Keycloak (8080) đang chạy; tài khoản benhnhan1 / 123456.
const API = process.env.API_URL || 'http://localhost:5080'
const TOKEN_URL = 'http://localhost:8080/realms/hlt01/protocol/openid-connect/token'
const slotId = Number(process.argv[2])
const n = Number(process.argv[3] || 10)
if (!slotId) { console.log('Cách dùng: node tools/concurrency-test.mjs <slotId> [soLuong]'); process.exit(1) }

const res = await fetch(TOKEN_URL, {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: new URLSearchParams({ grant_type: 'password', client_id: 'hlt-web', username: 'benhnhan1', password: '123456' }),
})
const { access_token } = await res.json()

const results = await Promise.all(Array.from({ length: n }, () =>
    fetch(`${API}/api/appointments`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${access_token}` },
        body: JSON.stringify({ slotId, reason: 'Kiểm thử đồng thời' }),
    }).then(r => r.status)))

const count = s => results.filter(x => x === s).length
console.log('Kết quả:', results.join(' '))
console.log(`201 Created: ${count(201)}  |  409 Conflict: ${count(409)}  |  khác: ${n - count(201) - count(409)}`)
console.log(count(201) === 1 ? 'ĐẠT: chỉ một lịch hẹn được tạo.' : 'KHÔNG ĐẠT: kiểm tra lại rowversion / chỉ mục duy nhất.')