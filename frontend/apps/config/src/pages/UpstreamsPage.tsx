import { useState, useCallback, useMemo } from "react";
import {
  Box, Button, Card, CardContent, Typography, IconButton,
  TextField, Chip, CircularProgress,
  Dialog, DialogTitle, DialogContent, DialogActions,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import EditIcon from "@mui/icons-material/Edit";
import DeleteIcon from "@mui/icons-material/Delete";
import SaveIcon from "@mui/icons-material/Save";
import CloudIcon from "@mui/icons-material/Cloud";
import { PageContainer, ConfirmDialog, useConfirm } from "@broker/ui-kit";
import { useUpstreams, useSaveUpstreams } from "../api/hooks";
import type { UpstreamEntry, UpstreamsMap } from "../api/types";

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
  const [upstreams, setUpstreams] = useState<UpstreamsMap | null>(null);
  const [editDialog, setEditDialog] = useState<{ open: boolean; name: string; entry: UpstreamEntry | null }>({ open: false, name: "", entry: null });
  const { confirm, confirmDialogProps } = useConfirm();

  const currentUpstreams = useMemo(() => upstreams ?? rawUpstreams ?? {}, [upstreams, rawUpstreams]);

  const handleSave = useCallback(async () => {
    try {
      await saveUpstreams.mutateAsync({ upstreams: currentUpstreams });
      setUpstreams(null);
    } catch { /* error toast via MutationCache */ }
  }, [currentUpstreams, saveUpstreams]);

  const handleAddUpstream = () => {
    setEditDialog({ open: true, name: "", entry: null });
  };

  const handleEditUpstream = (name: string) => {
    setEditDialog({ open: true, name, entry: currentUpstreams[name] });
  };

  const handleDeleteUpstream = async (name: string) => {
    const confirmed = await confirm({ title: "Delete Upstream", message: `Are you sure you want to delete upstream "${name}"?` });
    if (!confirmed) return;

    setUpstreams((prev) => {
      const map = { ...(prev ?? rawUpstreams ?? {}) };
      delete map[name];
      return map;
    });
  };

  const handleDialogSave = (name: string, entry: UpstreamEntry) => {
    setUpstreams((prev) => ({
      ...(prev ?? rawUpstreams ?? {}),
      [name]: entry,
    }));
  };

  const hasChanges = upstreams !== null;

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
      breadcrumbs={[{ label: "Configuration", to: "/config" }, { label: "Upstreams" }]}
      actions={
        <Box sx={{ display: "flex", gap: 1 }}>
          <Button variant="outlined" startIcon={<AddIcon />} onClick={handleAddUpstream}>
            Add Upstream
          </Button>
          <Button
            variant="contained"
            startIcon={saveUpstreams.isPending ? <CircularProgress size={18} color="inherit" /> : <SaveIcon />}
            onClick={handleSave}
            disabled={!hasChanges || saveUpstreams.isPending}
          >
            Save
          </Button>
        </Box>
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
                  <IconButton size="small" onClick={() => handleEditUpstream(name)}>
                    <EditIcon fontSize="small" />
                  </IconButton>
                  <IconButton size="small" color="error" onClick={() => handleDeleteUpstream(name)}>
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

      <UpstreamDialog
        open={editDialog.open}
        name={editDialog.name}
        entry={editDialog.entry}
        onClose={() => setEditDialog({ open: false, name: "", entry: null })}
        onSave={handleDialogSave}
      />
      <ConfirmDialog {...confirmDialogProps} />
    </PageContainer>
  );
}
