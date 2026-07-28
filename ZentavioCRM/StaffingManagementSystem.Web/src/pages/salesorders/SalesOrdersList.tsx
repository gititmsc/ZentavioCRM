import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { salesOrderService, type SalesOrderListItem, type SalesOrderStatus } from "@/services/salesOrderService";

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

  const [orders, setOrders] = useState<SalesOrderListItem[]>([]);
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState<SalesOrderStatus | "">("");
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const pageSize = 20;

  const load = async (searchTerm: string, statusFilter: SalesOrderStatus | "", pageNumber: number) => {
    setIsLoading(true);
    const result = await salesOrderService.search({
      search: searchTerm || undefined,
      status: statusFilter || undefined,
      page: pageNumber,
      pageSize,
    });
    setIsLoading(false);
    if (!result.success || !result.data) {
      setError(result.message || "Unable to load sales orders.");
      return;
    }
    setOrders(result.data.items);
    setTotalCount(result.data.totalCount);
  };

  useEffect(() => {
    load(search, status, page);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, status]);

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setPage(1);
    load(search, status, 1);
  };

  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  return (
    <div>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h1 className="h4 mb-0">Sales Orders</h1>
      </div>

      <div className="d-flex gap-2 mb-3">
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
            setPage(1);
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

      {error && <div className="alert alert-danger">{error}</div>}

      <div className="card shadow-sm border-0">
        <div className="table-responsive">
          <table className="table table-hover align-middle mb-0">
            <thead className="table-light">
              <tr>
                <th>Number</th>
                <th>Quotation</th>
                <th>Customer</th>
                <th>Total</th>
                <th>Order Date</th>
                <th>Expected Delivery</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {isLoading && (
                <tr>
                  <td colSpan={7} className="text-center text-muted py-4">
                    Loading...
                  </td>
                </tr>
              )}
              {!isLoading && orders.length === 0 && (
                <tr>
                  <td colSpan={7} className="text-center text-muted py-4">
                    No sales orders found. Convert an accepted quotation to create one.
                  </td>
                </tr>
              )}
              {orders.map((order) => (
                <tr key={order.id} role="button" onClick={() => navigate(`/sales-orders/${order.id}`)}>
                  <td>{order.salesOrderNumber}</td>
                  <td>{order.quotationNumber}</td>
                  <td>{order.customerName}</td>
                  <td>{order.grandTotal.toLocaleString()}</td>
                  <td>{new Date(order.orderDate).toLocaleDateString()}</td>
                  <td>{order.expectedDeliveryDate ? new Date(order.expectedDeliveryDate).toLocaleDateString() : "—"}</td>
                  <td>
                    <span className={`badge ${STATUS_BADGE[order.status]}`}>{order.status}</span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {totalPages > 1 && (
        <div className="d-flex justify-content-between align-items-center mt-3">
          <span className="text-muted small">
            Page {page} of {totalPages} &middot; {totalCount} total
          </span>
          <div className="btn-group">
            <button
              type="button"
              className="btn btn-outline-secondary btn-sm"
              disabled={page <= 1}
              onClick={() => setPage((p) => p - 1)}
            >
              Previous
            </button>
            <button
              type="button"
              className="btn btn-outline-secondary btn-sm"
              disabled={page >= totalPages}
              onClick={() => setPage((p) => p + 1)}
            >
              Next
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
