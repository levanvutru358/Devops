import React from 'react'
import { useParams } from 'react-router-dom'
import { getOrder } from '../api'

export default function OrderDetail() {
  const { id } = useParams()
  const [items, setItems] = React.useState([])
  const [loading, setLoading] = React.useState(true)
  const [err, setErr] = React.useState('')
  React.useEffect(() => {
    getOrder(id).then(setItems).catch(e => setErr(String(e.message || e))).finally(() => setLoading(false))
  }, [id])
  return (
    <div className="stack-16">
      <h2 className="section-title">Order #{id}</h2>
      {loading && <div className="row"><div className="spinner" /> <span className="muted">Loading...</span></div>}
      {err && <div className="card" style={{ borderColor: 'var(--danger)' }}>{err}</div>}
      {items.length > 0 && (
        <div className="card">
          <table className="data">
            <thead><tr><th>Product</th><th>Price</th><th>Qty</th><th>Subtotal</th></tr></thead>
            <tbody>
              {items.map((it, i) => (
                <tr key={i}>
                  <td>{it.productName}</td>
                  <td>${Number(it.price).toFixed(2)}</td>
                  <td>{it.quantity}</td>
                  <td>${(Number(it.price) * it.quantity).toFixed(2)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}

