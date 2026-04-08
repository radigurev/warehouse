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

  if (errorCode) {
    const key = `errors.${errorCode}`;
    const translated = t(key);
    if (translated !== key) {
      return translated;
    }
    if (data?.detail) {
      return data.detail;
    }
  }

  if (axiosError?.code === 'ERR_NETWORK') {
    return t('errors.NETWORK_ERROR');
  }

  if (!status || status >= 500) {
    return t('errors.UNEXPECTED_ERROR');
  }

  return data?.detail || t('errors.UNEXPECTED_ERROR');
}
