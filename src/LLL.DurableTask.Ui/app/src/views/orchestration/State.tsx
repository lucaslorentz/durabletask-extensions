import { Box, Chip, Link, Table, TableBody, TableCell, TableContainer, TableRow } from "@mui/material";
import React from "react";
import { Link as RouterLink } from "react-router-dom";
import { useApiClient } from "../../hooks/useApiClient";
import { OrchestrationState } from "../../models/ApiModels";
import { formatDateTime } from "../../utils/date-utils";

type Props = {
  state: OrchestrationState;
  definedExecutionId: boolean;
};

const headerSx = { minWidth: 200, width: 200 } as const;

export function State(props: Props) {
  const apiClient = useApiClient();
  const { state, definedExecutionId } = props;

  return (
    <TableContainer>
      <Table>
        <TableBody>
          <TableRow>
            <TableCell variant="head" sx={headerSx}>
              InstanceId
            </TableCell>
            <TableCell>{state.orchestrationInstance.instanceId}</TableCell>
          </TableRow>
          <TableRow>
            <TableCell variant="head" sx={headerSx}>
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
                  underline="hover">
                  {state.orchestrationInstance.executionId}
                </Link>
              )}
            </TableCell>
          </TableRow>
          <TableRow>
            <TableCell variant="head" sx={headerSx}>
              Name
            </TableCell>
            <TableCell>{state.name}</TableCell>
          </TableRow>
          <TableRow>
            <TableCell variant="head" sx={headerSx}>
              Version
            </TableCell>
            <TableCell>{state.version}</TableCell>
          </TableRow>
          <TableRow>
            <TableCell variant="head" sx={headerSx}>
              Tags
            </TableCell>
            <TableCell padding="none">
              <Box
                sx={{
                  display: "flex",
                  flexWrap: "wrap",
                  mx: 1.5,
                  "& > *": { m: 0.25 },
                }}
              >
                {state.tags &&
                  Object.entries(state.tags).map(([key, value]) => (
                    <Chip key={key} size="small" label={`${key}: ${value}`} />
                  ))}
              </Box>
            </TableCell>
          </TableRow>
          {state.parentInstance && (
            <TableRow>
              <TableCell variant="head" sx={headerSx}>
                Parent
              </TableCell>
              <TableCell>
                <Link
                  component={RouterLink}
                  to={`/orchestrations/${encodeURIComponent(
                    state.parentInstance.orchestrationInstance.instanceId
                  )}`}
                  underline="hover">
                  {state.parentInstance.orchestrationInstance.instanceId}
                </Link>
              </TableCell>
            </TableRow>
          )}
          <TableRow>
            <TableCell variant="head" sx={headerSx}>
              Status
            </TableCell>
            <TableCell>{state.orchestrationStatus}</TableCell>
          </TableRow>
          <TableRow>
            <TableCell
              variant="head"
              sx={headerSx}
            >
              Custom Status
            </TableCell>
            <TableCell padding="none" sx={{ wordBreak: "break-all" }}>
              <Box sx={{ p: 2, maxHeight: 100, overflow: "auto" }}>
                {state.status}
              </Box>
            </TableCell>
          </TableRow>
          <TableRow>
            <TableCell variant="head" sx={headerSx}>
              Created Time
            </TableCell>
            <TableCell>{formatDateTime(state.createdTime)}</TableCell>
          </TableRow>
          <TableRow>
            <TableCell variant="head" sx={headerSx}>
              Completed Time
            </TableCell>
            <TableCell>{formatDateTime(state.completedTime)}</TableCell>
          </TableRow>
          <TableRow>
            <TableCell variant="head" sx={headerSx}>
              Last Updated Time
            </TableCell>
            <TableCell>{formatDateTime(state.lastUpdatedTime)}</TableCell>
          </TableRow>
        </TableBody>
      </Table>
    </TableContainer>
  );
}
