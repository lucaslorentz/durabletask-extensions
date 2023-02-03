import {
  Chip,
  Link,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
} from "@mui/material";
import makeStyles from "@mui/styles/makeStyles";
import { default as React } from "react";
import { Link as RouterLink } from "react-router-dom";
import { useApiClient } from "../../hooks/useApiClient";
import { OrchestrationsResponse } from "../../models/ApiModels";
import { formatDateTime } from "../../utils/date-utils";

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

interface Props {
  result: OrchestrationsResponse | undefined;
}

export function OrchestrationsTable({ result }: Props) {
  const classes = useStyles();
  const apiClient = useApiClient();

  return (
    <TableContainer>
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
          {result?.orchestrationState.map((orchestration) => (
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
              <TableCell>{formatDateTime(orchestration.createdTime)}</TableCell>
              <TableCell>
                {formatDateTime(orchestration.lastUpdatedTime)}
              </TableCell>
              {apiClient.hasFeature("Tags") && (
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
              )}
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
}
