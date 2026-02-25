import {
  Dialog, DialogTitle, DialogContent, DialogActions, Button,
  Typography, Box, Chip, Divider,
} from "@mui/material";
import type { GlobalOperationDto } from "../api/types";
import { CHANGE_TYPE_COLORS, getEntityTypeLabel } from "./changeHistoryUtils";
import { FieldRow, ChangeGroup } from "./ChangeHistoryComponents";

interface Props {
  operation: GlobalOperationDto | null;
  open: boolean;
  onClose: () => void;
}

export function AuditDetailDialog({ operation, open, onClose }: Props) {
  if (!operation) return null;

  const ts = new Date(operation.timestamp);
  const dateStr = ts.toLocaleDateString(undefined, { year: "numeric", month: "short", day: "numeric" });
  const timeStr = ts.toLocaleTimeString(undefined, { hour: "2-digit", minute: "2-digit", second: "2-digit" });
  const color = CHANGE_TYPE_COLORS[operation.changeType] ?? "default";
  const entityLabel = getEntityTypeLabel(operation.entityType) || operation.entityType;

  const rootChanges = operation.changes.filter((c) => !c.relatedEntityType);
  const relatedChanges = operation.changes.filter((c) => c.relatedEntityType);

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle sx={{ display: "flex", alignItems: "center", gap: 1, flexWrap: "wrap" }}>
        <Typography variant="h6" component="span">Change Details</Typography>
        <Chip label={operation.changeType} color={color} size="small" sx={{ ml: "auto" }} />
      </DialogTitle>
      <DialogContent>
        <Box sx={{ display: "flex", flexDirection: "column", gap: 1, mb: 2 }}>
          <Box sx={{ display: "flex", gap: 2, flexWrap: "wrap" }}>
            <Typography variant="body2" color="text.secondary">
              <strong>Date:</strong> {dateStr}, {timeStr}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              <strong>User:</strong> {operation.userName ?? "system"}
            </Typography>
          </Box>
          <Typography variant="body2" color="text.secondary">
            <strong>Entity:</strong> {entityLabel}
            {operation.entityDisplayName ? ` — "${operation.entityDisplayName}"` : ""}
          </Typography>
        </Box>

        <Divider sx={{ mb: 2 }} />

        {rootChanges.length > 0 && (
          <Box sx={{ mb: 2 }}>
            <Typography variant="subtitle2" sx={{ mb: 1 }}>
              {entityLabel} fields
            </Typography>
            {rootChanges.flatMap((g) =>
              g.fields.map((f, i) => <FieldRow key={i} field={f} entityType={operation.entityType} />)
            )}
          </Box>
        )}

        {rootChanges.length > 0 && relatedChanges.length > 0 && <Divider sx={{ my: 1 }} />}

        {relatedChanges.map((g, i) => (
          <ChangeGroup key={i} group={g} />
        ))}

        {rootChanges.length === 0 && relatedChanges.length === 0 && (
          <Typography color="text.secondary">No field changes recorded.</Typography>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Close</Button>
      </DialogActions>
    </Dialog>
  );
}
