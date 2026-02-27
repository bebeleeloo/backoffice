import { useState, useMemo } from "react";
import { useParams, Link as RouterLink } from "react-router-dom";
import {
  Box, Button, Card, CardContent, Checkbox, Chip, CircularProgress,
  Divider, FormControlLabel, Link, Typography,
} from "@mui/material";
import EditIcon from "@mui/icons-material/Edit";
import HistoryIcon from "@mui/icons-material/History";
import { useRole, usePermissions } from "../api/hooks";
import type { PermissionDto } from "../api/types";
import { useHasPermission } from "../auth/usePermission";
import { EditRoleDialog } from "./RoleDialogs";
import { EntityHistoryDialog } from "../components/EntityHistoryDialog";
import { PageContainer } from "../components/PageContainer";

function Field({ label, value }: { label: string; value: React.ReactNode }) {
  if (value === null || value === undefined || value === "") return null;
  return (
    <Box sx={{ minWidth: 180 }}>
      <Typography variant="caption" color="text.secondary">{label}</Typography>
      <Typography variant="body2">{value}</Typography>
    </Box>
  );
}

function groupPermissions(allPerms: PermissionDto[]) {
  return allPerms.reduce<Record<string, PermissionDto[]>>((acc, p) => {
    (acc[p.group] ??= []).push(p);
    return acc;
  }, {});
}

export function RoleDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const { data: role, isLoading } = useRole(id ?? "");
  const { data: allPermissions = [] } = usePermissions();
  const canUpdate = useHasPermission("roles.update");
  const canAudit = useHasPermission("audit.read");
  const [editOpen, setEditOpen] = useState(false);
  const [historyOpen, setHistoryOpen] = useState(false);

  const breadcrumbs = useMemo(() => [
    { label: "Roles", to: "/roles" },
    { label: role?.name ?? "" },
  ], [role?.name]);

  if (isLoading) {
    return (
      <Box sx={{ display: "flex", justifyContent: "center", mt: 8 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (!role) {
    return (
      <Box sx={{ p: 3 }}>
        <Typography>Role not found.</Typography>
        <Typography sx={{ mt: 1 }}>
          <Link component={RouterLink} to="/roles">Return to Roles list</Link>
        </Typography>
      </Box>
    );
  }

  const grouped = groupPermissions(allPermissions);
  const rolePermCodes = new Set(role.permissions);

  return (
    <PageContainer
      title={role.name}
      breadcrumbs={breadcrumbs}
      actions={
        <Box sx={{ display: "flex", gap: 1 }}>
          {canAudit && (
            <Button startIcon={<HistoryIcon />} onClick={() => setHistoryOpen(true)}>History</Button>
          )}
          {canUpdate && (
            <Button variant="contained" startIcon={<EditIcon />} onClick={() => setEditOpen(true)}>Edit</Button>
          )}
        </Box>
      }
    >
      {/* General */}
      <Card>
        <CardContent>
          <Typography variant="subtitle1" gutterBottom>General</Typography>
          <Box sx={{ display: "flex", flexWrap: "wrap", gap: 3 }}>
            <Field label="Name" value={role.name} />
            <Field label="Description" value={role.description} />
            <Field label="Type" value={
              <Chip
                label={role.isSystem ? "System" : "Custom"}
                color={role.isSystem ? "warning" : "default"}
                size="small"
              />
            } />
            <Field label="Created" value={new Date(role.createdAt).toLocaleString()} />
          </Box>
        </CardContent>
      </Card>

      {/* Permissions */}
      <Card>
        <CardContent>
          <Typography variant="subtitle1" gutterBottom>
            Permissions ({role.permissions.length})
          </Typography>
          {Object.entries(grouped).map(([group, perms]) => (
            <Box key={group} sx={{ mb: 2 }}>
              <Typography variant="subtitle2" color="text.secondary" sx={{ textTransform: "capitalize" }}>{group}</Typography>
              <Divider sx={{ mb: 1 }} />
              {perms.map((p) => (
                <FormControlLabel
                  key={p.id}
                  control={<Checkbox checked={rolePermCodes.has(p.code)} disabled size="small" />}
                  label={<Typography variant="body2">{p.name}</Typography>}
                />
              ))}
            </Box>
          ))}
          {allPermissions.length === 0 && (
            <Typography variant="body2" color="text.secondary">No permissions available</Typography>
          )}
        </CardContent>
      </Card>

      <EditRoleDialog open={editOpen} onClose={() => setEditOpen(false)} role={role} />
      <EntityHistoryDialog entityType="Role" entityId={role.id} open={historyOpen} onClose={() => setHistoryOpen(false)} />
    </PageContainer>
  );
}
