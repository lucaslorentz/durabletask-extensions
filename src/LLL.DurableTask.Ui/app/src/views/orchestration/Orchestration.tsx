import DeleteIcon from "@mui/icons-material/Delete";
import ReplayIcon from "@mui/icons-material/Replay";
import StopIcon from "@mui/icons-material/Stop";
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
import React, { useCallback, useState } from "react";
import { Link as RouterLink, useNavigate, useParams } from "react-router";
import { ErrorAlert } from "../../components/ErrorAlert";
import { ReasonDialog } from "../../components/ReasonDialog";
import { AutoRefreshButton } from "../../components/RefreshButton";
import { useApiClient } from "../../hooks/useApiClient";
import { useQueryState } from "../../hooks/useQueryState";
import { useRefreshInterval } from "../../hooks/useRefreshInterval";
import { ExecutionsList } from "./ExecutionsList";
import { HistoryTable } from "./HistoryTable";
import { RaiseEvent } from "./RaiseEvent";
import { State } from "./State";

type RouteParams = {
  instanceId: string;
  executionId: string;
};

type TabValue = "state" | "history" | "executions" | "raise_event" | "json";

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

  const handlePurgeClick = useCallback(async () => {
    const { confirmed } = await confirm({
      description:
        "This action is irreversible. Do you confirm the purge of this instance?",
    });
    if (!confirmed) return;
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
  }, [
    closeSnackbar,
    confirm,
    enqueueSnackbar,
    instanceId,
    navigate,
    purgeMutation,
  ]);

  const [reasonDialog, setReasonDialog] = useState<{
    action: string;
    fn: (reason: string) => Promise<void>;
  } | null>(null);

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
        {stateQuery.isSuccess && (
          <Stack direction="row" spacing={1}>
            {apiClient.isAuthorized("OrchestrationsTerminate") && (
              <Button
                variant="outlined"
                startIcon={<StopIcon />}
                size="small"
                onClick={() =>
                  setReasonDialog({
                    action: "Terminate",
                    fn: async (reason) => {
                      await apiClient.terminateOrchestration(instanceId, {
                        reason,
                      });
                      enqueueSnackbar("Termination requested", {
                        variant: "success",
                      });
                      stateQuery.refetch();
                    },
                  })
                }
              >
                Terminate
              </Button>
            )}
            {apiClient.hasFeature("Rewind") &&
              apiClient.isAuthorized("OrchestrationsRewind") && (
                <Button
                  variant="outlined"
                  startIcon={<ReplayIcon />}
                  size="small"
                  onClick={() =>
                    setReasonDialog({
                      action: "Rewind",
                      fn: async (reason) => {
                        await apiClient.rewindOrchestration(instanceId, {
                          reason,
                        });
                        enqueueSnackbar("Failures rewound", {
                          variant: "success",
                        });
                        stateQuery.refetch();
                      },
                    })
                  }
                >
                  Rewind
                </Button>
              )}
            {apiClient.isAuthorized("OrchestrationsPurgeInstance") && (
              <LoadingButton
                variant="outlined"
                color="error"
                startIcon={<DeleteIcon />}
                loading={purgeMutation.isPending}
                onClick={handlePurgeClick}
                size="small"
              >
                Purge
              </LoadingButton>
            )}
          </Stack>
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
          {apiClient.hasFeature("StatePerExecution") && (
            <Tab value="executions" label="Executions" />
          )}
          {apiClient.isAuthorized("OrchestrationsRaiseEvent") && (
            <Tab value="raise_event" label="Raise Event" />
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
            {apiClient.hasFeature("StatePerExecution") &&
              tab === "executions" && (
                <ExecutionsList
                  instanceId={instanceId}
                  currentExecutionId={
                    stateQuery.data.orchestrationInstance.executionId
                  }
                />
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
      <ReasonDialog
        open={reasonDialog !== null}
        title={reasonDialog?.action ?? ""}
        description={`Provide a reason for the ${reasonDialog?.action.toLowerCase()}.`}
        onClose={() => setReasonDialog(null)}
        onConfirm={async (reason) => {
          const dialog = reasonDialog;
          setReasonDialog(null);
          if (dialog) {
            try {
              await dialog.fn(reason);
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
          }
        }}
      />
    </div>
  );
}
