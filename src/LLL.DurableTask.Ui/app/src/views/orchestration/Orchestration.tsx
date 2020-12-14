import {
  Box,
  Breadcrumbs,
  Chip,
  LinearProgress,
  Link,
  makeStyles,
  Paper,
  Tab,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Tabs,
  Typography,
  Button,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogContentText,
  DialogActions,
} from "@material-ui/core";
import React, { useEffect, useState } from "react";
import {
  Link as RouterLink,
  useRouteMatch,
  useHistory,
} from "react-router-dom";
import { useWindowSize } from "react-use";
import { useDynamicRefs } from "../../hooks/useDynamicRefs";
import { useQueryState } from "../../hooks/useQueryState";
import { OrchestrationState } from "../../models/ApiModels";
import { Dot } from "./Dot";
import { LineBuilder } from "./Line";
import { RaiseEvent } from "./RaiseEvent";
import { Terminate } from "./Terminate";
import DeleteIcon from "@material-ui/icons/Delete";

type RouteParams = {
  instanceId: string;
  executionId: string;
};

const useStyles = makeStyles((theme) => ({
  chips: {
    display: "flex",
    flexWrap: "wrap",
    margin: theme.spacing(0, 1.5),
    "& > *": {
      margin: theme.spacing(0.25),
    },
  },
}));

export function Orchestration() {
  const classes = useStyles();

  const [state, setState] = useState<OrchestrationState | undefined>(undefined);
  const [eventsHistory, setEventsHistory] = useState<any[]>([]);
  const [tab, setTab] = useQueryState("tab", 0, {
    parse: Number,
  });
  const [isLoading, setIsLoading] = useState(false);

  const route = useRouteMatch<RouteParams>();

  const { instanceId, executionId } = route.params;

  useEffect(() => {
    (async () => {
      setState(undefined);
      setEventsHistory([]);
      setIsLoading(true);

      let url = `/api/v1/orchestrations/${instanceId}`;
      if (executionId) {
        url = `${url}/${executionId}`;
      }

      var state = await fetch(url).then(
        (r) => r.json() as Promise<OrchestrationState>
      );

      var history = await fetch(
        `/api/v1/orchestrations/${instanceId}/${state.orchestrationInstance.executionId}/history`
      ).then((r) => r.json());

      setState(state);
      setEventsHistory(history);
      setIsLoading(false);
    })();
  }, [instanceId]);

  const filteredEvents = eventsHistory.filter(
    (e) =>
      e.eventType !== "OrchestratorStarted" &&
      e.eventType !== "OrchestratorCompleted"
  );

  const [getRefCurrent, getRefCallback] = useDynamicRefs(true);

  useWindowSize();
  const data = prepareData(filteredEvents, getRefCurrent);

  const history = useHistory();
  const [showConfirmPurge, setShowConfirmPurge] = useState(false);
  function handlePurgeClick() {
    setShowConfirmPurge(true);
  }
  async function handleConfirmPurgeClick() {
    var response = await fetch(`/api/v1/orchestrations/${instanceId}`, {
      method: "DELETE",
      headers: {
        "Content-Type": "application/json",
      },
    });

    if (response.status >= 400) {
      throw new Error("Invalid response");
    }

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
      <Box marginBottom={1} display="flex">
        <Box flex={1}>
          <Breadcrumbs aria-label="breadcrumb">
            <Link component={RouterLink} to="/orchestrations">
              Orchestrations
            </Link>
            <Typography color="textPrimary">{instanceId}</Typography>
          </Breadcrumbs>
        </Box>
        <Box>
          <Button
            variant="contained"
            startIcon={<DeleteIcon />}
            onClick={handlePurgeClick}
            size="small"
          >
            Purge
          </Button>
        </Box>
      </Box>
      <Box height={4} marginBottom={1}>
        {isLoading && <LinearProgress />}
      </Box>
      <Paper variant="outlined">
        <Tabs
          value={tab}
          onChange={(x, v) => setTab(v)}
          indicatorColor="primary"
          textColor="primary"
        >
          <Tab label="State" />
          <Tab label="History" />
          <Tab label="Raise Event" />
          <Tab label="Terminate" />
          <Tab label="Json" />
        </Tabs>
        {state && tab === 0 && (
          <TableContainer>
            <Table>
              <TableBody>
                <TableRow>
                  <TableCell>InstanceId</TableCell>
                  <TableCell>
                    {state.orchestrationInstance.instanceId}
                  </TableCell>
                </TableRow>
                <TableRow>
                  <TableCell>ExecutionId</TableCell>
                  <TableCell>
                    {state.orchestrationInstance.executionId}
                  </TableCell>
                </TableRow>
                <TableRow>
                  <TableCell>Name</TableCell>
                  <TableCell>{state.name}</TableCell>
                </TableRow>
                <TableRow>
                  <TableCell>Version</TableCell>
                  <TableCell>{state.version}</TableCell>
                </TableRow>
                <TableRow>
                  <TableCell>Tags</TableCell>
                  <TableCell padding="none">
                    <div className={classes.chips}>
                      {state.tags &&
                        Object.entries(state.tags).map(([key, value]) => (
                          <Chip
                            key={key}
                            size="small"
                            label={`${key}: ${value}`}
                          />
                        ))}
                    </div>
                  </TableCell>
                </TableRow>
                {state.parentInstance && (
                  <TableRow>
                    <TableCell>Parent</TableCell>
                    <TableCell>
                      <Link
                        component={RouterLink}
                        to={`/orchestrations/${state.parentInstance.orchestrationInstance.instanceId}`}
                      >
                        {state.parentInstance.orchestrationInstance.instanceId}
                      </Link>
                    </TableCell>
                  </TableRow>
                )}
                <TableRow>
                  <TableCell>Status</TableCell>
                  <TableCell>{state.orchestrationStatus}</TableCell>
                </TableRow>
                <TableRow>
                  <TableCell>Custom Status</TableCell>
                  <TableCell>{state.status}</TableCell>
                </TableRow>
                <TableRow>
                  <TableCell>Created Time</TableCell>
                  <TableCell>{state.createdTime}</TableCell>
                </TableRow>
                <TableRow>
                  <TableCell>Completed Time</TableCell>
                  <TableCell>
                    {state.completedTime.indexOf("9999") !== 0
                      ? state.completedTime
                      : null}
                  </TableCell>
                </TableRow>
                <TableRow>
                  <TableCell>Last Updated Time</TableCell>
                  <TableCell>{state.lastUpdatedTime}</TableCell>
                </TableRow>
              </TableBody>
            </Table>
          </TableContainer>
        )}
        {tab === 1 && (
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell></TableCell>
                  <TableCell>EventType</TableCell>
                  <TableCell>Timestamp</TableCell>
                  <TableCell>Target</TableCell>
                  <TableCell>Data</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {data.rows.map((row, index) => {
                  return (
                    <TableRow key={index}>
                      <TableCell style={{ width: 1, position: "relative" }}>
                        <Dot
                          ref={getRefCallback<HTMLDivElement>(index)}
                          style={{
                            marginLeft: row.indentation,
                            backgroundColor: row.line.color,
                          }}
                        ></Dot>
                        {index === 0 && (
                          <svg
                            ref={getRefCallback<SVGSVGElement>("svg")}
                            style={{
                              position: "absolute",
                              top: 0,
                              left: 0,
                              width: "100%",
                              height: data.svgHeight,
                            }}
                          >
                            {data.lines.map((line, index) => (
                              <path
                                key={index}
                                d={line.toPath()}
                                style={{
                                  fill: "none",
                                  stroke: line.color,
                                  strokeWidth: 2,
                                }}
                              />
                            ))}
                          </svg>
                        )}
                      </TableCell>
                      <TableCell>{row.event.eventType}</TableCell>
                      <TableCell>{row.event.timestamp}</TableCell>
                      <TableCell>
                        {row.event.name ? (
                          <>
                            {row.event.name}
                            {row.event.version
                              ? ` (${row.event.version})`
                              : null}
                          </>
                        ) : (
                          row.event.fireAt ?? row.event.orchestrationStatus
                        )}
                      </TableCell>
                      <TableCell
                        style={{ wordBreak: "break-all", width: "30%" }}
                      >
                        {row.event.instanceId ? (
                          <Link
                            component={RouterLink}
                            to={`/orchestrations/${row.event.instanceId}`}
                          >
                            {row.event.instanceId}
                          </Link>
                        ) : (
                          row.event.input ??
                          row.event.result ??
                          row.event.reason ??
                          row.event.reason
                        )}
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          </TableContainer>
        )}
        {tab === 2 && (
          <Box padding={2}>
            {state && <RaiseEvent instanceId={instanceId} />}
          </Box>
        )}
        {tab === 3 && (
          <Box padding={2}>
            {state && <Terminate instanceId={instanceId} />}
          </Box>
        )}
        {tab === 4 && (
          <Box padding={2}>
            <pre>
              {JSON.stringify(
                { state: state, history: eventsHistory },
                null,
                2
              )}
            </pre>
          </Box>
        )}
      </Paper>
    </div>
  );
}

type Data = {
  lines: LineBuilder[];
  rows: DataRow[];
  svgHeight: number;
};

type DataRow = {
  indentation: number;
  line: LineBuilder;
  event: any;
};

const forkEventTypes = [
  "TaskScheduled",
  "TimerCreated",
  "SubOrchestrationInstanceCreated",
];
const mergeEventTypes = [
  "TaskCompleted",
  "TaskFailed",
  "TimerFired",
  "SubOrchestrationInstanceCompleted",
  "SubOrchestrationInstanceFailed",
];
const forceRootEventTypes = [
  "ExecutionCompleted",
  "EventSent",
  "ContinueAsNew",
];

function prepareData(
  events: any[],
  getRefCurrent: <T>(name: string | number) => T
): Data {
  const indentSize = 20;
  const mainline = new LineBuilder(getColor([]));
  const linesStack: LineBuilder[] = [mainline];
  const linesById: Record<number, LineBuilder> = {
    [-1]: mainline,
  };
  const rows: DataRow[] = [];
  const svgElement = getRefCurrent<SVGSVGElement>("svg");
  let svgHeight = 0;

  for (var index = 0; index < events.length; index++) {
    const event = events[index];

    const dotElement = getRefCurrent<HTMLDivElement>(index);
    const center =
      dotElement && svgElement ? getCenter(dotElement, svgElement) : null;

    const lineId = forceRootEventTypes.includes(event.eventType)
      ? -1
      : event.taskScheduledId ?? event.timerId ?? event.eventId;

    let line = linesById[lineId];
    if (!line) {
      line = new LineBuilder(getColor(linesStack.map((s) => s.color)));
      linesStack.push(line);
      linesById[lineId] = line;
    }

    if (center && forkEventTypes.includes(event.eventType)) {
      line.lineTo(21, center.top - indentSize * 2);
    }

    if (center) {
      for (var otherline of linesStack) {
        otherline.lineTo(
          linesStack.indexOf(otherline) * indentSize + 21,
          center.top
        );
      }

      svgHeight = center.top;
    }

    rows.push({
      event: event,
      indentation: linesStack.indexOf(line) * indentSize,
      line: line,
    });

    if (mergeEventTypes.includes(event.eventType)) {
      if (center) {
        line.lineTo(21, center.top + indentSize * 2);
      }
      linesStack.splice(linesStack.indexOf(line), 1);
    }
  }

  return {
    rows: rows,
    lines: Object.values(linesById),
    svgHeight: svgHeight,
  };
}

function getCenter(dotElement: HTMLDivElement, svgElement: SVGSVGElement) {
  const rect = dotElement.getBoundingClientRect();
  const svgRect = svgElement.getBoundingClientRect();
  const left = rect.left + rect.width / 2 - svgRect.left;
  const top = rect.top + rect.height / 2 - svgRect.top;
  return { left, top };
}

function getColor(usedColors: string[]): string {
  for (let color of colors) {
    if (!usedColors.includes(color)) {
      return color;
    }
  }
  return "#000000";
}

export const colors = [
  "#000000",
  "#E53935",
  "#1E88E5",
  "#4CAF50",
  "#FFEB3B",
  "#FF5722",
];
