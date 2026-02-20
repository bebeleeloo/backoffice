/**
 * Lightweight mock of FilteredDataGrid for unit tests.
 * Renders a plain div with row count and column info — no real DataGrid, no
 * virtualization, no ResizeObserver, no layout measurements.
 */
export function FilteredDataGrid(props: {
  rows: { id: string; [k: string]: unknown }[];
  columns: { field: string; headerName?: string; renderCell?: (p: { row: unknown; value: unknown }) => unknown }[];
  filterDefs?: Map<string, () => unknown>;
  [k: string]: unknown;
}) {
  const { rows, columns, filterDefs } = props;
  return (
    <div data-testid="datagrid">
      <div data-testid="datagrid-info">rows: {rows.length}</div>
      <div data-testid="datagrid-columns">
        {columns.map((c) => (
          <span key={c.field} data-testid={`col-${c.field}`}>
            {c.headerName ?? c.field}
          </span>
        ))}
      </div>
      {rows.map((row) => (
        <div key={row.id} data-testid={`row-${row.id}`}>
          {columns.map((col) => {
            if (col.renderCell) {
              return (
                <span key={col.field} data-testid={`cell-${row.id}-${col.field}`}>
                  {col.renderCell({ row, value: row[col.field] }) as React.ReactNode}
                </span>
              );
            }
            return (
              <span key={col.field} data-testid={`cell-${row.id}-${col.field}`}>
                {String(row[col.field] ?? "")}
              </span>
            );
          })}
        </div>
      ))}
      {filterDefs && (
        <div data-testid="datagrid-filters">
          {Array.from(filterDefs.entries()).map(([field, render]) => (
            <div key={field} data-testid={`filter-${field}`}>
              {render() as React.ReactNode}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
