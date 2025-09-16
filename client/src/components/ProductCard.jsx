import React from 'react'
import { Link } from 'react-router-dom'

export default function ProductCard({ product }) {
  return (
    <div className="card">
      {product.imageUrl ? (
        <img alt={product.name} src={product.imageUrl} style={{ width: '100%', height: 140, objectFit: 'cover', borderRadius: 10, border: '1px solid var(--border)' }} />
      ) : (
        <div style={{ height: 140, borderRadius: 10, border: '1px solid var(--border)', display: 'grid', placeItems: 'center', color: 'var(--muted)' }}>
          No image
        </div>
      )}
      <h3 title={product.name}>{product.name}</h3>
      <div className="row" style={{ justifyContent: 'space-between' }}>
        <span className="price">${Number(product.price).toFixed(2)}</span>
        <span className="badge">{product.stock} in stock</span>
      </div>
      <p className="muted" style={{ minHeight: 38 }}>{product.description?.slice(0, 72)}</p>
      <Link className="btn ghost" to={`/products/${product.id}`}>View</Link>
    </div>
  )
}

