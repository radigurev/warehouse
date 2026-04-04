import apiClient from '@shared/api/client';
import type {
  CustomerAddressDto,
  CustomerPhoneDto,
  CustomerEmailDto,
  CustomerAccountDto,
  CreateAddressRequest,
  UpdateAddressRequest,
  CreatePhoneRequest,
  UpdatePhoneRequest,
  CreateEmailRequest,
  UpdateEmailRequest,
  CreateAccountRequest,
  UpdateAccountRequest,
  MergeAccountsRequest,
} from '@features/customers/types/customer';

// --- Addresses ---

export function getAddresses(customerId: number): Promise<CustomerAddressDto[]> {
  return apiClient.get<CustomerAddressDto[]>(`/customers/${customerId}/addresses`).then((r) => r.data);
}

export function createAddress(customerId: number, request: CreateAddressRequest): Promise<CustomerAddressDto> {
  return apiClient.post<CustomerAddressDto>(`/customers/${customerId}/addresses`, request).then((r) => r.data);
}

export function updateAddress(customerId: number, addressId: number, request: UpdateAddressRequest): Promise<CustomerAddressDto> {
  return apiClient.put<CustomerAddressDto>(`/customers/${customerId}/addresses/${addressId}`, request).then((r) => r.data);
}

export function deleteAddress(customerId: number, addressId: number): Promise<void> {
  return apiClient.delete(`/customers/${customerId}/addresses/${addressId}`).then(() => undefined);
}

// --- Phones ---

export function getPhones(customerId: number): Promise<CustomerPhoneDto[]> {
  return apiClient.get<CustomerPhoneDto[]>(`/customers/${customerId}/phones`).then((r) => r.data);
}

export function createPhone(customerId: number, request: CreatePhoneRequest): Promise<CustomerPhoneDto> {
  return apiClient.post<CustomerPhoneDto>(`/customers/${customerId}/phones`, request).then((r) => r.data);
}

export function updatePhone(customerId: number, phoneId: number, request: UpdatePhoneRequest): Promise<CustomerPhoneDto> {
  return apiClient.put<CustomerPhoneDto>(`/customers/${customerId}/phones/${phoneId}`, request).then((r) => r.data);
}

export function deletePhone(customerId: number, phoneId: number): Promise<void> {
  return apiClient.delete(`/customers/${customerId}/phones/${phoneId}`).then(() => undefined);
}

// --- Emails ---

export function getEmails(customerId: number): Promise<CustomerEmailDto[]> {
  return apiClient.get<CustomerEmailDto[]>(`/customers/${customerId}/emails`).then((r) => r.data);
}

export function createEmail(customerId: number, request: CreateEmailRequest): Promise<CustomerEmailDto> {
  return apiClient.post<CustomerEmailDto>(`/customers/${customerId}/emails`, request).then((r) => r.data);
}

export function updateEmail(customerId: number, emailId: number, request: UpdateEmailRequest): Promise<CustomerEmailDto> {
  return apiClient.put<CustomerEmailDto>(`/customers/${customerId}/emails/${emailId}`, request).then((r) => r.data);
}

export function deleteEmail(customerId: number, emailId: number): Promise<void> {
  return apiClient.delete(`/customers/${customerId}/emails/${emailId}`).then(() => undefined);
}

// --- Accounts ---

export function getAccounts(customerId: number): Promise<CustomerAccountDto[]> {
  return apiClient.get<CustomerAccountDto[]>(`/customers/${customerId}/accounts`).then((r) => r.data);
}

export function createAccount(customerId: number, request: CreateAccountRequest): Promise<CustomerAccountDto> {
  return apiClient.post<CustomerAccountDto>(`/customers/${customerId}/accounts`, request).then((r) => r.data);
}

export function updateAccount(customerId: number, accountId: number, request: UpdateAccountRequest): Promise<CustomerAccountDto> {
  return apiClient.put<CustomerAccountDto>(`/customers/${customerId}/accounts/${accountId}`, request).then((r) => r.data);
}

export function deactivateAccount(customerId: number, accountId: number): Promise<void> {
  return apiClient.delete(`/customers/${customerId}/accounts/${accountId}`).then(() => undefined);
}

export function mergeAccounts(customerId: number, request: MergeAccountsRequest): Promise<CustomerAccountDto> {
  return apiClient.post<CustomerAccountDto>(`/customers/${customerId}/accounts/merge`, request).then((r) => r.data);
}
