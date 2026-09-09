import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "@/context/AuthContext";
import { productService, type Product, type ProductSearchParams, type ProductType } from "@/services/productService";
import { PermissionCodes } from "@/services/permissionCodes";
import { PageHeader } from "@/components/layout/PageHeader";
import { DataTable, type DataTableColumn } from "@/components/datatable/DataTable";
import { Pagination } from "@/components/datatable/Pagination";
import { usePagedList } from "@/hooks/usePagedList";

const currencyFormatter = new Intl.NumberFormat(undefined, { style: "currency", currency: "USD" });

export default function ProductsList() {
  const navigate = useNavigate();
  const { hasPermission } = useAuth();
  const canCreate = hasPermission(PermissionCodes.ProductsCreate);
  const canEdit = hasPermission(PermissionCodes.ProductsEdit);
  const canDelete = hasPermission(PermissionCodes.ProductsDelete);

  const [search, setSearch] = useState("");
  const [type, setType] = useState<ProductType | "">("");

  const {
    items: products,
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
    reload,
  } = usePagedList<Product, ProductSearchParams>(
    productService.search,
    ({ page, pageSize, sortBy, sortDescending }) => ({
      search: search || undefined,
      type: type || undefined,
      page,
      pageSize,
      sortBy,
      sortDescending,
    }),
    [search, type],
    { defaultSortDescending: false }
  );

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    resetToFirstPage();
  };

  const handleDelete = async (product: Product) => {
    if (!window.confirm(`Delete "${product.name}" from the catalog?`)) {
      return;
    }
    const result = await productService.remove(product.id);
    if (!result.success) {
      window.alert(result.message || "Unable to delete this catalog item.");
      return;
    }
    reload();
  };

  const columns: DataTableColumn<Product>[] = [
    { key: "sku", header: "SKU", render: (p) => <span className="font-monospace">{p.sku}</span> },
    { key: "name", header: "Name", render: (p) => p.name },
    {
      key: "type",
      header: "Type",
      render: (p) => (
        <span className={`badge ${p.type === "Service" ? "text-bg-info" : "text-bg-secondary"}`}>{p.type}</span>
      ),
    },
    {
      key: "category",
      header: "Category",
      render: (p) => p.category ?? <span className="text-muted">&mdash;</span>,
    },
    {
      key: "unitprice",
      header: "Unit Price",
      align: "end",
      render: (p) => currencyFormatter.format(p.unitPrice),
    },
    {
      key: "isactive",
      header: "Status",
      render: (p) => (
        <span className={`badge ${p.isActive ? "text-bg-success" : "text-bg-secondary"}`}>
          {p.isActive ? "Active" : "Inactive"}
        </span>
      ),
    },
    ...(canEdit || canDelete
      ? [
          {
            header: "",
            align: "end" as const,
            render: (product: Product) => (
              <>
                {canEdit && (
                  <Link
                    to={`/products/${product.id}/edit`}
                    className="btn btn-sm btn-outline-secondary me-2"
                    onClick={(e) => e.stopPropagation()}
                  >
                    Edit
                  </Link>
                )}
                {canDelete && (
                  <button
                    type="button"
                    className="btn btn-sm btn-outline-danger"
                    onClick={(e) => {
                      e.stopPropagation();
                      handleDelete(product);
                    }}
                  >
                    Delete
                  </button>
                )}
              </>
            ),
          },
        ]
      : []),
  ];

  return (
    <div>
      <PageHeader
        title="Product Catalog"
        subtitle="Products and services your team can attach to opportunities and quotations."
        actions={
          canCreate && (
            <button type="button" className="btn btn-primary" onClick={() => navigate("/products/new")}>
              <i className="bi bi-plus-lg me-1" aria-hidden="true" />
              New Catalog Item
            </button>
          )
        }
      />

      <div className="card shadow-sm border-0 p-3 mb-3">
        <form className="d-flex gap-2 flex-wrap" onSubmit={handleSearchSubmit}>
          <input
            className="form-control"
            style={{ maxWidth: 320 }}
            placeholder="Search by SKU, name, category, brand..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
          <select
            className="form-select"
            style={{ maxWidth: 180 }}
            value={type}
            onChange={(e) => {
              setType(e.target.value as ProductType | "");
              resetToFirstPage();
            }}
          >
            <option value="">All Types</option>
            <option value="Product">Product</option>
            <option value="Service">Service</option>
          </select>
          <button type="submit" className="btn btn-outline-secondary">
            <i className="bi bi-search" aria-hidden="true" />
          </button>
        </form>
      </div>

      {error && <div className="alert alert-danger">{error}</div>}

      <DataTable
        columns={columns}
        items={products}
        rowKey={(p) => p.id}
        isLoading={isLoading}
        emptyMessage="No catalog items yet."
        emptyIcon="bi-box-seam"
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
