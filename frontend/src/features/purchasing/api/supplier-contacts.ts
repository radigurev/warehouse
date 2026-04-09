import apiClient from '@shared/api/client';
import type {
  SupplierAddressDto,
  CreateSupplierAddressRequest,
  UpdateSupplierAddressRequest,
  SupplierPhoneDto,
  CreateSupplierPhoneRequest,
  UpdateSupplierPhoneRequest,
  SupplierEmailDto,
  CreateSupplierEmailRequest,
  UpdateSupplierEmailRequest,
} from '@features/purchasing/types/purchasing';

export function createSupplierAddress(supplierId: number, request: CreateSupplierAddressRequest): Promise<SupplierAddressDto> {
  return apiClient.post<SupplierAddressDto>(`/suppliers/${supplierId}/addresses`, request).then((r) => r.data);
}

export function updateSupplierAddress(supplierId: number, addressId: number, request: UpdateSupplierAddressRequest): Promise<SupplierAddressDto> {
  return apiClient.put<SupplierAddressDto>(`/suppliers/${supplierId}/addresses/${addressId}`, request).then((r) => r.data);
}

export function deleteSupplierAddress(supplierId: number, addressId: number): Promise<void> {
  return apiClient.delete(`/suppliers/${supplierId}/addresses/${addressId}`).then(() => undefined);
}

export function createSupplierPhone(supplierId: number, request: CreateSupplierPhoneRequest): Promise<SupplierPhoneDto> {
  return apiClient.post<SupplierPhoneDto>(`/suppliers/${supplierId}/phones`, request).then((r) => r.data);
}

export function updateSupplierPhone(supplierId: number, phoneId: number, request: UpdateSupplierPhoneRequest): Promise<SupplierPhoneDto> {
  return apiClient.put<SupplierPhoneDto>(`/suppliers/${supplierId}/phones/${phoneId}`, request).then((r) => r.data);
}

export function deleteSupplierPhone(supplierId: number, phoneId: number): Promise<void> {
  return apiClient.delete(`/suppliers/${supplierId}/phones/${phoneId}`).then(() => undefined);
}

export function createSupplierEmail(supplierId: number, request: CreateSupplierEmailRequest): Promise<SupplierEmailDto> {
  return apiClient.post<SupplierEmailDto>(`/suppliers/${supplierId}/emails`, request).then((r) => r.data);
}

export function updateSupplierEmail(supplierId: number, emailId: number, request: UpdateSupplierEmailRequest): Promise<SupplierEmailDto> {
  return apiClient.put<SupplierEmailDto>(`/suppliers/${supplierId}/emails/${emailId}`, request).then((r) => r.data);
}

export function deleteSupplierEmail(supplierId: number, emailId: number): Promise<void> {
  return apiClient.delete(`/suppliers/${supplierId}/emails/${emailId}`).then(() => undefined);
}
