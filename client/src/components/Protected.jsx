import React from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { isAuthed } from '../api'

export default function Protected({ children }) {
  const loc = useLocation()
  if (!isAuthed()) return <Navigate to="/login" state={{ from: loc.pathname }} replace />
  return children
}

