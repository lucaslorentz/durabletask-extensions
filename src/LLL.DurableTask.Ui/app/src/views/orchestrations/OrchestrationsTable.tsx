import DeleteIcon from "@mui/icons-material/Delete";
import StopIcon from "@mui/icons-material/Stop";
import ReplayIcon from "@mui/icons-material/Replay";
import { LoadingButton } from "@mui/lab";
import {
  Box,
  Checkbox,
  Chip,
  Link,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Toolbar,
  Typography,
} from "@mui/material";
import { default as React, useState } from "react";
import { Link as RouterLink } from "react-router";
import { useConfirm } from "material-ui-confirm";
import { useSnackbar } from "notistack";
import { useApiClient } from "../../hooks/useApiClient";
import { OrchestrationsResponse } from "../../models/ApiModels";
import { ReasonDialog } from "../../components/ReasonDialog";
import { formatDateTime } from "../../utils/date-utils";

interface Props {
  result: OrchestrationsResponse | undefined;
  onAction?: () => void;
}

export function OrchestrationsTable({ result, onAction }: Props) {
  const apiClient = useApiClient();
  const confirm = useConfirm();
  const { enqueueSnackbar } = useSnackbar();
  const [selected, setSelected] = useState<Set<string>>(new Set());

  const orchestrations = result?.orchestrationState ?? [];
  const instanceIds = orchestrations.map(
    (o) => o.orchestrationInstance.instanceId,
  );
  const allSelected =
    instanceIds.length > 0 && instanceIds.every((id) => selected.has(id));
  const someSelected = instanceIds.some((id) => selected.has(id));

  const toggleAll = () => {
    if (allSelected) {
      setSelected(new Set());
    } else {
      setSelected(new Set(instanceIds));
    }
  };

  const toggleOne = (instanceId: string) => {
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(instanceId)) {
        next.delete(instanceId);
      } else {
        next.add(instanceId);
      }
      return next;
    });
  };

  const selectedIds = instanceIds.filter((id) => selected.has(id));

  const [reasonDialog, setReasonDialog] = useState<{
    action: string;
    fn: (instanceId: string, reason: string) => Promise<void>;
  } | null>(null);
  const [loading, setLoading] = useState<string | null>(null);

  const executeBulk = async (
    ids: string[],
    action: string,
    fn: (instanceId: string) => Promise<void>,
  ) => {
    setLoading(action);
    let succeeded = 0;
    let failed = 0;
    for (const id of ids) {
      try {
        await fn(id);
        succeeded++;
      } catch {
        failed++;
      }
    }
    setLoading(null);

    const message =
      failed > 0
        ? `${action}: ${succeeded} succeeded, ${failed} failed`
        : `${action}: ${succeeded} succeeded`;
    enqueueSnackbar(message, {
      variant: failed > 0 ? "warning" : "success",
    });

    setSelected(new Set());
    onAction?.();
  };

  const runBulkAction = async (
    action: string,
    fn: (instanceId: string) => Promise<void>,
  ) => {
    const ids = [...selectedIds];
    const { confirmed } = await confirm({
      description: `This will ${action} ${ids.length} orchestration(s). Continue?`,
    });
    if (!confirmed) return;
    executeBulk(ids, action, fn);
  };

  const hasActions =
    apiClient.isAuthorized("OrchestrationsTerminate") ||
    apiClient.isAuthorized("OrchestrationsPurgeInstance") ||
    (apiClient.hasFeature("Rewind") &&
      apiClient.isAuthorized("OrchestrationsRewind"));

  return (
    <TableContainer>
      {selectedIds.length > 0 && (
        <Toolbar variant="dense" sx={{ bgcolor: "action.selected" }}>
          <Typography flex={1} variant="subtitle2">
            {selectedIds.length} selected
          </Typography>
          <Stack direction="row" spacing={1}>
            {apiClient.isAuthorized("OrchestrationsTerminate") && (
              <LoadingButton
                size="small"
                startIcon={<StopIcon />}
                loading={loading === "Terminate"}
                loadingPosition="start"
                disabled={loading !== null}
                onClick={() =>
                  setReasonDialog({
                    action: "Terminate",
                    fn: (id, reason) =>
                      apiClient.terminateOrchestration(id, { reason }),
                  })
                }
              >
                Terminate
              </LoadingButton>
            )}
            {apiClient.hasFeature("Rewind") &&
              apiClient.isAuthorized("OrchestrationsRewind") && (
                <LoadingButton
                  size="small"
                  startIcon={<ReplayIcon />}
                  loading={loading === "Rewind"}
                  loadingPosition="start"
                  disabled={loading !== null}
                  onClick={() =>
                    setReasonDialog({
                      action: "Rewind",
                      fn: (id, reason) =>
                        apiClient.rewindOrchestration(id, { reason }),
                    })
                  }
                >
                  Rewind
                </LoadingButton>
              )}
            {apiClient.isAuthorized("OrchestrationsPurgeInstance") && (
              <LoadingButton
                size="small"
                color="error"
                startIcon={<DeleteIcon />}
                loading={loading === "Purge"}
                loadingPosition="start"
                disabled={loading !== null}
                onClick={() =>
                  runBulkAction("Purge", (id) =>
                    apiClient.purgeOrchestration(id),
                  )
                }
              >
                Purge
              </LoadingButton>
            )}
          </Stack>
        </Toolbar>
      )}
      <Table>
        <TableHead>
          <TableRow>
            {hasActions && (
              <TableCell padding="checkbox">
                <Checkbox
                  indeterminate={someSelected && !allSelected}
                  checked={allSelected}
                  onChange={toggleAll}
                />
              </TableCell>
            )}
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
          {orchestrations.map((orchestration) => {
            const instanceId = orchestration.orchestrationInstance.instanceId;
            const isSelected = selected.has(instanceId);
            return (
              <TableRow
                key={orchestration.orchestrationInstance.executionId}
                selected={isSelected}
              >
                {hasActions && (
                  <TableCell padding="checkbox">
                    <Checkbox
                      checked={isSelected}
                      onChange={() => toggleOne(instanceId)}
                    />
                  </TableCell>
                )}
                <TableCell>
                  <Link
                    component={RouterLink}
                    to={`/orchestrations/${encodeURIComponent(instanceId)}`}
                    underline="hover"
                  >
                    {instanceId}
                  </Link>
                </TableCell>
                <TableCell>
                  {apiClient.hasFeature("StatePerExecution") ? (
                    <Link
                      component={RouterLink}
                      to={`/orchestrations/${encodeURIComponent(
                        instanceId,
                      )}/${encodeURIComponent(
                        orchestration.orchestrationInstance.executionId,
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
                    <Box
                      sx={{
                        display: "flex",
                        flexWrap: "wrap",
                        mx: 1.5,
                        "& > *": { m: 0.25 },
                      }}
                    >
                      {orchestration.tags &&
                        Object.entries(orchestration.tags).map(
                          ([key, value]) => (
                            <Chip
                              key={key}
                              size="small"
                              label={`${key}: ${value}`}
                            />
                          ),
                        )}
                    </Box>
                  </TableCell>
                )}
              </TableRow>
            );
          })}
        </TableBody>
      </Table>
      <ReasonDialog
        open={reasonDialog !== null}
        title={reasonDialog?.action ?? ""}
        description={`This will ${reasonDialog?.action.toLowerCase()} ${selectedIds.length} orchestration(s).`}
        onClose={() => setReasonDialog(null)}
        onConfirm={(reason) => {
          const dialog = reasonDialog;
          const ids = [...selectedIds];
          setReasonDialog(null);
          if (dialog) {
            executeBulk(ids, dialog.action, (id) => dialog.fn(id, reason));
          }
        }}
      />
    </TableContainer>
  );
}
