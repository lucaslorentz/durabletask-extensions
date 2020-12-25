import axios from "axios";
import React from "react";
import { useAsync } from "react-use";
import { apiAxios } from "./apiAxios";
import { ErrorAlert } from "./components/ErrorAlert";
import { Configuration } from "./models/Configuration";

type Props = {
  children: React.ReactNode;
};

export function ConfigurationLoader(props: Props) {
  const { children } = props;

  const configAsync = useAsync(async () => {
    const response = await axios.get<Configuration>("configuration.json");
    const configuration = response.data;

    apiAxios.defaults.baseURL = configuration.apiBaseUrl;
  }, []);

  if (configAsync.error) {
    return <ErrorAlert error={configAsync.error} />;
  }

  if (configAsync.loading) return null;

  return <>{children}</>;
}
