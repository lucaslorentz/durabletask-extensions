import {
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
import { OrchestrationState } from "../../models/ApiModels";

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
}));

export function State(props: Props) {
  const classes = useStyles();
  const { state, definedExecutionId } = props;

  return (
    <TableContainer>
      <Table>
        <TableBody>
          <TableRow>
            <TableCell>InstanceId</TableCell>
            <TableCell>{state.orchestrationInstance.instanceId}</TableCell>
          </TableRow>
          <TableRow>
            <TableCell>ExecutionId</TableCell>
            <TableCell>
              {definedExecutionId ? (
                state.orchestrationInstance.executionId
              ) : (
                <Link
                  component={RouterLink}
                  to={`/orchestrations/${encodeURIComponent(state.orchestrationInstance.instanceId)}/${encodeURIComponent(state.orchestrationInstance.executionId)}`}
                >
                  {state.orchestrationInstance.executionId}
                </Link>
              )}
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
                    <Chip key={key} size="small" label={`${key}: ${value}`} />
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
                  to={`/orchestrations/${encodeURIComponent(state.parentInstance.orchestrationInstance.instanceId)}`}
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
  );
}
