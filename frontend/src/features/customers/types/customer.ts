export interface CustomerDto {
  id: number;
  code: string;
  name: string;
  taxId: string | null;
  categoryName: string | null;
  isActive: boolean;
  createdAtUtc: string;
}

export interface CustomerDetailDto {
  id: number;
  code: string;
  name: string;
  taxId: string | null;
  categoryName: string | null;
  categoryId: number | null;
  isActive: boolean;
  notes: string | null;
  createdAtUtc: string;
  addresses: CustomerAddressDto[];
  phones: CustomerPhoneDto[];
  emails: CustomerEmailDto[];
  accounts: CustomerAccountDto[];
}

export interface CustomerAddressDto {
  id: number;
  addressType: string;
  streetLine1: string;
  streetLine2: string | null;
  city: string;
  stateProvince: string | null;
  postalCode: string;
  countryCode: string;
  isDefault: boolean;
  createdAtUtc: string;
}

export interface CustomerPhoneDto {
  id: number;
  phoneType: string;
  phoneNumber: string;
  extension: string | null;
  isPrimary: boolean;
  createdAtUtc: string;
}

export interface CustomerEmailDto {
  id: number;
  emailType: string;
  emailAddress: string;
  isPrimary: boolean;
  createdAtUtc: string;
}

export interface CustomerAccountDto {
  id: number;
  currencyCode: string;
  balance: number;
  description: string | null;
  isPrimary: boolean;
  createdAtUtc: string;
}

export interface CustomerCategoryDto {
  id: number;
  name: string;
  description: string | null;
  createdAtUtc: string;
}

export interface CreateCustomerRequest {
  name: string;
  code?: string | null;
  taxId?: string | null;
  categoryId?: number | null;
  notes?: string | null;
}

export interface UpdateCustomerRequest {
  name: string;
  taxId?: string | null;
  categoryId?: number | null;
  notes?: string | null;
}

export interface SearchCustomersRequest {
  name?: string | null;
  code?: string | null;
  taxId?: string | null;
  categoryId?: number | null;
  includeDeleted: boolean;
  sortBy?: string | null;
  sortDescending: boolean;
  page: number;
  pageSize: number;
}

export interface CreateCategoryRequest {
  name: string;
  description?: string | null;
}

export interface UpdateCategoryRequest {
  name: string;
  description?: string | null;
}

export interface CreateAddressRequest {
  addressType: string;
  streetLine1: string;
  streetLine2?: string | null;
  city: string;
  stateProvince?: string | null;
  postalCode: string;
  countryCode: string;
}

export interface UpdateAddressRequest {
  addressType: string;
  streetLine1: string;
  streetLine2?: string | null;
  city: string;
  stateProvince?: string | null;
  postalCode: string;
  countryCode: string;
  isDefault: boolean;
}

export interface CreatePhoneRequest {
  phoneType: string;
  phoneNumber: string;
  extension?: string | null;
}

export interface UpdatePhoneRequest {
  phoneType: string;
  phoneNumber: string;
  extension?: string | null;
  isPrimary: boolean;
}

export interface CreateEmailRequest {
  emailType: string;
  emailAddress: string;
}

export interface UpdateEmailRequest {
  emailType: string;
  emailAddress: string;
  isPrimary: boolean;
}

export interface CreateAccountRequest {
  currencyCode: string;
  description?: string | null;
}

export interface UpdateAccountRequest {
  description?: string | null;
  isPrimary: boolean;
}

export interface MergeAccountsRequest {
  sourceAccountId: number;
  targetAccountId: number;
}
