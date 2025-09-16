import React from 'react'
import { BrowserRouter, Routes, Route, Link, useNavigate, useParams } from 'react-router-dom'
import { isAuthed, login, register, getCategories, getProducts, getProduct, getCart, addToCart, updateCartItem, deleteCartItem, createOrder, listOrders } from './api'
import Layout from './components/Layout'
import ProductCard from './components/ProductCard'
import Protected from './components/Protected'
import OrderDetail from './pages/OrderDetail'

function Home() {
  const [cats, setCats] = React.useState([])
  const [items, setItems] = React.useState([])
  const [loading, setLoading] = React.useState(true)
  const [error, setError] = React.useState(null)
  const [q, setQ] = React.useState('')
  const [cat, setCat] = React.useState('')

  React.useEffect(() => {
    Promise.all([getCategories(), getProducts({})])
      .then(([c, p]) => { setCats(c); setItems(p) })
      .catch(setError)
      .finally(() => setLoading(false))
  }, [])

  const search = async (e) => {
    e?.preventDefault()
    setLoading(true)
    try { const p = await getProducts({ q, categoryId: cat || undefined }); setItems(p) }
    catch (err) { setError(err) } finally { setLoading(false) }
  }

  return (
    <div className="stack-24">
      <section className="hero">
        <div className="space-between">
          <div>
            <h2 style={{ margin: 0 }}>Discover products</h2>
            <div className="subtle">Search and filter by category</div>
          </div>
        </div>
        <form onSubmit={search} className="row" style={{ gap: 10, marginTop: 14 }}>
          <input className="input" placeholder="Search products..." value={q} onChange={e => setQ(e.target.value)} />
          <select className="select" value={cat} onChange={e => setCat(e.target.value)}>
            <option value="">All Categories</option>
            {cats.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
          <button className="btn primary" type="submit">Search</button>
        </form>
      </section>

      {loading && <div className="row"><div className="spinner" /> <span className="muted">Loading...</span></div>}
      {error && <div className="card" style={{ borderColor: 'var(--danger)' }}>{String(error.message || error)}</div>}

      <div className="grid">
        {items.map(p => <ProductCard key={p.id} product={p} />)}
      </div>
    </div>
  )
}

function ProductDetail() {
  const { id } = useParams()
  const [p, setP] = React.useState(null)
  const [q, setQ] = React.useState(1)
  const [msg, setMsg] = React.useState('')
  React.useEffect(() => { getProduct(id).then(setP) }, [id])
  const add = async () => {
    try { await addToCart(Number(id), Number(q)); setMsg('Added to cart') } catch (e) { setMsg(String(e.message || e)) }
  }
  if (!p) return <div className="row"><div className="spinner" /> <span className="muted">Loading...</span></div>
  return (
    <div className="stack-16">
      <div className="space-between">
        <h2 style={{ margin: 0 }}>{p.name}</h2>
        <span className="badge">{p.stock} in stock</span>
      </div>
      <p className="muted">{p.description}</p>
      <div className="price" style={{ fontSize: 22 }}>${Number(p.price).toFixed(2)}</div>
      <div className="row">
        <input className="input" type="number" min={1} max={p.stock} value={q} onChange={e => setQ(e.target.value)} style={{ width: 120 }} />
        <button className="btn primary" onClick={add} disabled={p.stock < 1}>Add to cart</button>
      </div>
      {msg && <div className="card" style={{ borderColor: 'var(--border)' }}>{msg}</div>}
    </div>
  )
}

function Cart() {
  const [items, setItems] = React.useState([])
  const [loading, setLoading] = React.useState(true)
  const nav = useNavigate()
  const load = () => getCart().then(setItems).finally(() => setLoading(false))
  React.useEffect(() => { load() }, [])
  const total = items.reduce((s, it) => s + it.price * it.quantity, 0)
  const upd = async (id, q) => { await updateCartItem(id, q); load() }
  const del = async (id) => { await deleteCartItem(id); load() }
  return (
    <div className="stack-16">
      <h2 className="section-title">Your Cart</h2>
      {!isAuthed() && <p className="muted">Please <Link to="/login">login</Link> to use your cart.</p>}
      {loading && <div className="row"><div className="spinner" /> <span className="muted">Loading...</span></div>}
      {items.length === 0 && !loading ? <p className="muted">Cart is empty</p> : (
        <div className="card">
          <table className="data">
            <thead><tr><th>Product</th><th>Price</th><th>Qty</th><th>Subtotal</th><th></th></tr></thead>
            <tbody>
              {items.map(it => (
                <tr key={it.id}>
                  <td>{it.productName}</td>
                  <td>${Number(it.price).toFixed(2)}</td>
                  <td><input className="input" type="number" min={1} value={it.quantity} onChange={e => upd(it.id, Number(e.target.value))} style={{ width: 90 }} /></td>
                  <td>${(it.price * it.quantity).toFixed(2)}</td>
                  <td><button className="btn danger" onClick={() => del(it.id)}>Remove</button></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      <div className="space-between">
        <span className="total">Total: ${total.toFixed(2)}</span>
        <button className="btn primary" onClick={() => nav('/checkout')} disabled={items.length === 0}>Checkout</button>
      </div>
    </div>
  )
}

function Checkout() {
  const nav = useNavigate()
  const [form, setForm] = React.useState({ shippingName: '', shippingPhone: '', shippingAddress: '' })
  const [msg, setMsg] = React.useState('')
  const submit = async (e) => {
    e.preventDefault()
    try { const r = await createOrder(form); setMsg('Order created'); nav('/orders') }
    catch (e) { setMsg(String(e.message || e)) }
  }
  return (
    <div className="stack-16">
      <h2 className="section-title">Checkout</h2>
      <form onSubmit={submit} className="stack-16" style={{ maxWidth: 520 }}>
        <input className="input" placeholder="Full name" value={form.shippingName} onChange={e => setForm({ ...form, shippingName: e.target.value })} required />
        <input className="input" placeholder="Phone" value={form.shippingPhone} onChange={e => setForm({ ...form, shippingPhone: e.target.value })} required />
        <textarea className="textarea" placeholder="Address" value={form.shippingAddress} onChange={e => setForm({ ...form, shippingAddress: e.target.value })} required />
        <div className="row" style={{ justifyContent: 'flex-end' }}>
          <button className="btn primary" type="submit">Place Order</button>
        </div>
      </form>
      {msg && <div className="card">{msg}</div>}
    </div>
  )
}

function Login() {
  const nav = useNavigate()
  const [form, setForm] = React.useState({ email: '', password: '' })
  const [msg, setMsg] = React.useState('')
  const submit = async (e) => {
    e.preventDefault()
    try { await login(form); nav('/') } catch (e) { setMsg(String(e.message || e)) }
  }
  return (
    <div className="stack-16" style={{ maxWidth: 420 }}>
      <h2 className="section-title">Login</h2>
      <form onSubmit={submit} className="stack-16">
        <input className="input" type="email" placeholder="Email" value={form.email} onChange={e => setForm({ ...form, email: e.target.value })} required />
        <input className="input" type="password" placeholder="Password" value={form.password} onChange={e => setForm({ ...form, password: e.target.value })} required />
        <div className="row" style={{ justifyContent: 'flex-end' }}>
          <button className="btn primary" type="submit">Login</button>
        </div>
      </form>
      {msg && <div className="card" style={{ borderColor: 'var(--danger)' }}>{msg}</div>}
    </div>
  )
}

function Register() {
  const nav = useNavigate()
  const [form, setForm] = React.useState({ email: '', password: '', name: '' })
  const [msg, setMsg] = React.useState('')
  const submit = async (e) => {
    e.preventDefault()
    try { await register(form); nav('/') } catch (e) { setMsg(String(e.message || e)) }
  }
  return (
    <div className="stack-16" style={{ maxWidth: 420 }}>
      <h2 className="section-title">Create Account</h2>
      <form onSubmit={submit} className="stack-16">
        <input className="input" type="text" placeholder="Name" value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} required />
        <input className="input" type="email" placeholder="Email" value={form.email} onChange={e => setForm({ ...form, email: e.target.value })} required />
        <input className="input" type="password" placeholder="Password" value={form.password} onChange={e => setForm({ ...form, password: e.target.value })} required />
        <div className="row" style={{ justifyContent: 'flex-end' }}>
          <button className="btn primary" type="submit">Create Account</button>
        </div>
      </form>
      {msg && <div className="card" style={{ borderColor: 'var(--danger)' }}>{msg}</div>}
    </div>
  )
}

function Orders() {
  const [items, setItems] = React.useState([])
  const [loading, setLoading] = React.useState(true)
  React.useEffect(() => { listOrders().then(setItems).finally(() => setLoading(false)) }, [])
  return (
    <div className="stack-16">
      <h2 className="section-title">Your Orders</h2>
      {loading && <div className="row"><div className="spinner" /> <span className="muted">Loading...</span></div>}
      <div className="stack-16">
        {items.map(o => (
          <div key={o.id} className="card space-between">
            <div>
              <div><b>Order #{o.id}</b></div>
              <div className="muted">{new Date(o.createdAt).toLocaleString()}</div>
            </div>
            <div className="row" style={{ gap: 12 }}>
              <span className="badge">{o.status}</span>
              <span className="price">${Number(o.total).toFixed(2)}</span>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}

export default function App() {
  return (
    <BrowserRouter>
      <Layout>
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/products/:id" element={<ProductDetail />} />
          <Route path="/cart" element={<Protected><Cart /></Protected>} />
          <Route path="/checkout" element={<Protected><Checkout /></Protected>} />
          <Route path="/login" element={<Login />} />
          <Route path="/register" element={<Register />} />
          <Route path="/orders" element={<Protected><Orders /></Protected>} />
          <Route path="/orders/:id" element={<Protected><OrderDetail /></Protected>} />
          <Route path="*" element={<div className="muted">404 — Page not found</div>} />
        </Routes>
      </Layout>
    </BrowserRouter>
  )
}
