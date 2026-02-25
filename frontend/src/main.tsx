import React from "react";
import ReactDOM from "react-dom/client";
import { QueryClient, QueryClientProvider, MutationCache } from "@tanstack/react-query";
import { CssBaseline, ThemeProvider } from "@mui/material";
import { LocalizationProvider } from "@mui/x-date-pickers/LocalizationProvider";
import { AdapterDayjs } from "@mui/x-date-pickers/AdapterDayjs";
import { RouterProvider } from "react-router-dom";
import { SnackbarProvider, enqueueSnackbar } from "notistack";
import { router } from "./router";
import { theme } from "./theme";
import { AuthProvider } from "./auth/AuthContext";
import { extractErrorMessage } from "./utils/extractErrorMessage";

const mutationCache = new MutationCache({
  onError: (error, _variables, _context, mutation) => {
    if (mutation.meta?.skipErrorToast) return;
    enqueueSnackbar(extractErrorMessage(error), { variant: "error" });
  },
  onSuccess: (_data, _variables, _context, mutation) => {
    const msg = mutation.meta?.successMessage;
    if (msg) enqueueSnackbar(msg, { variant: "success" });
  },
});

const queryClient = new QueryClient({
  mutationCache,
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <QueryClientProvider client={queryClient}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <SnackbarProvider maxSnack={3} anchorOrigin={{ vertical: "bottom", horizontal: "right" }} autoHideDuration={4000}>
          <LocalizationProvider dateAdapter={AdapterDayjs}>
            <AuthProvider>
              <RouterProvider router={router} />
            </AuthProvider>
          </LocalizationProvider>
        </SnackbarProvider>
      </ThemeProvider>
    </QueryClientProvider>
  </React.StrictMode>
);
