import DeleteIcon from "@mui/icons-material/Delete";
import { LoadingButton } from "@mui/lab";
import {
  Box,
  Breadcrumbs,
  Button,
  LinearProgress,
  Link,
  Paper,
  Stack,
  Tab,
  Tabs,
  Typography,
} from "@mui/material";
import { keepPreviousData, useMutation, useQuery } from "@tanstack/react-query";
import { useConfirm } from "material-ui-confirm";
import { useSnackbar } from "notistack";
import React, { useCallback } from "react";
import { Link as RouterLink, useNavigate, useParams } from "react-router-dom";
import { ErrorAlert } from "../../components/ErrorAlert";
import { AutoRefreshButton } from "../../components/RefreshButton";
import { useApiClient } from "../../hooks/useApiClient";
import { useQueryState } from "../../hooks/useQueryState";
import { useRefreshInterval } from "../../hooks/useRefreshInterval";
import { HistoryTable } from "./HistoryTable";
import { RaiseEvent } from "./RaiseEvent";
import { Rewind } from "./Rewind";
import { State } from "./State";
import { Terminate } from "./Terminate";

type RouteParams = {
  instanceId: string;
  executionId: string;
};

type TabValue =
  | "state"
  | "history"
  | "raise_event"
  | "terminate"
  | "rewind"
  | "json";

export function Orchestration() {
  const [tab, setTab] = useQueryState<TabValue>("tab", "state");

  const [refreshInterval, setRefreshInterval] =
    useRefreshInterval("orchestration");
  const apiClient = useApiClient();
  const navigate = useNavigate();
  const confirm = useConfirm();
  const { enqueueSnackbar, closeSnackbar } = useSnackbar();
  const params = useParams<RouteParams>() as RouteParams;

  const instanceId = params.instanceId && decodeURIComponent(params.instanceId);
  const executionId =
    params.executionId && decodeURIComponent(params.executionId);

  const stateQuery = useQuery({
    queryKey: ["orchestration", instanceId, executionId],
    queryFn: () => apiClient.getOrchestrationState(instanceId, executionId),
    refetchInterval: refreshInterval ? refreshInterval * 1000 : undefined,
  });

  const historyQuery = useQuery({
    queryKey: [
      "orchestration",
      instanceId,
      stateQuery.data?.orchestrationInstance.executionId,
      "history",
      stateQuery.dataUpdatedAt,
    ],
    queryFn: () =>
      apiClient.getOrchestrationHistory(
        instanceId,
        stateQuery.data!.orchestrationInstance.executionId,
      ),
    placeholderData: keepPreviousData,
    enabled:
      Boolean(stateQuery.data?.orchestrationInstance.executionId) &&
      apiClient.isAuthorized("OrchestrationsGetExecutionHistory"),
  });

  const purgeMutation = useMutation<
    void,
    unknown,
    Parameters<typeof apiClient.purgeOrchestration>
  >({
    mutationFn: (args) => apiClient.purgeOrchestration(...args),
  });

  const handlePurgeClick = useCallback(() => {
    confirm({
      description:
        "This action is irreversible. Do you confirm the purge of this instance?",
    }).then(async () => {
      try {
        await purgeMutation.mutateAsync([instanceId]);
        enqueueSnackbar("Instance purged", {
          variant: "success",
        });
        navigate(`/orchestrations`);
      } catch (error) {
        enqueueSnackbar(String(error), {
          variant: "error",
          persist: true,
          action: (key) => (
            <Button color="inherit" onClick={() => closeSnackbar(key)}>
              Dismiss
            </Button>
          ),
        });
      }
    });
  }, [
    closeSnackbar,
    confirm,
    enqueueSnackbar,
    instanceId,
    navigate,
    purgeMutation,
  ]);

  return (
    <div>
      <Box marginBottom={1}>
        <Breadcrumbs aria-label="breadcrumb">
          <Link component={RouterLink} to="/orchestrations" underline="hover">
            Orchestrations
          </Link>
          {executionId ? (
            <Link
              component={RouterLink}
              to={`/orchestrations/${encodeURIComponent(instanceId)}`}
              underline="hover"
            >
              {instanceId}
            </Link>
          ) : (
            <Typography color="textPrimary">{instanceId}</Typography>
          )}
          {executionId && (
            <Typography color="textPrimary">{executionId}</Typography>
          )}
        </Breadcrumbs>
      </Box>
      <Stack direction="row" spacing={4} alignItems="center">
        <Box flex={1}>
          <AutoRefreshButton
            onClick={stateQuery.refetch}
            refreshInterval={refreshInterval}
            setRefreshInterval={setRefreshInterval}
          />
        </Box>
        {apiClient.isAuthorized("OrchestrationsPurgeInstance") &&
          stateQuery.isSuccess && (
            <Box>
              <LoadingButton
                variant="outlined"
                startIcon={<DeleteIcon />}
                loading={purgeMutation.isPending}
                onClick={handlePurgeClick}
                size="small"
              >
                Purge
              </LoadingButton>
            </Box>
          )}
      </Stack>
      <Box height={4} marginTop={0.5} marginBottom={0.5}>
        {(stateQuery.isFetching || historyQuery.isFetching) && (
          <LinearProgress />
        )}
      </Box>
      {stateQuery.error ?? historyQuery.error ? (
        <Box marginBottom={2}>
          <ErrorAlert error={stateQuery.error ?? historyQuery.error} />
        </Box>
      ) : null}
      <Paper variant="outlined">
        <Tabs
          value={tab}
          onChange={(x, v) => setTab(v)}
          indicatorColor="primary"
          textColor="primary"
          variant="scrollable"
        >
          <Tab value="state" label="State" />
          {apiClient.isAuthorized("OrchestrationsGetExecutionHistory") && (
            <Tab value="history" label="History" />
          )}
          {apiClient.isAuthorized("OrchestrationsRaiseEvent") && (
            <Tab value="raise_event" label="Raise Event" />
          )}
          {apiClient.isAuthorized("OrchestrationsTerminate") && (
            <Tab value="terminate" label="Terminate" />
          )}
          {apiClient.hasFeature("Rewind") &&
            apiClient.isAuthorized("OrchestrationsRewind") && (
              <Tab value="rewind" label="Rewind" />
            )}
          <Tab value="json" label="Json" />
        </Tabs>
        {stateQuery.data ? (
          <>
            {tab === "state" && (
              <State
                state={stateQuery.data}
                definedExecutionId={Boolean(executionId)}
              />
            )}
            {apiClient.isAuthorized("OrchestrationsGetExecutionHistory") &&
              tab === "history" &&
              historyQuery.data && (
                <HistoryTable historyEvents={historyQuery.data} />
              )}
            {apiClient.isAuthorized("OrchestrationsRaiseEvent") &&
              tab === "raise_event" && (
                <Box padding={2}>
                  <RaiseEvent
                    instanceId={instanceId}
                    onRaiseEvent={stateQuery.refetch}
                  />
                </Box>
              )}
            {apiClient.isAuthorized("OrchestrationsTerminate") &&
              tab === "terminate" && (
                <Box padding={2}>
                  <Terminate
                    instanceId={instanceId}
                    onTerminate={stateQuery.refetch}
                  />
                </Box>
              )}
            {apiClient.hasFeature("Rewind") &&
              apiClient.isAuthorized("OrchestrationsRewind") &&
              tab === "rewind" && (
                <Box padding={2}>
                  <Rewind
                    instanceId={instanceId}
                    onRewind={stateQuery.refetch}
                  />
                </Box>
              )}
            {tab === "json" && (
              <Box padding={2}>
                <pre style={{ whiteSpace: "pre-wrap", wordBreak: "break-all" }}>
                  {JSON.stringify(
                    { state: stateQuery.data, history: historyQuery.data },
                    null,
                    2,
                  )}
                </pre>
              </Box>
            )}
          </>
        ) : null}
      </Paper>
    </div>
  );
}
