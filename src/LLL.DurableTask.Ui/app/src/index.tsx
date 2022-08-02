import {
  StyledEngineProvider,
  Theme,
  ThemeProvider,
} from "@mui/material/styles";
import { ConfirmProvider } from "material-ui-confirm";
import { SnackbarProvider } from "notistack";
import React from "react";
import { createRoot } from "react-dom/client";
import { unstable_HistoryRouter as HistoryRouter } from "react-router-dom";
import { ApiClientProvider } from "./ApiClientProvider";
import { App } from "./App";
import { AuthProvider } from "./AuthProvider";
import { ConfigurationProvider } from "./ConfigurationProvider";
import { customTheme } from "./CustomTheme";
import { history } from "./history";
import * as serviceWorker from "./serviceWorker";

declare module "@mui/styles/defaultTheme" {
  // eslint-disable-next-line @typescript-eslint/no-empty-interface
  interface DefaultTheme extends Theme {}
}

const container = document.getElementById("root");
const root = createRoot(container!);

root.render(
  // <React.StrictMode>
  <HistoryRouter history={history}>
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
                  <App />
                </ApiClientProvider>
              </AuthProvider>
            </ConfigurationProvider>
          </ConfirmProvider>
        </SnackbarProvider>
      </ThemeProvider>
    </StyledEngineProvider>
  </HistoryRouter>
  // </React.StrictMode>
);

// If you want your app to work offline and load faster, you can change
// unregister() to register() below. Note this comes with some pitfalls.
// Learn more about service workers: https://bit.ly/CRA-PWA
serviceWorker.unregister();
