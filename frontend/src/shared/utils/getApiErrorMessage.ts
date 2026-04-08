import type { AxiosError } from 'axios';
import type { ProblemDetails } from '@shared/types/api';

export function getApiErrorMessage(
  err: unknown,
  t: (key: string) => string,
): string {
  const axiosError = err as AxiosError<ProblemDetails>;
  const errorCode = axiosError?.response?.data?.title;

  if (errorCode) {
    const key = `errors.${errorCode}`;
    const translated = t(key);
    if (translated !== key) {
      return translated;
    }
    const detail = axiosError?.response?.data?.detail;
    if (detail) {
      return detail;
    }
  }

  if (axiosError?.code === 'ERR_NETWORK') {
    return t('errors.NETWORK_ERROR');
  }

  return t('errors.UNEXPECTED_ERROR');
}
