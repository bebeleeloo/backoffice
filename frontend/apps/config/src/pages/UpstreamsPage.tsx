import { useState, useMemo } from "react";
import {
  Box, Button, Card, CardContent, Typography, IconButton,
  TextField, Chip, CircularProgress,
  Dialog, DialogTitle, DialogContent, DialogActions,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import EditIcon from "@mui/icons-material/Edit";
import DeleteIcon from "@mui/icons-material/Delete";
import CloudIcon from "@mui/icons-material/Cloud";
import HistoryIcon from "@mui/icons-material/History";
import { ConfirmDialog, PageContainer, useConfirm, EntityHistoryDialog, useHasPermission } from "@broker/ui-kit";
import { useUpstreams, useSaveUpstreams } from "../api/hooks";
import type { UpstreamEntry } from "../api/types";

interface UpstreamDialogProps {
  open: boolean;
  name: string;
  entry: UpstreamEntry | null;
  onClose: () => void;
  onSave: (name: string, entry: UpstreamEntry) => void;
}

function UpstreamDialog({ open, name: initialName, entry, onClose, onSave }: UpstreamDialogProps) {
  const [name, setName] = useState(initialName);
  const [address, setAddress] = useState(entry?.address ?? "");
  const [routes, setRoutes] = useState(entry?.routes?.join("\n") ?? "");

  const handleSave = () => {
    onSave(name, {
      address,
      routes: routes.split("\n").map((r) => r.trim()).filter(Boolean),
    });
    onClose();
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>{entry ? "Edit Upstream" : "Add Upstream"}</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "16px !important" }}>
        <TextField
          label="Name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          size="small"
          required
          disabled={!!entry}
          InputLabelProps={{ shrink: true }}
        />
        <TextField
          label="Address"
          value={address}
          onChange={(e) => setAddress(e.target.value)}
          size="small"
          required
          placeholder="http://service:8080"
          InputLabelProps={{ shrink: true }}
        />
        <TextField
          label="Routes"
          value={routes}
          onChange={(e) => setRoutes(e.target.value)}
          size="small"
          multiline
          rows={6}
          placeholder={"/api/v1/example\n/api/v1/another"}
          helperText="One route per line"
          InputLabelProps={{ shrink: true }}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" onClick={handleSave} disabled={!name || !address}>Save</Button>
      </DialogActions>
    </Dialog>
  );
}

export function UpstreamsPage() {
  const { data: rawUpstreams, isLoading } = useUpstreams();
  const saveUpstreams = useSaveUpstreams();
  const [editDialog, setEditDialog] = useState<{ open: boolean; name: string; entry: UpstreamEntry | null }>({ open: false, name: "", entry: null });
  const [historyOpen, setHistoryOpen] = useState(false);
  const [historyUpstream, setHistoryUpstream] = useState<string | null>(null);
  const canAudit = useHasPermission("audit.read");
  const { confirm, confirmDialogProps } = useConfirm();

  const currentUpstreams = useMemo(() => rawUpstreams ?? {}, [rawUpstreams]);

  const handleAddUpstream = () => {
    setEditDialog({ open: true, name: "", entry: null });
  };

  const handleEditUpstream = (name: string) => {
    setEditDialog({ open: true, name, entry: currentUpstreams[name] });
  };

  const handleDeleteUpstream = async (name: string) => {
    const confirmed = await confirm({ title: "Delete Upstream", message: `Are you sure you want to delete upstream "${name}"?` });
    if (!confirmed) return;

    const map = { ...currentUpstreams };
    delete map[name];
    try {
      await saveUpstreams.mutateAsync({ upstreams: map });
    } catch { /* error toast via MutationCache */ }
  };

  const handleDialogSave = async (name: string, entry: UpstreamEntry) => {
    const map = { ...currentUpstreams, [name]: entry };
    try {
      await saveUpstreams.mutateAsync({ upstreams: map });
    } catch { /* error toast via MutationCache */ }
  };

  if (isLoading) {
    return (
      <Box sx={{ display: "flex", justifyContent: "center", py: 8 }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <PageContainer
      title="Upstreams"
      actions={
        <>
          {canAudit && (
            <Button variant="outlined" startIcon={<HistoryIcon />} onClick={() => setHistoryOpen(true)}>
              History
            </Button>
          )}
          <Button variant="outlined" startIcon={<AddIcon />} onClick={handleAddUpstream} disabled={saveUpstreams.isPending}>
            Add Upstream
          </Button>
        </>
      }
    >
      <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", md: "repeat(2, 1fr)" }, gap: 3 }}>
        {Object.entries(currentUpstreams).map(([name, entry]) => (
          <Card key={name}>
            <CardContent>
              <Box sx={{ display: "flex", alignItems: "center", justifyContent: "space-between", mb: 2 }}>
                <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
                  <CloudIcon color="primary" />
                  <Typography variant="h6" fontWeight={600}>{name}</Typography>
                </Box>
                <Box>
                  {canAudit && (
                    <IconButton size="small" onClick={() => setHistoryUpstream(name)} disabled={saveUpstreams.isPending}>
                      <HistoryIcon fontSize="small" />
                    </IconButton>
                  )}
                  <IconButton size="small" onClick={() => handleEditUpstream(name)} disabled={saveUpstreams.isPending}>
                    <EditIcon fontSize="small" />
                  </IconButton>
                  <IconButton size="small" color="error" onClick={() => handleDeleteUpstream(name)} disabled={saveUpstreams.isPending}>
                    <DeleteIcon fontSize="small" />
                  </IconButton>
                </Box>
              </Box>
              <Typography variant="body2" color="text.secondary" gutterBottom>
                {entry.address}
              </Typography>
              <Box sx={{ display: "flex", flexWrap: "wrap", gap: 0.5, mt: 1 }}>
                {entry.routes.map((route) => (
                  <Chip key={route} label={route} size="small" variant="outlined" sx={{ fontFamily: "monospace", fontSize: "0.75rem" }} />
                ))}
              </Box>
            </CardContent>
          </Card>
        ))}
      </Box>

      {editDialog.open && (
        <UpstreamDialog
          open
          name={editDialog.name}
          entry={editDialog.entry}
          onClose={() => setEditDialog({ open: false, name: "", entry: null })}
          onSave={handleDialogSave}
        />
      )}
      <ConfirmDialog {...confirmDialogProps} />
      <EntityHistoryDialog entityType="UpstreamsConfig" entityId="config" open={historyOpen} onClose={() => setHistoryOpen(false)} />
      <EntityHistoryDialog entityType="UpstreamsConfig" entityId="config" open={historyUpstream !== null} onClose={() => setHistoryUpstream(null)} filterRelatedEntityId={historyUpstream ?? undefined} />
    </PageContainer>
  );
}
