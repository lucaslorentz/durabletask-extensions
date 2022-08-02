import Alert from "@mui/material/Alert";
import React from "react";
import { useApiClient } from "../ApiClientProvider";
import { Endpoint } from "../models/ApiModels";

type Props = {
  requiredEndpoints: Endpoint[];
  children?: React.ReactNode;
};

export function AuthorizedGuard(props: Props) {
  const apiClient = useApiClient();

  const { requiredEndpoints, children } = props;

  const authorizedEndpoints = requiredEndpoints.filter((e) =>
    apiClient.isAuthorized(e)
  );

  const authorized = requiredEndpoints.length === authorizedEndpoints.length;

  if (!authorized) {
    return <Alert severity="info">Unauthorized</Alert>;
  }

  return <>{children}</>;
}
