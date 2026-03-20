import { Typography, Box, Chip } from "@mui/material";
import type { FieldChangeDto, EntityChangeGroupDto } from "../api/types";
import { CHANGE_TYPE_COLORS, getFieldLabel, getEntityTypeLabel } from "./changeHistoryUtils";


export function FieldRow({ field, entityType }: { field: FieldChangeDto; entityType: string | null }) {
  const label = getFieldLabel(entityType, field.fieldName);
  const color = CHANGE_TYPE_COLORS[field.changeType] ?? "default";

  return (
    <Box sx={{ display: "flex", alignItems: "baseline", gap: 1, py: 0.25, pl: 2 }}>
      <Typography variant="body2" color="text.secondary" sx={{ minWidth: 140, flexShrink: 0 }}>
        {label}:
      </Typography>
      {field.changeType === "Created" && (
        <Typography variant="body2" color={`${color}.main`}>{field.newValue ?? "—"}</Typography>
      )}
      {field.changeType === "Deleted" && (
        <Typography variant="body2" sx={{ textDecoration: "line-through" }} color={`${color}.main`}>
          {field.oldValue ?? "—"}
        </Typography>
      )}
      {field.changeType === "Modified" && (
        <Typography variant="body2">
          <Box component="span" sx={{ textDecoration: "line-through", color: "text.secondary" }}>
            {field.oldValue ?? "—"}
          </Box>
          {" → "}
          <Box component="span" color={`${color}.main`}>{field.newValue ?? "—"}</Box>
        </Typography>
      )}
    </Box>
  );
}

export function ChangeGroup({ group }: { group: EntityChangeGroupDto }) {
  const entityLabel = group.relatedEntityType
    ? getEntityTypeLabel(group.relatedEntityType)
    : null;
  const displayName = group.relatedEntityDisplayName;
  const color = CHANGE_TYPE_COLORS[group.changeType] ?? "default";

  return (
    <Box sx={{ mb: 1 }}>
      {entityLabel && (
        <Box sx={{ display: "flex", alignItems: "center", gap: 1, mb: 0.5 }}>
          <Typography variant="body2" fontWeight={600}>
            {entityLabel}{displayName ? ` — ${displayName}` : ""}
          </Typography>
          <Chip label={group.changeType} color={color} size="small" variant="outlined" />
        </Box>
      )}
      {group.fields.map((f, i) => (
        <FieldRow key={i} field={f} entityType={group.relatedEntityType ?? null} />
      ))}
    </Box>
  );
}
