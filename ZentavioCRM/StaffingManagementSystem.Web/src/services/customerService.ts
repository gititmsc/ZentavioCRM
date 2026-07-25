/**
 * Customers API — thin wrapper around ZentavioCRM.Api's CustomersController.
 */
import { apiClient } from "@/services/apiClient";
import { callApi } from "@/services/apiHelpers";
import type { PagedResult } from "@/services/leadService";

export type CustomerType =
  | "Prospect"
  | "Individual"
  | "Business"
  | "Vendor"
  | "Partner"
  | "Supplier"
  | "Distributor"
  | "Dealer"
  | "Franchise"
  | "Consultant";

export type AddressType = "Billing" | "Shipping" | "RegisteredOffice" | "BranchOffice" | "Warehouse" | "Site";

export interface CustomerListItem {
  id: string;
  customerNumber: string;
  type: CustomerType;
  displayName: string;
  industry: string | null;
  email: string | null;
  phone: string | null;
  assignedToUserName: string | null;
  isActive: boolean;
  createdAtUtc: string;
}

export interface ContactPerson {
  id: string;
  firstName: string;
  lastName: string;
  designation: string | null;
  department: string | null;
  email: string | null;
  mobile: string | null;
  whatsApp: string | null;
  linkedIn: string | null;
  isPrimary: boolean;
  isDecisionMaker: boolean;
  notes: string | null;
}

export interface CustomerAddress {
  id: string;
  type: AddressType;
  line1: string;
  line2: string | null;
  city: string | null;
  state: string | null;
  country: string | null;
  postalCode: string | null;
  isPrimary: boolean;
}

export interface Customer {
  id: string;
  customerNumber: string;
  type: CustomerType;
  legalName: string;
  displayName: string;
  industry: string | null;
  website: string | null;
  email: string | null;
  phone: string | null;
  taxNumber: string | null;
  employeesCount: number | null;
  annualRevenue: number | null;
  currencyCode: string;
  paymentTermsDays: number | null;
  creditLimit: number | null;
  rating: string | null;
  assignedToUserId: string | null;
  assignedToUserName: string | null;
  isActive: boolean;
  createdAtUtc: string;
  contacts: ContactPerson[];
  addresses: CustomerAddress[];
}

export interface SaveCustomerRequest {
  type: CustomerType;
  legalName: string;
  displayName: string;
  industry: string | null;
  website: string | null;
  email: string | null;
  phone: string | null;
  taxNumber: string | null;
  employeesCount: number | null;
  annualRevenue: number | null;
  currencyCode: string;
  paymentTermsDays: number | null;
  creditLimit: number | null;
  rating: string | null;
  assignedToUserId: string | null;
  isActive: boolean;
  contacts: Omit<ContactPerson, "id">[];
  addresses: Omit<CustomerAddress, "id">[];
}

export interface CustomerSearchParams {
  search?: string;
  assignedToUserId?: string;
  isActive?: boolean;
  page?: number;
  pageSize?: number;
}

const search = (params: CustomerSearchParams) =>
  callApi<PagedResult<CustomerListItem>>(apiClient.get("/api/customers", { params }));

const getById = (id: string) => callApi<Customer>(apiClient.get(`/api/customers/${id}`));

const create = (request: SaveCustomerRequest) => callApi<Customer>(apiClient.post("/api/customers", request));

const update = (id: string, request: SaveCustomerRequest) =>
  callApi<Customer>(apiClient.put(`/api/customers/${id}`, request));

const remove = (id: string) => callApi<boolean>(apiClient.delete(`/api/customers/${id}`));

export const customerService = { search, getById, create, update, remove };
