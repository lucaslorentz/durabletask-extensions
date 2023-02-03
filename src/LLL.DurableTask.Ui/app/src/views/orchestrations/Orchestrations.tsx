import {
  Box,
  Breadcrumbs,
  LinearProgress,
  Paper,
  Typography,
} from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { default as React, useCallback } from "react";
import { useDebounce } from "use-debounce";
import { useApiClient } from "../../hooks/useApiClient";
import { AutoRefreshButton } from "../../components/RefreshButton";
import { ErrorAlert } from "../../components/ErrorAlert";
import { Pagination } from "../../components/Pagination";
import { useLocationState } from "../../hooks/useLocationState";
import { useOrchestrationsFilter } from "../../hooks/useOrchestrationsFilter";
import { usePageSize } from "../../hooks/usePageSize";
import { useRefreshInterval } from "../../hooks/useRefreshInterval";
import { OrchestrationsRequest } from "../../models/ApiModels";
import { OrchestrationsSearch } from "./OrchestrationsSearch";
import { OrchestrationsTable } from "./OrchestrationsTable";

export function Orchestrations() {
  const apiClient = useApiClient();

  const filter = useOrchestrationsFilter();

  const [refreshInterval, setRefreshInterval] =
    useRefreshInterval("orchestrations");

  const [continuationTokenStack, setContinuationTokenStack] = useLocationState<
    string[]
  >("continuationTokenStack", []);

  const [pageSize, setPageSize] = usePageSize();

  const debouncedFilter = useDebounce(filter, 500);

  const request: OrchestrationsRequest = {
    ...debouncedFilter,
    pageSize,
    continuationToken:
      continuationTokenStack.length > 0 ? continuationTokenStack[0] : undefined,
  };

  const query = useQuery({
    queryKey: ["orchestrations", request],
    queryFn: () => apiClient.listOrchestrations(request),
    keepPreviousData: true,
    refetchInterval: refreshInterval ? refreshInterval * 1000 : undefined,
  });

  const firstPageCallback = useCallback(
    () => setContinuationTokenStack([]),
    [setContinuationTokenStack]
  );
  const previousPageCallback = useCallback(() => {
    const [, ...newStack] = continuationTokenStack;
    setContinuationTokenStack(newStack);
  }, [continuationTokenStack, setContinuationTokenStack]);
  const nextPageCallback = useCallback(() => {
    if (
      query.data != null &&
      continuationTokenStack[0] !== query.data.continuationToken
    ) {
      setContinuationTokenStack([
        query.data.continuationToken,
        ...continuationTokenStack,
      ]);
    }
  }, [continuationTokenStack, query.data, setContinuationTokenStack]);

  return (
    <div>
      <Box marginBottom={2}>
        <Breadcrumbs aria-label="breadcrumb">
          <Typography color="textPrimary">Orchestrations</Typography>
        </Breadcrumbs>
      </Box>
      <Box marginBottom={2}>
        <OrchestrationsSearch searchState={filter} />
      </Box>
      <Box>
        <AutoRefreshButton
          refreshInterval={refreshInterval}
          setRefreshInterval={setRefreshInterval}
          onClick={query.refetch}
        />
      </Box>
      <Box height={4} marginTop={0.5} marginBottom={0.5}>
        {query.isFetching && <LinearProgress />}
      </Box>
      {query.isError ? (
        <Box marginBottom={2}>
          <ErrorAlert error={query.error} />
        </Box>
      ) : null}
      <Paper variant="outlined">
        <OrchestrationsTable result={query.data} />
        <Pagination
          count={query.data?.orchestrationState?.length ?? 0}
          pageSize={pageSize}
          setPageSize={setPageSize}
          continuationTokenStack={continuationTokenStack}
          nextContinuationToken={query.data?.continuationToken}
          onFirst={firstPageCallback}
          onPrevious={previousPageCallback}
          onNext={nextPageCallback}
        />
      </Paper>
    </div>
  );
}
