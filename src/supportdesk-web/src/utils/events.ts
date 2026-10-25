/** Fired after notification read state changes so the shell can refresh its badge. */
export const notificationsChangedEvent = 'supportdesk:notifications-changed'

export function notifyNotificationsChanged(): void {
  window.dispatchEvent(new Event(notificationsChangedEvent))
}
