import { useState, useCallback } from "react";
import { IconButton, CircularProgress, Tooltip } from "@mui/material";
import FileDownloadIcon from "@mui/icons-material/FileDownload";
import { useSnackbar } from "notistack";
import { type ExcelColumn, exportToExcel } from "../utils/exportToExcel";

interface Props<T> {
  fetchData: () => Promise<T[]>;
  columns: ExcelColumn<T>[];
  filename: string;
}

export function ExportButton<T>({ fetchData, columns, filename }: Props<T>) {
  const [loading, setLoading] = useState(false);
  const { enqueueSnackbar } = useSnackbar();

  const handleExport = useCallback(async () => {
    setLoading(true);
    try {
      const data = await fetchData();
      await exportToExcel(data, columns, filename);
    } catch {
      enqueueSnackbar("Failed to export data", { variant: "error" });
    } finally {
      setLoading(false);
    }
  }, [fetchData, columns, filename, enqueueSnackbar]);

  return (
    <Tooltip title="Export to Excel">
      <span>
        <IconButton size="small" onClick={handleExport} disabled={loading}>
          {loading ? <CircularProgress size={20} /> : <FileDownloadIcon />}
        </IconButton>
      </span>
    </Tooltip>
  );
}
