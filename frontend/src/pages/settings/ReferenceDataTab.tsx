import { useState } from "react";
import { Accordion, AccordionSummary, AccordionDetails, Card, Typography } from "@mui/material";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import {
  useAllClearers, useDeleteClearer,
  useAllTradePlatforms, useDeleteTradePlatform,
  useAllExchanges, useDeleteExchange,
  useAllCurrencies, useDeleteCurrency,
} from "../../api/hooks";
import type { ClearerDto, TradePlatformDto, ExchangeDto, CurrencyDto } from "../../api/types";
import { ReferenceDataTable, type Column } from "./ReferenceDataTable";
import { CreateClearerDialog, EditClearerDialog } from "./ClearerDialogs";
import { CreateTradePlatformDialog, EditTradePlatformDialog } from "./TradePlatformDialogs";
import { CreateExchangeDialog, EditExchangeDialog } from "./ExchangeDialogs";
import { CreateCurrencyDialog, EditCurrencyDialog } from "./CurrencyDialogs";
import { ConfirmDialog } from "../../components/ConfirmDialog";
import { useConfirm } from "../../hooks/useConfirm";

const clearerCols: Column<ClearerDto>[] = [
  { header: "Name", render: (r) => r.name },
  { header: "Description", render: (r) => r.description ?? "\u2014" },
];

const platformCols: Column<TradePlatformDto>[] = [
  { header: "Name", render: (r) => r.name },
  { header: "Description", render: (r) => r.description ?? "\u2014" },
];

const exchangeCols: Column<ExchangeDto>[] = [
  { header: "Code", render: (r) => r.code, width: 100 },
  { header: "Name", render: (r) => r.name },
];

const currencyCols: Column<CurrencyDto>[] = [
  { header: "Code", render: (r) => r.code, width: 80 },
  { header: "Name", render: (r) => r.name },
  { header: "Symbol", render: (r) => r.symbol ?? "\u2014", width: 80 },
];

export function ReferenceDataTab() {
  // Clearers
  const { data: clearers, isLoading: loadingClearers } = useAllClearers();
  const deleteClearer = useDeleteClearer();
  const [createClearerOpen, setCreateClearerOpen] = useState(false);
  const [editClearer, setEditClearer] = useState<ClearerDto | null>(null);

  // Trade Platforms
  const { data: platforms, isLoading: loadingPlatforms } = useAllTradePlatforms();
  const deletePlatform = useDeleteTradePlatform();
  const [createPlatformOpen, setCreatePlatformOpen] = useState(false);
  const [editPlatform, setEditPlatform] = useState<TradePlatformDto | null>(null);

  // Exchanges
  const { data: exchanges, isLoading: loadingExchanges } = useAllExchanges();
  const deleteExchange = useDeleteExchange();
  const [createExchangeOpen, setCreateExchangeOpen] = useState(false);
  const [editExchange, setEditExchange] = useState<ExchangeDto | null>(null);

  // Currencies
  const { data: currencies, isLoading: loadingCurrencies } = useAllCurrencies();
  const deleteCurrency = useDeleteCurrency();
  const [createCurrencyOpen, setCreateCurrencyOpen] = useState(false);
  const [editCurrency, setEditCurrency] = useState<CurrencyDto | null>(null);

  const { confirm, confirmDialogProps } = useConfirm();

  const isDeleting = deleteClearer.isPending || deletePlatform.isPending || deleteExchange.isPending || deleteCurrency.isPending;

  const handleDelete = async (type: string, id: string) => {
    const ok = await confirm({ title: `Delete ${type}`, message: `Are you sure you want to delete this ${type.toLowerCase()}?` });
    if (!ok) return;
    try {
      if (type === "Clearer") await deleteClearer.mutateAsync(id);
      else if (type === "Trade Platform") await deletePlatform.mutateAsync(id);
      else if (type === "Exchange") await deleteExchange.mutateAsync(id);
      else if (type === "Currency") await deleteCurrency.mutateAsync(id);
    } catch { /* handled by MutationCache */ }
  };

  return (
    <Card variant="outlined">
      <Accordion defaultExpanded disableGutters>
        <AccordionSummary expandIcon={<ExpandMoreIcon />}>
          <Typography fontWeight={600}>Clearers</Typography>
        </AccordionSummary>
        <AccordionDetails>
          <ReferenceDataTable
            title=""
            columns={clearerCols}
            rows={clearers ?? []}
            isLoading={loadingClearers}
            onAdd={() => setCreateClearerOpen(true)}
            onEdit={setEditClearer}
            onDelete={(r) => handleDelete("Clearer", r.id)}
          />
        </AccordionDetails>
      </Accordion>

      <Accordion disableGutters>
        <AccordionSummary expandIcon={<ExpandMoreIcon />}>
          <Typography fontWeight={600}>Trade Platforms</Typography>
        </AccordionSummary>
        <AccordionDetails>
          <ReferenceDataTable
            title=""
            columns={platformCols}
            rows={platforms ?? []}
            isLoading={loadingPlatforms}
            onAdd={() => setCreatePlatformOpen(true)}
            onEdit={setEditPlatform}
            onDelete={(r) => handleDelete("Trade Platform", r.id)}
          />
        </AccordionDetails>
      </Accordion>

      <Accordion disableGutters>
        <AccordionSummary expandIcon={<ExpandMoreIcon />}>
          <Typography fontWeight={600}>Exchanges</Typography>
        </AccordionSummary>
        <AccordionDetails>
          <ReferenceDataTable
            title=""
            columns={exchangeCols}
            rows={exchanges ?? []}
            isLoading={loadingExchanges}
            onAdd={() => setCreateExchangeOpen(true)}
            onEdit={setEditExchange}
            onDelete={(r) => handleDelete("Exchange", r.id)}
          />
        </AccordionDetails>
      </Accordion>

      <Accordion disableGutters>
        <AccordionSummary expandIcon={<ExpandMoreIcon />}>
          <Typography fontWeight={600}>Currencies</Typography>
        </AccordionSummary>
        <AccordionDetails>
          <ReferenceDataTable
            title=""
            columns={currencyCols}
            rows={currencies ?? []}
            isLoading={loadingCurrencies}
            onAdd={() => setCreateCurrencyOpen(true)}
            onEdit={setEditCurrency}
            onDelete={(r) => handleDelete("Currency", r.id)}
          />
        </AccordionDetails>
      </Accordion>

      {/* Dialogs */}
      <CreateClearerDialog open={createClearerOpen} onClose={() => setCreateClearerOpen(false)} />
      <EditClearerDialog open={!!editClearer} onClose={() => setEditClearer(null)} item={editClearer} />
      <CreateTradePlatformDialog open={createPlatformOpen} onClose={() => setCreatePlatformOpen(false)} />
      <EditTradePlatformDialog open={!!editPlatform} onClose={() => setEditPlatform(null)} item={editPlatform} />
      <CreateExchangeDialog open={createExchangeOpen} onClose={() => setCreateExchangeOpen(false)} />
      <EditExchangeDialog open={!!editExchange} onClose={() => setEditExchange(null)} item={editExchange} />
      <CreateCurrencyDialog open={createCurrencyOpen} onClose={() => setCreateCurrencyOpen(false)} />
      <EditCurrencyDialog open={!!editCurrency} onClose={() => setEditCurrency(null)} item={editCurrency} />
      <ConfirmDialog {...confirmDialogProps} isLoading={isDeleting} />
    </Card>
  );
}
