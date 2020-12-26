import React, { useContext } from "react";
import { useAsync } from "react-use";
import { apiAxios } from "./apiAxios";
import { useAuth } from "./AuthProvider";
import { ErrorAlert } from "./components/ErrorAlert";
import { EntrypointResponse } from "./models/ApiModels";

type Props = {
  children: React.ReactNode;
};

const entrypointContext = React.createContext<EntrypointResponse | undefined>(
  undefined
);

export function useEntrypoint(): EntrypointResponse {
  return useContext(entrypointContext) as EntrypointResponse;
}

export function EntrypointProvider(props: Props) {
  const { children } = props;

  const auth = useAuth();

  const entrypointAsync = useAsync(async () => {
    try {
      var response = await apiAxios.get<EntrypointResponse>(`/`);
      return response.data;
    } catch (error) {
      if (error?.response?.status === 401) {
        await auth.signIn?.();
      }
      throw error;
    }
  }, [auth.user]);

  if (entrypointAsync.error) {
    return <ErrorAlert error={entrypointAsync.error} />;
  }

  if (entrypointAsync.loading) return null;

  return (
    <entrypointContext.Provider value={entrypointAsync.value}>
      {children}
    </entrypointContext.Provider>
  );
}
