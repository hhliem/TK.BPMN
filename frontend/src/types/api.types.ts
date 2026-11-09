/**
 * Standard Result<T> structure returned from Enterprise BPM Modular Monolith API
 */
export interface Result<T = unknown> {
  isSuccess: boolean;
  value?: T;
  error?: string;
  validationErrors?: Record<string, string[]>;
}

export interface ApiError {
  status: number;
  message: string;
  correlationId?: string;
  errors?: Record<string, string[]>;
}

export interface PaginatedList<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface PaginationParams {
  pageNumber?: number;
  pageSize?: number;
  searchTerm?: string;
  sortBy?: string;
  isAscending?: boolean;
}
