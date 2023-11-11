import {
  StyledEngineProvider,
  Theme,
  ThemeProvider,
} from "@mui/material/styles";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ConfirmProvider } from "material-ui-confirm";
import { SnackbarProvider } from "notistack";
import React from "react";
import { createRoot } from "react-dom/client";
import { HashRouter } from "react-router-dom";
import { App } from "./App";
import { ConfigurationProvider } from "./ConfigurationProvider";
import { customTheme } from "./CustomTheme";
import { ApiClientProvider } from "./hooks/useApiClient";
import { AuthProvider } from "./hooks/useAuth";
import * as serviceWorker from "./serviceWorker";

declare module "@mui/styles/defaultTheme" {
  // eslint-disable-next-line @typescript-eslint/no-empty-interface
  interface DefaultTheme extends Theme {}
}

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
  // <React.StrictMode>
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
            <ConfigurationProvider>
              <AuthProvider>
                <ApiClientProvider>
                  <QueryClientProvider client={queryClient}>
                    <App />
                  </QueryClientProvider>
                </ApiClientProvider>
              </AuthProvider>
            </ConfigurationProvider>
          </ConfirmProvider>
        </SnackbarProvider>
      </ThemeProvider>
    </StyledEngineProvider>
  </HashRouter>,
  // </React.StrictMode>
);

// If you want your app to work offline and load faster, you can change
// unregister() to register() below. Note this comes with some pitfalls.
// Learn more about service workers: https://bit.ly/CRA-PWA
serviceWorker.unregister();
