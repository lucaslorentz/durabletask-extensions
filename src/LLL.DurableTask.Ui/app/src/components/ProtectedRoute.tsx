import Alert from "@material-ui/lab/Alert";
import React from "react";
import { Route, RouteProps } from "react-router-dom";
import { Endpoint } from "../models/ApiModels";
import { useApiClient } from "../ApiClientProvider";

type Props = RouteProps & {
  requiredEndpoints: Endpoint[];
};

export function ProtectedRoute(props: Props) {
  const apiClient = useApiClient();

  const { requiredEndpoints, render, children, ...other } = props;

  const authorizedEndpoints = requiredEndpoints.filter((e) =>
    apiClient.isAuthorized(e)
  );

  const authorized = requiredEndpoints.length === authorizedEndpoints.length;

  return (
    <Route
      {...other}
      render={(route) => {
        if (!authorized) {
          return <Alert severity="info">Unauthorized</Alert>;
        }

        if (render) {
          return render(route);
        }

        return <>{children}</>;
      }}
    />
  );
}
