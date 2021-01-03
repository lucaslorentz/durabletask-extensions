import {
  Box,
  Breadcrumbs,
  Button,
  ButtonGroup,
  Grid,
  LinearProgress,
  Link,
  Paper,
  Tab,
  Tabs,
  Typography,
} from "@material-ui/core";
import Menu from "@material-ui/core/Menu";
import MenuItem from "@material-ui/core/MenuItem";
import { ArrowDropDown, Sync } from "@material-ui/icons";
import DeleteIcon from "@material-ui/icons/Delete";
import { useConfirm } from "material-ui-confirm";
import { useSnackbar } from "notistack";
import React, { useEffect, useReducer, useState } from "react";
import {
  Link as RouterLink,
  useHistory,
  useRouteMatch,
} from "react-router-dom";
import { useApiClient } from "../../ApiClientProvider";
import { ErrorAlert } from "../../components/ErrorAlert";
import { useQueryState } from "../../hooks/useQueryState";
import { HistoryEvent, OrchestrationState } from "../../models/ApiModels";
import { HistoryTable } from "./HistoryTable";
import { RaiseEvent } from "./RaiseEvent";
import { Rewind } from "./Rewind";
import { State } from "./State";
import { Terminate } from "./Terminate";

type RouteParams = {
  instanceId: string;
  executionId: string;
};

const autoRefreshOptions = [
  { value: undefined, label: "Off" },
  { value: 5, label: "5 seconds" },
  { value: 10, label: "10 seconds" },
  { value: 20, label: "20 seconds" },
  { value: 30, label: "30 seconds" },
];

type TabValue =
  | "state"
  | "history"
  | "raise_event"
  | "terminate"
  | "rewind"
  | "json";

export function Orchestration() {
  const [state, setState] = useState<OrchestrationState | undefined>(undefined);
  const [historyEvents, setHistoryEvents] = useState<
    HistoryEvent[] | undefined
  >(undefined);
  const [error, setError] = useState<any>();
  const [tab, setTab] = useQueryState<TabValue>("tab", "state");
  const [isLoading, setIsLoading] = useState(false);
  const [refreshInterval, setRefreshInterval] = useQueryState<
    number | undefined
  >("refreshInterval", undefined, {
    parse: parseInt,
  });
  const [refreshAnchor, setRefreshAnchor] = useState<HTMLElement | undefined>();
  const [refreshCount, triggerRefresh] = useReducer((x) => x + 1, 0);
  const [loadedCount, incrementLoadedCount] = useReducer((x) => x + 1, 0);
  const apiClient = useApiClient();
  const history = useHistory();
  const confirm = useConfirm();
  const { enqueueSnackbar, closeSnackbar } = useSnackbar();
  const route = useRouteMatch<RouteParams>();

  const instanceId =
    route.params.instanceId && decodeURIComponent(route.params.instanceId);
  const executionId =
    route.params.executionId && decodeURIComponent(route.params.executionId);

  useEffect(() => {
    setState(undefined);
    setHistoryEvents(undefined);
  }, [instanceId, executionId]);

  useEffect(() => {
    if (!refreshInterval) return;

    const timeout = setTimeout(() => triggerRefresh(), refreshInterval * 1000);
    return () => clearTimeout(timeout);
  }, [refreshInterval, loadedCount]);

  useEffect(() => {
    (async () => {
      try {
        setIsLoading(true);

        const stateResult = await apiClient.getOrchestrationState(
          instanceId,
          executionId
        );
        setState(stateResult);

        if (apiClient.isAuthorized("OrchestrationsGetExecutionHistory")) {
          var historyEventsResult = await apiClient.getOrchestrationHistory(
            instanceId,
            stateResult.orchestrationInstance.executionId
          );
          setHistoryEvents(historyEventsResult);
        }

        setError(undefined);
      } catch (error) {
        setError(error);
        setState(undefined);
        setHistoryEvents(undefined);
      } finally {
        setIsLoading(false);
        incrementLoadedCount();
      }
    })();
  }, [instanceId, executionId, refreshCount, apiClient]);

  function handlePurgeClick() {
    confirm({
      description:
        "This action is irreversible. Do you confirm the purge of this instance?",
    }).then(async () => {
      try {
        await apiClient.purgeOrchestration(instanceId);
        enqueueSnackbar("Instance purged", {
          variant: "success",
        });
        history.push(`/orchestrations`);
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
  }

  return (
    <div>
      <Box marginBottom={1}>
        <Breadcrumbs aria-label="breadcrumb">
          <Link component={RouterLink} to="/orchestrations">
            Orchestrations
          </Link>
          {executionId ? (
            <Link
              component={RouterLink}
              to={`/orchestrations/${encodeURIComponent(instanceId)}`}
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
      <Grid container spacing={4} alignItems="center">
        <Grid item xs>
          <ButtonGroup color="primary" size="small">
            <Button onClick={() => triggerRefresh()} title="Refresh">
              <Sync />
            </Button>
            <Button onClick={(e) => setRefreshAnchor(e.currentTarget)}>
              {refreshInterval ? `${refreshInterval} seconds` : "Off"}
              <ArrowDropDown />
            </Button>
          </ButtonGroup>
          <Menu
            anchorEl={refreshAnchor}
            keepMounted
            getContentAnchorEl={null}
            anchorOrigin={{
              vertical: "bottom",
              horizontal: "right",
            }}
            transformOrigin={{
              vertical: "top",
              horizontal: "right",
            }}
            open={Boolean(refreshAnchor)}
            onClose={() => setRefreshAnchor(undefined)}
          >
            {autoRefreshOptions.map((option, index) => (
              <MenuItem
                key={index}
                selected={refreshInterval === option.value}
                onClick={() => {
                  setRefreshInterval(option.value);
                  setRefreshAnchor(undefined);
                }}
              >
                {option.label}
              </MenuItem>
            ))}
          </Menu>
        </Grid>
        {apiClient.isAuthorized("OrchestrationsPurgeInstance") && state && (
          <Grid item>
            <Button
              variant="outlined"
              startIcon={<DeleteIcon />}
              onClick={handlePurgeClick}
              size="small"
            >
              Purge
            </Button>
          </Grid>
        )}
      </Grid>
      <Box height={4} marginTop={0.5} marginBottom={0.5}>
        {isLoading && <LinearProgress />}
      </Box>
      {error && (
        <Box marginBottom={2}>
          <ErrorAlert error={error} />
        </Box>
      )}
      <Paper variant="outlined">
        <Tabs
          value={tab}
          onChange={(x, v) => setTab(v)}
          indicatorColor="primary"
          textColor="primary"
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
        {state && (
          <>
            {tab === "state" && (
              <State state={state} definedExecutionId={Boolean(executionId)} />
            )}
            {apiClient.isAuthorized("OrchestrationsGetExecutionHistory") &&
              tab === "history" &&
              historyEvents && <HistoryTable historyEvents={historyEvents} />}
            {apiClient.isAuthorized("OrchestrationsRaiseEvent") &&
              tab === "raise_event" && (
                <Box padding={2}>
                  <RaiseEvent
                    instanceId={instanceId}
                    onRaiseEvent={triggerRefresh}
                  />
                </Box>
              )}
            {apiClient.isAuthorized("OrchestrationsTerminate") &&
              tab === "terminate" && (
                <Box padding={2}>
                  <Terminate
                    instanceId={instanceId}
                    onTerminate={triggerRefresh}
                  />
                </Box>
              )}
            {apiClient.hasFeature("Rewind") &&
              apiClient.isAuthorized("OrchestrationsRewind") &&
              tab === "rewind" && (
                <Box padding={2}>
                  <Rewind instanceId={instanceId} onRewind={triggerRefresh} />
                </Box>
              )}
            {tab === "json" && (
              <Box padding={2}>
                <pre style={{ whiteSpace: "pre-wrap", wordBreak: "break-all" }}>
                  {JSON.stringify(
                    { state: state, history: historyEvents },
                    null,
                    2
                  )}
                </pre>
              </Box>
            )}
          </>
        )}
      </Paper>
    </div>
  );
}
