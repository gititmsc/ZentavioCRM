import { useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  salesOrderService,
  type SalesOrderListItem,
  type SalesOrderSearchParams,
  type SalesOrderStatus,
} from "@/services/salesOrderService";
import { PageHeader } from "@/components/layout/PageHeader";
import { DataTable, type DataTableColumn } from "@/components/datatable/DataTable";
import { Pagination } from "@/components/datatable/Pagination";
import { usePagedList } from "@/hooks/usePagedList";

const STATUSES: SalesOrderStatus[] = ["Draft", "Confirmed", "PartiallyDelivered", "Delivered", "Cancelled"];

const STATUS_BADGE: Record<SalesOrderStatus, string> = {
  Draft: "text-bg-secondary",
  Confirmed: "text-bg-info",
  PartiallyDelivered: "text-bg-warning",
  Delivered: "text-bg-success",
  Cancelled: "text-bg-danger",
};

export default function SalesOrdersList() {
  const navigate = useNavigate();

  const [search, setSearch] = useState("");
  const [status, setStatus] = useState<SalesOrderStatus | "">("");

  const {
    items: orders,
    totalCount,
    totalPages,
    page,
    pageSize,
    sortBy,
    sortDescending,
    isLoading,
    error,
    setPage,
    setPageSize,
    onSortChange,
    resetToFirstPage,
  } = usePagedList<SalesOrderListItem, SalesOrderSearchParams>(
    salesOrderService.search,
    ({ page, pageSize, sortBy, sortDescending }) => ({
      search: search || undefined,
      status: status || undefined,
      page,
      pageSize,
      sortBy,
      sortDescending,
    }),
    [search, status]
  );

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    resetToFirstPage();
  };

  const columns: DataTableColumn<SalesOrderListItem>[] = [
    { key: "salesOrderNumber", header: "Number", render: (o) => o.salesOrderNumber },
    { key: "quotationNumber", header: "Quotation", render: (o) => o.quotationNumber },
    { key: "customerName", header: "Customer", render: (o) => o.customerName },
    { key: "grandTotal", header: "Total", align: "end", render: (o) => o.grandTotal.toLocaleString() },
    { key: "orderDate", header: "Order Date", render: (o) => new Date(o.orderDate).toLocaleDateString() },
    {
      key: "expectedDeliveryDate",
      header: "Expected Delivery",
      render: (o) => (o.expectedDeliveryDate ? new Date(o.expectedDeliveryDate).toLocaleDateString() : <span className="text-muted">&mdash;</span>),
    },
    {
      key: "status",
      header: "Status",
      render: (o) => <span className={`badge ${STATUS_BADGE[o.status]}`}>{o.status}</span>,
    },
  ];

  return (
    <div>
      <PageHeader title="Sales Orders" subtitle="Confirmed orders and delivery tracking." />

      <div className="card shadow-sm border-0 p-3 mb-3">
        <div className="d-flex gap-2">
          <form className="d-flex" style={{ maxWidth: 320 }} onSubmit={handleSearchSubmit}>
            <input
              className="form-control me-2"
              placeholder="Search sales orders..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
            <button type="submit" className="btn btn-outline-secondary">
              <i className="bi bi-search" aria-hidden="true" />
            </button>
          </form>

          <select
            className="form-select"
            style={{ maxWidth: 200 }}
            value={status}
            onChange={(e) => {
              setStatus(e.target.value as SalesOrderStatus | "");
              resetToFirstPage();
            }}
          >
            <option value="">All statuses</option>
            {STATUSES.map((s) => (
              <option key={s} value={s}>
                {s}
              </option>
            ))}
          </select>
        </div>
      </div>

      {error && <div className="alert alert-danger">{error}</div>}

      <DataTable
        columns={columns}
        items={orders}
        rowKey={(o) => o.id}
        isLoading={isLoading}
        emptyMessage="No sales orders found. Convert an accepted quotation to create one."
        emptyIcon="bi-cart-check"
        onRowClick={(o) => navigate(`/sales-orders/${o.id}`)}
        sortBy={sortBy}
        sortDescending={sortDescending}
        onSortChange={onSortChange}
      />

      <Pagination
        page={page}
        pageSize={pageSize}
        totalCount={totalCount}
        totalPages={totalPages}
        onPageChange={setPage}
        onPageSizeChange={setPageSize}
      />
    </div>
  );
}
