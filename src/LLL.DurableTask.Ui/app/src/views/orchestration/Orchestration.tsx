import {
  Box,
  Breadcrumbs,
  Button,
  ButtonGroup,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
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
import React, { useEffect, useReducer, useState } from "react";
import {
  Link as RouterLink,
  useHistory,
  useRouteMatch,
} from "react-router-dom";
import { apiAxios } from "../../apiAxios";
import { useEntrypoint } from "../../EntrypointProvider";
import { useQueryState } from "../../hooks/useQueryState";
import { HistoryEvent, OrchestrationState } from "../../models/ApiModels";
import { HistoryTable } from "./HistoryTable";
import { RaiseEvent } from "./RaiseEvent";
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

type TabValue = "state" | "history" | "raise_event" | "terminate" | "json";

export function Orchestration() {
  const [state, setState] = useState<OrchestrationState | undefined>(undefined);
  const [historyEvents, setHistoryEvents] = useState<
    HistoryEvent[] | undefined
  >(undefined);
  const [tab, setTab] = useQueryState<TabValue>("tab", "state");
  const [isLoading, setIsLoading] = useState(false);
  const [autoRefreshInterval, setAutoRefreshInterval] = useQueryState<
    number | undefined
  >("refreshInterval", undefined, {
    parse: parseInt,
  });
  const [autoRefreshAnchor, setAutoRefreshAnchor] = useState<
    HTMLElement | undefined
  >();
  const [refreshCount, triggerRefresh] = useReducer((x) => x + 1, 0);
  const [loadedCount, incrementLoadedCount] = useReducer((x) => x + 1, 0);
  const entrypoint = useEntrypoint();

  const route = useRouteMatch<RouteParams>();

  const { instanceId, executionId } = route.params;

  useEffect(() => {
    setState(undefined);
    setHistoryEvents(undefined);
  }, [instanceId, executionId]);

  useEffect(() => {
    if (!autoRefreshInterval) return;

    const timeout = setTimeout(
      () => triggerRefresh(),
      autoRefreshInterval * 1000
    );
    return () => clearTimeout(timeout);
  }, [autoRefreshInterval, loadedCount]);

  useEffect(() => {
    (async () => {
      setIsLoading(true);

      let url = `/v1/orchestrations/${instanceId}`;
      if (executionId) {
        url = `${url}/${executionId}`;
      }

      var stateResponse = await apiAxios.get<OrchestrationState>(url);
      setState(stateResponse.data);

      if (entrypoint.endpoints.OrchestrationsGetExecutionHistory.authorized) {
        var historyResponse = await apiAxios.get<HistoryEvent[]>(
          `/v1/orchestrations/${instanceId}/${stateResponse.data.orchestrationInstance.executionId}/history`
        );
        setHistoryEvents(historyResponse.data);
      }

      setIsLoading(false);
      incrementLoadedCount();
    })();
  }, [instanceId, executionId, refreshCount, entrypoint]);

  const history = useHistory();
  const [showConfirmPurge, setShowConfirmPurge] = useState(false);
  function handlePurgeClick() {
    setShowConfirmPurge(true);
  }
  async function handleConfirmPurgeClick() {
    await apiAxios.delete(`/v1/orchestrations/${instanceId}`);

    setShowConfirmPurge(false);

    history.goBack();
  }

  return (
    <div>
      <Dialog
        open={showConfirmPurge}
        onClose={() => setShowConfirmPurge(false)}
      >
        <DialogTitle>Confirm purge</DialogTitle>
        <DialogContent>
          <DialogContentText>
            This action is irreversible. Do you confirm the purge of this
            instance?
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setShowConfirmPurge(false)} color="primary">
            Cancel
          </Button>
          <Button onClick={handleConfirmPurgeClick} color="primary" autoFocus>
            Confirm
          </Button>
        </DialogActions>
      </Dialog>
      <Box marginBottom={1}>
        <Breadcrumbs aria-label="breadcrumb">
          <Link component={RouterLink} to="/orchestrations">
            Orchestrations
          </Link>
          {executionId ? (
            <Link component={RouterLink} to={`/orchestrations/${instanceId}`}>
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
            <Button onClick={(e) => setAutoRefreshAnchor(e.currentTarget)}>
              {autoRefreshInterval ? `${autoRefreshInterval} seconds` : "Off"}
              <ArrowDropDown />
            </Button>
          </ButtonGroup>
          <Menu
            anchorEl={autoRefreshAnchor}
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
            open={Boolean(autoRefreshAnchor)}
            onClose={() => setAutoRefreshAnchor(undefined)}
          >
            {autoRefreshOptions.map((option, index) => (
              <MenuItem
                key={index}
                selected={autoRefreshInterval === option.value}
                onClick={() => {
                  setAutoRefreshInterval(option.value);
                  setAutoRefreshAnchor(undefined);
                }}
              >
                {option.label}
              </MenuItem>
            ))}
          </Menu>
        </Grid>
        {entrypoint.endpoints.OrchestrationsPurgeInstance.authorized && (
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
      <Paper variant="outlined">
        <Tabs
          value={tab}
          onChange={(x, v) => setTab(v)}
          indicatorColor="primary"
          textColor="primary"
        >
          <Tab value="state" label="State" />
          {historyEvents && <Tab value="history" label="History" />}
          {entrypoint.endpoints.OrchestrationsRaiseEvent.authorized && (
            <Tab value="raise_event" label="Raise Event" />
          )}
          {entrypoint.endpoints.OrchestrationsTerminate.authorized && (
            <Tab value="terminate" label="Terminate" />
          )}
          <Tab value="json" label="Json" />
        </Tabs>
        {state && (
          <>
            {tab === "state" && (
              <State state={state} definedExecutionId={Boolean(executionId)} />
            )}
            {historyEvents && tab === "history" && historyEvents && (
              <HistoryTable historyEvents={historyEvents} />
            )}
            {entrypoint.endpoints.OrchestrationsRaiseEvent.authorized &&
              tab === "raise_event" && (
                <Box padding={2}>
                  <RaiseEvent instanceId={instanceId} />
                </Box>
              )}
            {entrypoint.endpoints.OrchestrationsTerminate.authorized &&
              tab === "terminate" && (
                <Box padding={2}>
                  <Terminate instanceId={instanceId} />
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
