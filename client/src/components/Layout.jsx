import React from 'react'
import { Link, NavLink, useNavigate, useLocation } from 'react-router-dom'
import { isAuthed, logout, getCart, getUserInfo } from '../api'

export default function Layout({ children }) {
  const nav = useNavigate()
  const loc = useLocation()
  const authed = isAuthed()
  const onLogout = () => { logout(); nav('/login') }
  const [cartCount, setCartCount] = React.useState(0)
  const user = getUserInfo()

  const refreshCart = React.useCallback(() => {
    if (!isAuthed()) { setCartCount(0); return }
    getCart().then(items => setCartCount(items.reduce((s, it) => s + Number(it.quantity || 0), 0))).catch(() => {})
  }, [])

  React.useEffect(() => { refreshCart() }, [refreshCart, loc.pathname])
  React.useEffect(() => {
    const handler = () => refreshCart()
    window.addEventListener('cart:changed', handler)
    return () => window.removeEventListener('cart:changed', handler)
  }, [refreshCart])
  return (
    <div>
      <header className="app-header">
        <div className="container nav">
          <div className="brand"><Link to="/">Emo Shop</Link></div>
          <nav className="nav-links">
            <NavLink to="/" end>Shop</NavLink>
            <NavLink to="/cart">Cart{cartCount ? ` (${cartCount})` : ''}</NavLink>
            {authed ? (
              <>
                <NavLink to="/orders">Orders</NavLink>
                <span className="badge" title={user?.name || user?.email}>Hi, {user?.name || user?.email || 'User'}</span>
                <button onClick={onLogout}>Logout</button>
              </>
            ) : (
              <>
                <NavLink to="/login">Login</NavLink>
                <NavLink to="/register">Register</NavLink>
              </>
            )}
          </nav>
        </div>
      </header>

      <main className="container" style={{ paddingTop: 20, paddingBottom: 40 }}>
        {children}
      </main>

      <footer className="footer">
        <div className="container subtle">© {new Date().getFullYear()} Emo Shop</div>
      </footer>
    </div>
  )
}
