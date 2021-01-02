import "./utils/yup-extensions";
import React from "react";
import ReactDOM from "react-dom";
import { Router } from "react-router-dom";
import { App } from "./App";
import * as serviceWorker from "./serviceWorker";
import { createBrowserHashHistory } from "./createBrowserHashHistory";
import { ThemeProvider } from "@material-ui/core/styles";
import { customTheme } from "./CustomTheme";
import { ConfigurationProvider } from "./ConfigurationProvider";
import { AuthProvider } from "./AuthProvider";
import { ApiClientProvider } from "./ApiClientProvider";
import { ConfirmProvider } from "material-ui-confirm";
import { SnackbarProvider } from "notistack";

ReactDOM.render(
  // <React.StrictMode>
  <Router history={createBrowserHashHistory({ clearSearch: true })}>
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
  </Router>,
  // </React.StrictMode>
  document.getElementById("root")
);

// If you want your app to work offline and load faster, you can change
// unregister() to register() below. Note this comes with some pitfalls.
// Learn more about service workers: https://bit.ly/CRA-PWA
serviceWorker.unregister();
