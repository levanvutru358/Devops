const BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5193'

function authHeader() {
  const token = localStorage.getItem('token')
  return token ? { Authorization: `Bearer ${token}` } : {}
}

function signalCartChanged() {
  try { window.dispatchEvent(new CustomEvent('cart:changed')) } catch {}
}

async function request(path, options = {}) {
  const res = await fetch(`${BASE_URL}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(options.headers || {}),
    },
  })
  if (!res.ok) {
    let msg = `API ${res.status}`
    try { const t = await res.text(); msg = t || msg } catch {}
    throw new Error(msg)
  }
  const ct = res.headers.get('content-type') || ''
  return ct.includes('application/json') ? res.json() : res.text()
}

// Auth
export async function register(data) {
  const resp = await request('/api/auth/register', { method: 'POST', body: JSON.stringify(data) })
  localStorage.setItem('token', resp.token)
  return resp
}
export async function login(data) {
  const resp = await request('/api/auth/login', { method: 'POST', body: JSON.stringify(data) })
  localStorage.setItem('token', resp.token)
  return resp
}
export function logout() {
  localStorage.removeItem('token')
}
export function isAuthed() { return !!localStorage.getItem('token') }

// Catalog
export function getCategories() { return request('/api/categories') }
export function getProducts(params = {}) {
  const qs = new URLSearchParams()
  if (params.page) qs.set('page', params.page)
  if (params.pageSize) qs.set('pageSize', params.pageSize)
  if (params.categoryId) qs.set('categoryId', params.categoryId)
  if (params.q) qs.set('q', params.q)
  const s = qs.toString()
  return request(`/api/products${s ? `?${s}` : ''}`)
}
export function getProduct(id) { return request(`/api/products/${id}`) }

// Cart
export function getCart() { return request('/api/cart', { headers: { ...authHeader() } }) }
export function addToCart(productId, quantity) {
  return request('/api/cart/items', { method: 'POST', headers: { ...authHeader() }, body: JSON.stringify({ productId, quantity }) })
    .then(r => { signalCartChanged(); return r })
}
export function updateCartItem(itemId, quantity) {
  return request(`/api/cart/items/${itemId}`, { method: 'PUT', headers: { ...authHeader() }, body: JSON.stringify({ quantity }) })
    .then(r => { signalCartChanged(); return r })
}
export function deleteCartItem(itemId) {
  return request(`/api/cart/items/${itemId}`, { method: 'DELETE', headers: { ...authHeader() } })
    .then(r => { signalCartChanged(); return r })
}

// Orders
export function createOrder(payload) {
  return request('/api/orders', { method: 'POST', headers: { ...authHeader() }, body: JSON.stringify(payload) })
    .then(r => { signalCartChanged(); return r })
}
export function listOrders() { return request('/api/orders', { headers: { ...authHeader() } }) }
export function getOrder(id) { return request(`/api/orders/${id}`, { headers: { ...authHeader() } }) }

// Minimal JWT decode (no verify): returns payload object or null
export function getUserInfo() {
  try {
    const token = localStorage.getItem('token')
    if (!token) return null
    const parts = token.split('.')
    if (parts.length < 2) return null
    const json = atob(parts[1].replace(/-/g, '+').replace(/_/g, '/'))
    return JSON.parse(json)
  } catch { return null }
}
