export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export const DEFAULT_PAGE_SIZE = 25;

export function totalPages(result: Pick<PagedResult<unknown>, 'pageSize' | 'totalCount'>): number {
  if (result.totalCount === 0) return 1;
  return Math.ceil(result.totalCount / result.pageSize);
}

export function pageRangeLabel(page: number, pageSize: number, totalCount: number): string {
  if (totalCount === 0) return 'No results';
  const start = (page - 1) * pageSize + 1;
  const end = Math.min(page * pageSize, totalCount);
  return `${start}–${end} of ${totalCount}`;
}
