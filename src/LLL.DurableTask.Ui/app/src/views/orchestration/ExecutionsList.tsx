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
import React from "react";
import { Link as RouterLink } from "react-router";
import { useQuery } from "@tanstack/react-query";
import { useApiClient } from "../../hooks/useApiClient";
import { formatDateTime } from "../../utils/date-utils";

type Props = {
  instanceId: string;
  currentExecutionId?: string;
};

export function ExecutionsList({ instanceId, currentExecutionId }: Props) {
  const apiClient = useApiClient();

  const query = useQuery({
    queryKey: ["executions", instanceId],
    queryFn: () =>
      apiClient.listOrchestrations({
        instanceIdPrefix: instanceId,
        includePreviousExecutions: true,
        pageSize: 20,
      }),
  });

  const executions = query.data?.orchestrationState.filter(
    (s) => s.orchestrationInstance.instanceId === instanceId
  );

  if (!executions?.length) return null;

  return (
    <TableContainer>
      <Table>
        <TableHead>
          <TableRow>
            <TableCell>ExecutionId</TableCell>
            <TableCell>Status</TableCell>
            <TableCell>Created Time</TableCell>
            <TableCell>Completed Time</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {executions.map((execution) => {
            const executionId = execution.orchestrationInstance.executionId;
            const isCurrent = executionId === currentExecutionId;
            return (
              <TableRow key={executionId} selected={isCurrent}>
                <TableCell>
                  <Link
                    component={RouterLink}
                    to={`/orchestrations/${encodeURIComponent(instanceId)}/${encodeURIComponent(executionId)}`}
                    underline="hover"
                  >
                    {executionId}
                  </Link>
                  {isCurrent && (
                    <Chip label="viewing" size="small" sx={{ ml: 1 }} />
                  )}
                </TableCell>
                <TableCell>{execution.orchestrationStatus}</TableCell>
                <TableCell>{formatDateTime(execution.createdTime)}</TableCell>
                <TableCell>{formatDateTime(execution.completedTime)}</TableCell>
              </TableRow>
            );
          })}
        </TableBody>
      </Table>
    </TableContainer>
  );
}
