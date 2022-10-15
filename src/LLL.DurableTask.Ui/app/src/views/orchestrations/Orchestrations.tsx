import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Box,
  Breadcrumbs,
  Button,
  Chip,
  FormControl,
  Grid,
  IconButton,
  InputLabel,
  LinearProgress,
  Link,
  MenuItem,
  Paper,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableFooter,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from "@mui/material";
import makeStyles from "@mui/styles/makeStyles";
import ChevronLeftIcon from "@mui/icons-material/ChevronLeft";
import ChevronRightIcon from "@mui/icons-material/ChevronRight";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import FirstPageIcon from "@mui/icons-material/FirstPage";
import RefreshIcon from "@mui/icons-material/Refresh";
import { default as React, useCallback, useState } from "react";
import { Link as RouterLink } from "react-router-dom";
import { useApiClient } from "../../ApiClientProvider";
import { ErrorAlert } from "../../components/ErrorAlert";
import { useDebouncedEffect } from "../../hooks/useDebouncedEffect";
import { useLocationState } from "../../hooks/useLocationState";
import { useQueryState } from "../../hooks/useQueryState";
import {
  OrchestrationsResponse,
  OrchestrationStatus,
} from "../../models/ApiModels";
import { formatDateTime, toLocalISO, toUtcISO } from "../../utils/date-utils";
import { usePageSize } from "../../hooks/usePageSize";

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

export function Orchestrations() {
  const classes = useStyles();

  const apiClient = useApiClient();

  const [instanceId, setInstanceId] = useQueryState<string>("instanceId", "");
  const [name, setName] = useQueryState<string>("name", "");
  const [createdTimeFrom, setCreatedTimeFrom] = useQueryState<string>(
    "createdTimeFrom",
    ""
  );
  const [createdTimeTo, setCreatedTimeTo] = useQueryState<string>(
    "createdTimeTo",
    ""
  );
  let [statuses, setStatuses] = useQueryState<OrchestrationStatus[]>(
    "statuses",
    [],
    { multiple: true }
  );

  const [isLoading, setIsLoading] = useState(false);

  const [continuationTokenStack, setContinuationTokenStack] = useLocationState<
    string[]
  >("continuationTokenStack", []);
  const [pageSize, setPageSize] = usePageSize();

  const [error, setError] = useState<any>();
  const [result, setResult] = useState<OrchestrationsResponse | undefined>(
    undefined
  );

  const load = useCallback(async () => {
    try {
      setIsLoading(true);

      var response = await apiClient.listOrchestrations({
        instanceId: instanceId,
        name: name,
        createdTimeFrom: createdTimeFrom,
        createdTimeTo: createdTimeTo,
        runtimeStatus: statuses,
        top: pageSize,
        continuationToken:
          continuationTokenStack.length > 0
            ? continuationTokenStack[0]
            : undefined,
      });
      setResult(response);
      setError(undefined);
    } catch (error) {
      setResult(undefined);
      setError(error);
    } finally {
      setIsLoading(false);
    }
  }, [
    instanceId,
    name,
    createdTimeFrom,
    createdTimeTo,
    statuses,
    pageSize,
    continuationTokenStack,
    setResult,
    apiClient,
  ]);

  const [skipDebounce] = useDebouncedEffect(load, [load], 500);

  const [searchExpanded, setSearchExpanded] = useState(() =>
    Boolean(
      instanceId || name || createdTimeFrom || createdTimeTo || statuses.length
    )
  );

  function changePage(action: "first" | "previous" | "next") {
    switch (action) {
      case "first":
        setContinuationTokenStack([]);
        skipDebounce();
        break;
      case "previous":
        const [, ...newStack] = continuationTokenStack;
        setContinuationTokenStack(newStack);
        skipDebounce();
        break;
      case "next":
        if (
          result != null &&
          continuationTokenStack[0] !== result.continuationToken
        ) {
          setContinuationTokenStack([
            result.continuationToken,
            ...continuationTokenStack,
          ]);
        }
        skipDebounce();
        break;
    }
  }

  return (
    <div>
      <Box marginBottom={2}>
        <Breadcrumbs aria-label="breadcrumb">
          <Typography color="textPrimary">Orchestrations</Typography>
        </Breadcrumbs>
      </Box>
      <Box>{RenderSearch()}</Box>
      <Box height={4} marginTop={0.5} marginBottom={0.5}>
        {isLoading && <LinearProgress />}
      </Box>
      {error && (
        <Box marginBottom={2}>
          <ErrorAlert error={error} />
        </Box>
      )}
      {RenderTable()}
    </div>
  );

  function RenderSearch() {
    return (
      <Accordion
        variant="outlined"
        expanded={searchExpanded}
        onChange={(_, ex) => setSearchExpanded(ex)}
      >
        <AccordionSummary expandIcon={<ExpandMoreIcon />}>
          Search
        </AccordionSummary>
        <AccordionDetails>
          <Grid container spacing={2}>
            {apiClient.hasFeature("SearchByInstanceId") && (
              <Grid item xs={3}>
                <TextField
                  fullWidth
                  label="InstanceId"
                  size="small"
                  value={instanceId}
                  onChange={(e) => setInstanceId(e.target.value)}
                />
              </Grid>
            )}
            {apiClient.hasFeature("SearchByName") && (
              <Grid item xs={3}>
                <TextField
                  fullWidth
                  label="Name"
                  size="small"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                />
              </Grid>
            )}
            {apiClient.hasFeature("SearchByCreatedTime") && (
              <>
                <Grid item xs={3}>
                  <TextField
                    fullWidth
                    label="Created Time From"
                    type="datetime-local"
                    size="small"
                    value={toLocalISO(createdTimeFrom)}
                    onChange={(e) =>
                      setCreatedTimeFrom(toUtcISO(e.target.value))
                    }
                    InputLabelProps={{
                      shrink: true,
                    }}
                  />
                </Grid>
                <Grid item xs={3}>
                  <TextField
                    fullWidth
                    label="Created Time To"
                    type="datetime-local"
                    size="small"
                    value={toLocalISO(createdTimeTo)}
                    onChange={(e) => setCreatedTimeTo(toUtcISO(e.target.value))}
                    InputLabelProps={{
                      shrink: true,
                    }}
                  />
                </Grid>
              </>
            )}
            {apiClient.hasFeature("SearchByStatus") && (
              <Grid item xs={3}>
                <FormControl fullWidth size="small">
                  <InputLabel>Status</InputLabel>
                  <Select
                    multiple
                    value={statuses}
                    onChange={(e) => setStatuses(e.target.value as any)}
                    label="Status"
                    MenuProps={{
                      anchorOrigin: {
                        vertical: "bottom",
                        horizontal: "right",
                      },
                      transformOrigin: {
                        vertical: "top",
                        horizontal: "right",
                      },
                    }}
                  >
                    <MenuItem value="Pending">Pending</MenuItem>
                    <MenuItem value="Running">Running</MenuItem>
                    <MenuItem value="Completed">Completed</MenuItem>
                    <MenuItem value="ContinuedAsNew">ContinuedAsNew</MenuItem>
                    <MenuItem value="Failed">Failed</MenuItem>
                    <MenuItem value="Canceled">Canceled</MenuItem>
                    <MenuItem value="Terminated">Terminated</MenuItem>
                  </Select>
                </FormControl>
              </Grid>
            )}
          </Grid>
        </AccordionDetails>
      </Accordion>
    );
  }

  function RenderTable(): React.ReactNode {
    return (
      <TableContainer component={Paper} variant="outlined">
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>InstanceId</TableCell>
              <TableCell>ExecutionId</TableCell>
              <TableCell>Name</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>CreatedTime</TableCell>
              <TableCell>LastUpdatedTime</TableCell>
              {apiClient.hasFeature("Tags") && <TableCell>Tags</TableCell>}
            </TableRow>
          </TableHead>
          <TableBody>
            {result?.orchestrations.map((orchestration) => (
              <TableRow key={orchestration.orchestrationInstance.executionId}>
                <TableCell>
                  <Link
                    component={RouterLink}
                    to={`/orchestrations/${encodeURIComponent(
                      orchestration.orchestrationInstance.instanceId
                    )}`}
                    underline="hover"
                  >
                    {orchestration.orchestrationInstance.instanceId}
                  </Link>
                </TableCell>
                <TableCell>
                  {apiClient.hasFeature("StatePerExecution") ? (
                    <Link
                      component={RouterLink}
                      to={`/orchestrations/${encodeURIComponent(
                        orchestration.orchestrationInstance.instanceId
                      )}/${encodeURIComponent(
                        orchestration.orchestrationInstance.executionId
                      )}`}
                      underline="hover"
                    >
                      {orchestration.orchestrationInstance.executionId}
                    </Link>
                  ) : (
                    orchestration.orchestrationInstance.executionId
                  )}
                </TableCell>
                <TableCell>{orchestration.name}</TableCell>
                <TableCell>{orchestration.orchestrationStatus}</TableCell>
                <TableCell>
                  {formatDateTime(orchestration.createdTime)}
                </TableCell>
                <TableCell>
                  {formatDateTime(orchestration.lastUpdatedTime)}
                </TableCell>
                {apiClient.hasFeature("Tags") && (
                  <TableCell padding="none">
                    <div className={classes.chips}>
                      {orchestration.tags &&
                        Object.entries(orchestration.tags).map(
                          ([key, value]) => (
                            <Chip
                              key={key}
                              size="small"
                              label={`${key}: ${value}`}
                            />
                          )
                        )}
                    </div>
                  </TableCell>
                )}
              </TableRow>
            ))}
          </TableBody>
          <TableFooter>
            <TableRow>
              <TableCell
                colSpan={apiClient.hasFeature("Tags") ? 7 : 6}
                padding="none"
                style={{ color: "#000" }}
              >
                {RenderFooter()}
              </TableCell>
            </TableRow>
          </TableFooter>
        </Table>
      </TableContainer>
    );
  }

  function RenderFooter() {
    return (
      <Stack marginX={2} direction="row" spacing={3} alignItems="center">
        <Box flex={1}>
          <Button startIcon={<RefreshIcon />} onClick={load}>
            Refresh
          </Button>
        </Box>
        <Box>
          Rows per page:{" "}
          <Select
            value={pageSize}
            onChange={(e) => setPageSize(e.target.value as number)}
            SelectDisplayProps={{ style: { fontSize: 13 } }}
            size="small"
            autoWidth
          >
            <MenuItem value={5}>5</MenuItem>
            <MenuItem value={10}>10</MenuItem>
            <MenuItem value={25}>25</MenuItem>
            <MenuItem value={50}>50</MenuItem>
            <MenuItem value={100}>100</MenuItem>
          </Select>
        </Box>
        <Box>
          {continuationTokenStack.length * pageSize + 1}-
          {continuationTokenStack.length * pageSize +
            (result?.orchestrations.length ?? 0)}{" "}
          {apiClient.hasFeature("QueryCount") && `of ${result?.count}`}
        </Box>
        <Box>
          <IconButton
            disabled={continuationTokenStack.length === 0}
            onClick={changePage.bind(null, "first")}
            size="large"
          >
            <FirstPageIcon />
          </IconButton>
          <IconButton
            disabled={continuationTokenStack.length === 0}
            onClick={changePage.bind(null, "previous")}
            size="large"
          >
            <ChevronLeftIcon />
          </IconButton>
          <IconButton
            disabled={!result?.continuationToken}
            onClick={changePage.bind(null, "next")}
            size="large"
          >
            <ChevronRightIcon />
          </IconButton>
        </Box>
      </Stack>
    );
  }
}
