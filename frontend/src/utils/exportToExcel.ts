import ExcelJS from "exceljs";
import { saveAs } from "file-saver";

export interface ExcelColumn<T> {
  header: string;
  value: (row: T) => string | number | boolean | null | undefined;
}

export async function exportToExcel<T>(
  data: T[],
  columns: ExcelColumn<T>[],
  filename: string,
): Promise<void> {
  const wb = new ExcelJS.Workbook();
  const ws = wb.addWorksheet("Data");

  // Add header row
  const headerRow = ws.addRow(columns.map((c) => c.header));
  headerRow.eachCell((cell) => {
    cell.font = { bold: true };
    cell.fill = {
      type: "pattern",
      pattern: "solid",
      fgColor: { argb: "FFD9E1F2" },
    };
    cell.alignment = { horizontal: "center" };
  });

  // Add data rows
  for (const row of data) {
    ws.addRow(
      columns.map((col) => {
        const v = col.value(row);
        return v === null || v === undefined ? "" : v;
      }),
    );
  }

  // Auto-fit column widths
  ws.columns.forEach((col, i) => {
    let max = columns[i].header.length;
    for (const row of data) {
      const len = String(columns[i].value(row) ?? "").length;
      if (len > max) max = len;
    }
    col.width = Math.min(max + 2, 50);
  });

  const buf = await wb.xlsx.writeBuffer();
  saveAs(
    new Blob([buf], { type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" }),
    `${filename}.xlsx`,
  );
}
