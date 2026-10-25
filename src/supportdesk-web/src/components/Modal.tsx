import { useEffect, useRef, type ReactNode } from 'react'

interface ModalProps {
  title: string
  description?: string
  onClose: () => void
  children: ReactNode
}

/** Lightweight accessible dialog used for admin actions instead of browser prompts. */
export default function Modal({ title, description, onClose, children }: ModalProps) {
  const containerRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        onClose()
      }
    }

    document.addEventListener('keydown', handleKeyDown)
    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    const focusTarget = containerRef.current?.querySelector<HTMLElement>('input, select, textarea')
    ;(focusTarget ?? containerRef.current?.querySelector<HTMLElement>('button'))?.focus()

    return () => {
      document.removeEventListener('keydown', handleKeyDown)
      document.body.style.overflow = previousOverflow
    }
  }, [onClose])

  return (
    <div
      className="modal-backdrop"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget) {
          onClose()
        }
      }}
    >
      <div className="modal" role="dialog" aria-modal="true" aria-label={title} ref={containerRef}>
        <div className="modal-header">
          <div>
            <h2>{title}</h2>
            {description ? <p className="muted small">{description}</p> : null}
          </div>
          <button type="button" className="button small" onClick={onClose} aria-label="Close dialog">
            Close
          </button>
        </div>
        {children}
      </div>
    </div>
  )
}
