import axios from "axios";
import React, { useContext } from "react";
import { useAsync } from "react-use";
import { ErrorAlert } from "./components/ErrorAlert";
import { Configuration } from "./models/Configuration";

type Props = {
  children: React.ReactNode;
};

const configurationContext = React.createContext<Configuration | undefined>(
  undefined
);

export function useConfiguration(): Configuration {
  return useContext(configurationContext) as Configuration;
}

export function ConfigurationProvider(props: Props) {
  const { children } = props;

  const configAsync = useAsync(async () => {
    const response = await axios.get<Configuration>("configuration.json");
    return response.data;
  }, []);

  if (configAsync.error) {
    return <ErrorAlert error={configAsync.error} />;
  }

  if (configAsync.loading) return null;

  return (
    <configurationContext.Provider value={configAsync.value}>
      {children}
    </configurationContext.Provider>
  );
}
