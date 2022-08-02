import React, { useContext } from "react";
import { useAsync } from "react-use";
import { useAuth } from "./AuthProvider";
import { ApiClient } from "./clients/ApiClient";
import { ErrorAlert } from "./components/ErrorAlert";
import { useConfiguration } from "./ConfigurationProvider";

type Props = {
  children: React.ReactNode;
};

const apiClientContext = React.createContext<ApiClient | undefined>(undefined);

export function useApiClient(): ApiClient {
  return useContext(apiClientContext) as ApiClient;
}

export function ApiClientProvider(props: Props) {
  const { children } = props;

  const configuration = useConfiguration();
  const auth = useAuth();

  const apiClientAsync = useAsync(async () => {
    try {
      const apiClient = new ApiClient();
      apiClient.setToken(auth.user?.access_token);
      await apiClient.initialize(configuration.apiBaseUrl);
      return apiClient;
    } catch (error: any) {
      if (error?.response?.status === 401) {
        await auth.signIn?.();
      }
      throw error;
    }
  }, [auth.user, configuration]);

  if (apiClientAsync.error) {
    return <ErrorAlert error={apiClientAsync.error} />;
  }

  if (apiClientAsync.loading) return null;

  return (
    <apiClientContext.Provider value={apiClientAsync.value}>
      {children}
    </apiClientContext.Provider>
  );
}
