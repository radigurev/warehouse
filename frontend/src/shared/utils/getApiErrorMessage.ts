import type { AxiosError } from 'axios';
import type { ProblemDetails } from '@shared/types/api';

export function getApiErrorMessage(
  err: unknown,
  t: (key: string) => string,
): string {
  const axiosError = err as AxiosError<ProblemDetails>;
  const status = axiosError?.response?.status;
  const data = axiosError?.response?.data;
  const errorCode = data?.title;

  // Known error code → try translation, then fall back to detail
  if (errorCode) {
    const key = `errors.${errorCode}`;
    const translated = t(key);
    if (translated !== key) {
      return translated;
    }
  }

  // 400 Validation errors — extract field-level messages from errors object
  if (status === 400 && data?.errors) {
    const messages: string[] = [];
    for (const field of Object.keys(data.errors)) {
      const fieldErrors = data.errors[field];
      if (Array.isArray(fieldErrors)) {
        messages.push(...fieldErrors);
      }
    }
    if (messages.length > 0) {
      return messages.join(' ');
    }
  }

  // Any response with a detail message — show it directly
  if (data?.detail) {
    return data.detail;
  }

  // Network error
  if (axiosError?.code === 'ERR_NETWORK') {
    return t('errors.NETWORK_ERROR');
  }

  // 401 → session expired
  if (status === 401) {
    return t('errors.TOKEN_EXPIRED');
  }

  // 403 → forbidden
  if (status === 403) {
    return t('errors.FORBIDDEN');
  }

  // 404 → not found
  if (status === 404) {
    return t('pageTitle.notFoundMessage');
  }

  // 409 → conflict, detail should have been caught above
  if (status === 409 && errorCode) {
    return errorCode;
  }

  // True unexpected: 500 or no response
  return t('errors.UNEXPECTED_ERROR');
}
