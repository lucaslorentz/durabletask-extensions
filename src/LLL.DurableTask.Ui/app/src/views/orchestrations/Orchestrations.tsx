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
  makeStyles,
  MenuItem,
  Paper,
  Select,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableFooter,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from "@material-ui/core";
import ChevronLeftIcon from "@material-ui/icons/ChevronLeft";
import ChevronRightIcon from "@material-ui/icons/ChevronRight";
import ExpandMoreIcon from "@material-ui/icons/ExpandMore";
import FirstPageIcon from "@material-ui/icons/FirstPage";
import RefreshIcon from "@material-ui/icons/Refresh";
import { default as React, useCallback, useState } from "react";
import { Link as RouterLink } from "react-router-dom";
import { apiAxios } from "../../apiAxios";
import { ErrorAlert } from "../../components/ErrorAlert";
import { useEntrypoint } from "../../EntrypointProvider";
import { useDebouncedEffect } from "../../hooks/useDebouncedEffect";
import { useLocationState } from "../../hooks/useLocationState";
import { useQueryState } from "../../hooks/useQueryState";
import {
  OrchestrationsResponse,
  OrchestrationStatus,
} from "../../models/ApiModels";
import { toDatetimeLocal, toUtcISO } from "../../utils/date-utils";

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

  const { features } = useEntrypoint();

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
  const [pageSize, setPageSize] = useQueryState("pageSize", 5, {
    parse: parseFloat,
  });

  const [error, setError] = useState<any>();
  const [result, setResult] = useState<OrchestrationsResponse | undefined>(
    undefined
  );

  const load = useCallback(async () => {
    try {
      setIsLoading(true);

      const params = new URLSearchParams();
      instanceId && params.append("instanceId", instanceId);
      name && params.append("name", name);
      createdTimeFrom && params.append("createdTimeFrom", createdTimeFrom);
      createdTimeTo && params.append("createdTimeTo", createdTimeTo);
      statuses.forEach((status) => params.append("runtimeStatus", status));
      params.append("top", pageSize.toString());
      continuationTokenStack.length > 0 &&
        params.append("continuationToken", continuationTokenStack[0]);

      var response = await apiAxios.get<OrchestrationsResponse>(
        `/v1/orchestrations?${params.toString()}`
      );
      setResult(response.data);
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
            {features.includes("SearchByInstanceId") && (
              <Grid item xs={3}>
                <TextField
                  fullWidth
                  label="InstanceId"
                  variant="outlined"
                  size="small"
                  value={instanceId}
                  onChange={(e) => setInstanceId(e.target.value)}
                />
              </Grid>
            )}
            {features.includes("SearchByName") && (
              <Grid item xs={3}>
                <TextField
                  fullWidth
                  label="Name"
                  variant="outlined"
                  size="small"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                />
              </Grid>
            )}
            {features.includes("SearchByCreatedTime") && (
              <>
                <Grid item xs={3}>
                  <TextField
                    fullWidth
                    label="Created Time From"
                    variant="outlined"
                    type="datetime-local"
                    size="small"
                    value={toDatetimeLocal(createdTimeFrom)}
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
                    variant="outlined"
                    type="datetime-local"
                    size="small"
                    value={toDatetimeLocal(createdTimeTo)}
                    onChange={(e) => setCreatedTimeTo(toUtcISO(e.target.value))}
                    InputLabelProps={{
                      shrink: true,
                    }}
                  />
                </Grid>
              </>
            )}
            {features.includes("SearchByStatus") && (
              <Grid item xs={3}>
                <FormControl fullWidth variant="outlined" size="small">
                  <InputLabel>Status</InputLabel>
                  <Select
                    multiple
                    value={statuses}
                    onChange={(e) => setStatuses(e.target.value as any)}
                    label="Status"
                    MenuProps={{
                      getContentAnchorEl: null,
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
              <TableCell>CompletedTime</TableCell>
              <TableCell>Tags</TableCell>
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
                  >
                    {orchestration.orchestrationInstance.instanceId}
                  </Link>
                </TableCell>
                <TableCell>
                  <Link
                    component={RouterLink}
                    to={`/orchestrations/${encodeURIComponent(
                      orchestration.orchestrationInstance.instanceId
                    )}/${encodeURIComponent(
                      orchestration.orchestrationInstance.executionId
                    )}`}
                  >
                    {orchestration.orchestrationInstance.executionId}
                  </Link>
                </TableCell>
                <TableCell>{orchestration.name}</TableCell>
                <TableCell>{orchestration.orchestrationStatus}</TableCell>
                <TableCell>
                  {new Date(orchestration.createdTime).toLocaleString()}
                </TableCell>
                <TableCell>
                  {orchestration.completedTime.indexOf("9999") !== 0
                    ? new Date(orchestration.completedTime).toLocaleString()
                    : null}
                </TableCell>
                <TableCell padding="none">
                  <div className={classes.chips}>
                    {orchestration.tags &&
                      Object.entries(orchestration.tags).map(([key, value]) => (
                        <Chip
                          key={key}
                          size="small"
                          label={`${key}: ${value}`}
                        />
                      ))}
                  </div>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
          <TableFooter>
            <TableRow>
              <TableCell colSpan={7} padding="none" style={{ color: "#000" }}>
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
      <Box marginX={2} display="flex" alignItems="center">
        <Box flex={1}>
          <Button startIcon={<RefreshIcon />} onClick={load}>
            Refresh
          </Button>
        </Box>
        <Box>
          Rows per page{" "}
          <Select
            value={pageSize}
            onChange={(e) => setPageSize(e.target.value as number)}
            SelectDisplayProps={{ style: { fontSize: 13 } }}
            autoWidth
            disableUnderline
          >
            <MenuItem value={5}>5</MenuItem>
            <MenuItem value={10}>10</MenuItem>
            <MenuItem value={25}>25</MenuItem>
            <MenuItem value={50}>50</MenuItem>
            <MenuItem value={100}>100</MenuItem>
          </Select>
          {continuationTokenStack.length * pageSize + 1}-
          {continuationTokenStack.length * pageSize +
            (result?.orchestrations.length ?? 0)}{" "}
          {features.includes("QueryCount") && `of ${result?.count}`}
        </Box>
        <IconButton
          disabled={continuationTokenStack.length === 0}
          onClick={changePage.bind(null, "first")}
        >
          <FirstPageIcon />
        </IconButton>
        <IconButton
          disabled={continuationTokenStack.length === 0}
          onClick={changePage.bind(null, "previous")}
        >
          <ChevronLeftIcon />
        </IconButton>
        <IconButton
          disabled={!result?.continuationToken}
          onClick={changePage.bind(null, "next")}
        >
          <ChevronRightIcon />
        </IconButton>
      </Box>
    );
  }
}
