import { type ReactNode } from "react";
import {
  Box, Button, Chip, IconButton, Skeleton,
  Table, TableBody, TableCell, TableHead, TableRow, Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import EditIcon from "@mui/icons-material/Edit";
import DeleteIcon from "@mui/icons-material/Delete";

export interface Column<T> {
  header: string;
  render: (row: T) => ReactNode;
  width?: number;
}

interface Props<T extends { id: string; isActive: boolean }> {
  title: string;
  columns: Column<T>[];
  rows: T[];
  isLoading: boolean;
  onAdd: () => void;
  onEdit: (row: T) => void;
  onDelete: (row: T) => void;
}

export function ReferenceDataTable<T extends { id: string; isActive: boolean }>({
  title, columns, rows, isLoading, onAdd, onEdit, onDelete,
}: Props<T>) {
  return (
    <Box>
      <Box sx={{ display: "flex", justifyContent: "space-between", alignItems: "center", mb: 1 }}>
        <Typography variant="subtitle2">{title}</Typography>
        <Button size="small" startIcon={<AddIcon />} onClick={onAdd}>Add</Button>
      </Box>
      <Table size="small">
        <TableHead>
          <TableRow>
            {columns.map((col, i) => (
              <TableCell key={i} sx={{ fontWeight: 600, width: col.width }}>{col.header}</TableCell>
            ))}
            <TableCell sx={{ fontWeight: 600, width: 80 }}>Status</TableCell>
            <TableCell sx={{ width: 100 }} />
          </TableRow>
        </TableHead>
        <TableBody>
          {isLoading ? (
            Array.from({ length: 3 }).map((_, i) => (
              <TableRow key={i}>
                {columns.map((_, j) => <TableCell key={j}><Skeleton /></TableCell>)}
                <TableCell><Skeleton /></TableCell>
                <TableCell />
              </TableRow>
            ))
          ) : rows.length === 0 ? (
            <TableRow>
              <TableCell colSpan={columns.length + 2}>
                <Typography variant="body2" color="text.secondary" textAlign="center">No items</Typography>
              </TableCell>
            </TableRow>
          ) : (
            rows.map((row) => (
              <TableRow key={row.id}>
                {columns.map((col, j) => <TableCell key={j}>{col.render(row)}</TableCell>)}
                <TableCell>
                  <Chip label={row.isActive ? "Active" : "Inactive"} color={row.isActive ? "success" : "default"} size="small" />
                </TableCell>
                <TableCell sx={{ whiteSpace: "nowrap" }}>
                  <IconButton size="small" onClick={() => onEdit(row)}><EditIcon fontSize="small" /></IconButton>
                  <IconButton size="small" onClick={() => onDelete(row)} color="error"><DeleteIcon fontSize="small" /></IconButton>
                </TableCell>
              </TableRow>
            ))
          )}
        </TableBody>
      </Table>
    </Box>
  );
}
