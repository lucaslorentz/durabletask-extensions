import {
  Box,
  Chip,
  Link,
  makeStyles,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableRow,
} from "@material-ui/core";
import React from "react";
import { Link as RouterLink } from "react-router-dom";
import { useApiClient } from "../../ApiClientProvider";
import { OrchestrationState } from "../../models/ApiModels";
import { formatDateTime } from "../../utils/date-utils";

type Props = {
  state: OrchestrationState;
  definedExecutionId: boolean;
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
  header: {
    minWidth: 200,
    width: 200,
  },
}));

export function State(props: Props) {
  const classes = useStyles();
  const apiClient = useApiClient();
  const { state, definedExecutionId } = props;

  return (
    <TableContainer>
      <Table>
        <TableBody>
          <TableRow>
            <TableCell variant="head" className={classes.header}>
              InstanceId
            </TableCell>
            <TableCell>{state.orchestrationInstance.instanceId}</TableCell>
          </TableRow>
          <TableRow>
            <TableCell variant="head" className={classes.header}>
              ExecutionId
            </TableCell>
            <TableCell>
              {definedExecutionId ||
              !apiClient.hasFeature("StatePerExecution") ? (
                state.orchestrationInstance.executionId
              ) : (
                <Link
                  component={RouterLink}
                  to={`/orchestrations/${encodeURIComponent(
                    state.orchestrationInstance.instanceId
                  )}/${encodeURIComponent(
                    state.orchestrationInstance.executionId
                  )}`}
                >
                  {state.orchestrationInstance.executionId}
                </Link>
              )}
            </TableCell>
          </TableRow>
          <TableRow>
            <TableCell variant="head" className={classes.header}>
              Name
            </TableCell>
            <TableCell>{state.name}</TableCell>
          </TableRow>
          <TableRow>
            <TableCell variant="head" className={classes.header}>
              Version
            </TableCell>
            <TableCell>{state.version}</TableCell>
          </TableRow>
          <TableRow>
            <TableCell variant="head" className={classes.header}>
              Tags
            </TableCell>
            <TableCell padding="none">
              <div className={classes.chips}>
                {state.tags &&
                  Object.entries(state.tags).map(([key, value]) => (
                    <Chip key={key} size="small" label={`${key}: ${value}`} />
                  ))}
              </div>
            </TableCell>
          </TableRow>
          {state.parentInstance && (
            <TableRow>
              <TableCell variant="head" className={classes.header}>
                Parent
              </TableCell>
              <TableCell>
                <Link
                  component={RouterLink}
                  to={`/orchestrations/${encodeURIComponent(
                    state.parentInstance.orchestrationInstance.instanceId
                  )}`}
                >
                  {state.parentInstance.orchestrationInstance.instanceId}
                </Link>
              </TableCell>
            </TableRow>
          )}
          <TableRow>
            <TableCell variant="head" className={classes.header}>
              Status
            </TableCell>
            <TableCell>{state.orchestrationStatus}</TableCell>
          </TableRow>
          <TableRow>
            <TableCell
              variant="head"
              className={classes.header}
              style={{ width: 200 }}
            >
              Custom Status
            </TableCell>
            <TableCell padding="none" style={{ wordBreak: "break-all" }}>
              <Box padding={2} style={{ maxHeight: 100, overflow: "auto" }}>
                {state.status}
              </Box>
            </TableCell>
          </TableRow>
          <TableRow>
            <TableCell variant="head" className={classes.header}>
              Created Time
            </TableCell>
            <TableCell>{formatDateTime(state.createdTime)}</TableCell>
          </TableRow>
          <TableRow>
            <TableCell variant="head" className={classes.header}>
              Completed Time
            </TableCell>
            <TableCell>{formatDateTime(state.completedTime)}</TableCell>
          </TableRow>
          <TableRow>
            <TableCell variant="head" className={classes.header}>
              Last Updated Time
            </TableCell>
            <TableCell>{formatDateTime(state.lastUpdatedTime)}</TableCell>
          </TableRow>
        </TableBody>
      </Table>
    </TableContainer>
  );
}
