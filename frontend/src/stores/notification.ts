import { defineStore } from 'pinia';
import { ref } from 'vue';

export type NotificationType = 'success' | 'error' | 'warning' | 'info';

export interface Notification {
  message: string;
  type: NotificationType;
  timeout: number;
}

export const useNotificationStore = defineStore('notification', () => {
  const visible = ref(false);
  const message = ref('');
  const type = ref<NotificationType>('info');
  const timeout = ref(4000);

  function show(notification: Notification): void {
    message.value = notification.message;
    type.value = notification.type;
    timeout.value = notification.timeout;
    visible.value = true;
  }

  function success(msg: string): void {
    show({ message: msg, type: 'success', timeout: 4000 });
  }

  function error(msg: string): void {
    show({ message: msg, type: 'error', timeout: 6000 });
  }

  function warning(msg: string): void {
    show({ message: msg, type: 'warning', timeout: 5000 });
  }

  function info(msg: string): void {
    show({ message: msg, type: 'info', timeout: 4000 });
  }

  function hide(): void {
    visible.value = false;
  }

  return {
    visible,
    message,
    type,
    timeout,
    show,
    success,
    error,
    warning,
    info,
    hide,
  };
});
