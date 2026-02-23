import { useState } from "react";
import {
  Dialog, DialogTitle, DialogContent, DialogActions, Button,
  Accordion, AccordionSummary, AccordionDetails,
  Typography, Box, Chip, CircularProgress, Pagination, Divider,
} from "@mui/material";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import { useEntityChanges } from "../api/hooks";
import type { OperationDto } from "../api/types";
import {
  CHANGE_TYPE_COLORS, getEntityTypeLabel,
  FieldRow, ChangeGroup,
} from "./ChangeHistoryComponents";

interface Props {
  entityType: string;
  entityId: string;
  open: boolean;
  onClose: () => void;
}

function OperationItem({ op, entityType }: { op: OperationDto; entityType: string }) {
  const ts = new Date(op.timestamp);
  const dateStr = ts.toLocaleDateString(undefined, { year: "numeric", month: "short", day: "numeric" });
  const timeStr = ts.toLocaleTimeString(undefined, { hour: "2-digit", minute: "2-digit", second: "2-digit" });
  const color = CHANGE_TYPE_COLORS[op.changeType] ?? "default";
  const entityLabel = getEntityTypeLabel(entityType) || entityType;
  const displayName = op.entityDisplayName;

  const rootChanges = op.changes.filter((c) => !c.relatedEntityType);
  const relatedChanges = op.changes.filter((c) => c.relatedEntityType);

  return (
    <Accordion disableGutters variant="outlined" sx={{ "&:before": { display: "none" } }}>
      <AccordionSummary expandIcon={<ExpandMoreIcon />}>
        <Box sx={{ display: "flex", alignItems: "center", gap: 1, width: "100%" }}>
          <Typography variant="body2" fontWeight={600}>{dateStr}, {timeStr}</Typography>
          <Typography variant="body2" color="text.secondary">— {op.userName ?? "system"}</Typography>
          <Chip label={op.changeType} color={color} size="small" sx={{ ml: "auto" }} />
        </Box>
      </AccordionSummary>
      <AccordionDetails sx={{ pt: 0 }}>
        {rootChanges.length > 0 && (
          <Box sx={{ mb: 1 }}>
            <Typography variant="body2" fontWeight={600} sx={{ mb: 0.5 }}>
              {entityLabel}{displayName ? ` — ${displayName}` : ""}
            </Typography>
            {rootChanges.flatMap((g) =>
              g.fields.map((f, i) => <FieldRow key={i} field={f} entityType={entityType} />)
            )}
          </Box>
        )}

        {rootChanges.length > 0 && relatedChanges.length > 0 && <Divider sx={{ my: 1 }} />}

        {relatedChanges.map((g, i) => (
          <ChangeGroup key={i} group={g} />
        ))}
      </AccordionDetails>
    </Accordion>
  );
}

export function EntityHistoryDialog({ entityType, entityId, open, onClose }: Props) {
  const [page, setPage] = useState(1);
  const pageSize = 10;

  const { data, isLoading } = useEntityChanges(
    { entityType, entityId, page, pageSize },
    open && !!entityId,
  );

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>Change History</DialogTitle>
      <DialogContent sx={{ minHeight: 200 }}>
        {isLoading ? (
          <Box sx={{ display: "flex", justifyContent: "center", mt: 4 }}>
            <CircularProgress />
          </Box>
        ) : !data || data.items.length === 0 ? (
          <Typography color="text.secondary" sx={{ mt: 2 }}>No changes recorded.</Typography>
        ) : (
          <Box sx={{ display: "flex", flexDirection: "column", gap: 1 }}>
            {data.items.map((op) => (
              <OperationItem key={op.operationId} op={op} entityType={entityType} />
            ))}
          </Box>
        )}

        {data && data.totalPages > 1 && (
          <Box sx={{ display: "flex", justifyContent: "center", mt: 2 }}>
            <Pagination
              count={data.totalPages}
              page={page}
              onChange={(_, p) => setPage(p)}
              size="small"
            />
          </Box>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Close</Button>
      </DialogActions>
    </Dialog>
  );
}
