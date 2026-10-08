interface PaginationProps {
  page: number
  totalPages: number
  totalCount: number
  onPageChange: (page: number) => void
}

export function Pagination({ page, totalPages, totalCount, onPageChange }: PaginationProps) {
  const lastPage = Math.max(1, totalPages)
  return (
    <nav className="pagination" aria-label="Pagination">
      <span className="muted">
        {totalCount.toLocaleString('en-GB')} entries · page {page} of {lastPage}
      </span>
      <div className="pagination-buttons">
        <button type="button" className="button" onClick={() => onPageChange(1)} disabled={page <= 1}>
          First
        </button>
        <button type="button" className="button" onClick={() => onPageChange(page - 1)} disabled={page <= 1}>
          Previous
        </button>
        <button type="button" className="button" onClick={() => onPageChange(page + 1)} disabled={page >= lastPage}>
          Next
        </button>
      </div>
    </nav>
  )
}
