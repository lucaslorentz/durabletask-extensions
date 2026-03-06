import {
  StyledEngineProvider,
  ThemeProvider,
} from "@mui/material/styles";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ConfirmProvider } from "material-ui-confirm";
import { SnackbarProvider } from "notistack";
import React from "react";
import { createRoot } from "react-dom/client";
import { HashRouter } from "react-router-dom";
import { App } from "./App";
import { ErrorBoundary } from "./components/ErrorBoundary";
import { ConfigurationProvider } from "./ConfigurationProvider";
import { customTheme } from "./CustomTheme";
import { ApiClientProvider } from "./hooks/useApiClient";
import { AuthProvider } from "./hooks/useAuth";

const container = document.getElementById("root");
const root = createRoot(container!);

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      gcTime: 0,
      retry: 0,
      refetchOnWindowFocus: false,
      refetchOnReconnect: false,
    },
  },
});

root.render(
  <React.StrictMode>
    <HashRouter>
      <StyledEngineProvider injectFirst>
        <ThemeProvider theme={customTheme}>
          <SnackbarProvider
            anchorOrigin={{
              vertical: "bottom",
              horizontal: "center",
            }}
          >
            <ConfirmProvider>
              <ErrorBoundary>
                <ConfigurationProvider>
                  <AuthProvider>
                    <ApiClientProvider>
                      <QueryClientProvider client={queryClient}>
                        <App />
                      </QueryClientProvider>
                    </ApiClientProvider>
                  </AuthProvider>
                </ConfigurationProvider>
              </ErrorBoundary>
            </ConfirmProvider>
          </SnackbarProvider>
        </ThemeProvider>
      </StyledEngineProvider>
    </HashRouter>
  </React.StrictMode>,
);
