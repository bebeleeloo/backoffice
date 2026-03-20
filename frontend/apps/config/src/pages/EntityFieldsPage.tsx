import { useState, useCallback, useMemo } from "react";
import {
  Box, Button, Card, CardContent, Typography, IconButton,
  Accordion, AccordionSummary, AccordionDetails,
  Table, TableHead, TableBody, TableRow, TableCell, Checkbox,
  TextField, CircularProgress,
} from "@mui/material";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import AddIcon from "@mui/icons-material/Add";
import DeleteIcon from "@mui/icons-material/Delete";
import SaveIcon from "@mui/icons-material/Save";
import { PageContainer } from "@broker/ui-kit";
import { useEntitiesRaw, useSaveEntities } from "../api/hooks";
import type { EntityConfig, EntityFieldConfig } from "../api/types";

const KNOWN_ROLES = ["Manager", "Operator", "Viewer"];

export function EntityFieldsPage() {
  const { data: rawEntities, isLoading } = useEntitiesRaw();
  const saveEntities = useSaveEntities();
  const [entities, setEntities] = useState<EntityConfig[] | null>(null);
  const [newFieldNames, setNewFieldNames] = useState<Record<string, string>>({});

  const currentEntities = useMemo(() => entities ?? rawEntities ?? [], [entities, rawEntities]);

  const handleToggleRole = (entityIdx: number, fieldIdx: number, role: string) => {
    setEntities((prev) => {
      const list = [...(prev ?? rawEntities ?? [])].map((e) => ({
        ...e,
        fields: e.fields.map((f) => ({ ...f, roles: [...f.roles] })),
      }));
      const field = list[entityIdx].fields[fieldIdx];
      if (role === "*") {
        field.roles = field.roles.includes("*") ? [] : ["*"];
      } else {
        if (field.roles.includes("*")) {
          field.roles = KNOWN_ROLES.filter((r) => r !== role);
        } else if (field.roles.includes(role)) {
          field.roles = field.roles.filter((r) => r !== role);
        } else {
          field.roles = [...field.roles, role];
          if (KNOWN_ROLES.every((r) => field.roles.includes(r))) {
            field.roles = ["*"];
          }
        }
      }
      return list;
    });
  };

  const handleAddField = (entityIdx: number) => {
    const entityName = currentEntities[entityIdx].name;
    const fieldName = newFieldNames[entityName]?.trim();
    if (!fieldName) return;

    setEntities((prev) => {
      const list = [...(prev ?? rawEntities ?? [])].map((e) => ({
        ...e,
        fields: [...e.fields.map((f) => ({ ...f, roles: [...f.roles] }))],
      }));
      list[entityIdx].fields.push({ name: fieldName, roles: ["*"] });
      return list;
    });
    setNewFieldNames((prev) => ({ ...prev, [entityName]: "" }));
  };

  const handleRemoveField = (entityIdx: number, fieldIdx: number) => {
    setEntities((prev) => {
      const list = [...(prev ?? rawEntities ?? [])].map((e) => ({
        ...e,
        fields: e.fields.map((f) => ({ ...f, roles: [...f.roles] })),
      }));
      list[entityIdx].fields.splice(fieldIdx, 1);
      return list;
    });
  };

  const handleSave = useCallback(async () => {
    try {
      await saveEntities.mutateAsync({ entities: currentEntities });
      setEntities(null);
    } catch { /* error toast via MutationCache */ }
  }, [currentEntities, saveEntities]);

  const hasChanges = entities !== null;

  const isRoleChecked = (field: EntityFieldConfig, role: string): boolean => {
    if (field.roles.includes("*")) return true;
    return field.roles.includes(role);
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
      title="Entity Fields"
      breadcrumbs={[{ label: "Configuration", to: "/config" }, { label: "Entity Fields" }]}
      actions={
        <Button
          variant="contained"
          startIcon={saveEntities.isPending ? <CircularProgress size={18} color="inherit" /> : <SaveIcon />}
          onClick={handleSave}
          disabled={!hasChanges || saveEntities.isPending}
        >
          Save
        </Button>
      }
    >
      {currentEntities.map((entity, entityIdx) => (
        <Accordion key={entity.name} defaultExpanded>
          <AccordionSummary expandIcon={<ExpandMoreIcon />}>
            <Typography fontWeight={600}>{entity.name}</Typography>
            <Typography variant="body2" color="text.secondary" sx={{ ml: 2 }}>
              {entity.fields.length} fields
            </Typography>
          </AccordionSummary>
          <AccordionDetails sx={{ p: 0 }}>
            <Card variant="outlined" sx={{ border: 0 }}>
              <CardContent sx={{ p: 0, "&:last-child": { pb: 0 } }}>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>Field</TableCell>
                      <TableCell align="center">All (*)</TableCell>
                      {KNOWN_ROLES.map((role) => (
                        <TableCell key={role} align="center">{role}</TableCell>
                      ))}
                      <TableCell align="center" width={48} />
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {entity.fields.map((field, fieldIdx) => (
                      <TableRow key={field.name}>
                        <TableCell>
                          <Typography variant="body2" fontFamily="monospace">{field.name}</Typography>
                        </TableCell>
                        <TableCell align="center">
                          <Checkbox
                            size="small"
                            checked={field.roles.includes("*")}
                            onChange={() => handleToggleRole(entityIdx, fieldIdx, "*")}
                          />
                        </TableCell>
                        {KNOWN_ROLES.map((role) => (
                          <TableCell key={role} align="center">
                            <Checkbox
                              size="small"
                              checked={isRoleChecked(field, role)}
                              onChange={() => handleToggleRole(entityIdx, fieldIdx, role)}
                              disabled={field.roles.includes("*")}
                            />
                          </TableCell>
                        ))}
                        <TableCell align="center">
                          <IconButton size="small" color="error" onClick={() => handleRemoveField(entityIdx, fieldIdx)}>
                            <DeleteIcon fontSize="small" />
                          </IconButton>
                        </TableCell>
                      </TableRow>
                    ))}
                    <TableRow>
                      <TableCell colSpan={KNOWN_ROLES.length + 3}>
                        <Box sx={{ display: "flex", gap: 1, alignItems: "center" }}>
                          <TextField
                            size="small"
                            placeholder="New field name"
                            value={newFieldNames[entity.name] ?? ""}
                            onChange={(e) => setNewFieldNames((prev) => ({ ...prev, [entity.name]: e.target.value }))}
                            onKeyDown={(e) => { if (e.key === "Enter") handleAddField(entityIdx); }}
                            sx={{ flex: 1 }}
                          />
                          <Button size="small" startIcon={<AddIcon />} onClick={() => handleAddField(entityIdx)}>
                            Add
                          </Button>
                        </Box>
                      </TableCell>
                    </TableRow>
                  </TableBody>
                </Table>
              </CardContent>
            </Card>
          </AccordionDetails>
        </Accordion>
      ))}
    </PageContainer>
  );
}
