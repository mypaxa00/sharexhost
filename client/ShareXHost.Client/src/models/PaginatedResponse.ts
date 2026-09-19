export interface PaginatedResponse<T> {
    page: number;
    pageSize: number;
    totalCount: number;
    items: T[];
}