import { LoadingButton } from "@mui/lab";
import { Button, Grid } from "@mui/material";
import { useMutation } from "@tanstack/react-query";
import { useSnackbar } from "notistack";
import React, { useCallback } from "react";
import * as yup from "yup";
import { TextField } from "../../form/TextField";
import { useForm } from "../../form/useForm";
import { useApiClient } from "../../hooks/useApiClient";
import { TerminateRequest } from "../../models/ApiModels";

type Props = {
  instanceId: string;
  onTerminate?: () => void;
};

const schema = yup
  .object({
    reason: yup.string().label("Reason").default(""),
  })
  .required();

export function Terminate(props: Props) {
  const { instanceId, onTerminate } = props;

  const form = useForm(schema);
  const apiClient = useApiClient();
  const { enqueueSnackbar, closeSnackbar } = useSnackbar();

  const terminateMutation = useMutation<
    void,
    unknown,
    Parameters<typeof apiClient.terminateOrchestration>
  >({
    mutationFn: (args) => apiClient.terminateOrchestration(...args),
  });

  const handleSaveClick = useCallback(async () => {
    try {
      const request: TerminateRequest = {
        reason: form.value.reason,
      };

      await terminateMutation.mutateAsync([instanceId, request]);

      enqueueSnackbar("Termination requested", {
        variant: "success",
      });

      form.reset();

      onTerminate?.();
    } catch (error) {
      enqueueSnackbar(String(error), {
        variant: "error",
        persist: true,
        action: (key) => (
          <Button color="inherit" onClick={() => closeSnackbar(key)}>
            Dismiss
          </Button>
        ),
      });
    }
  }, [
    closeSnackbar,
    enqueueSnackbar,
    form,
    instanceId,
    onTerminate,
    terminateMutation,
  ]);

  return (
    <div>
      <Grid container spacing={2}>
        <Grid item xs={12}>
          <TextField field={form.field("reason")} multiline rows={6} />
        </Grid>
        {form.render((form) => (
          <Grid
            item
            xs={12}
            container
            spacing={1}
            justifyContent="space-between"
          >
            <Grid item>
              <LoadingButton
                variant="contained"
                color="primary"
                loading={terminateMutation.isPending}
                onClick={handleSaveClick}
                disabled={
                  form.pendingValidation || Object.keys(form.errors).length > 0
                }
              >
                Terminate
              </LoadingButton>
            </Grid>
            <Grid item>
              <Button
                onClick={() => form.reset()}
                disabled={terminateMutation.isPending}
              >
                Reset
              </Button>
            </Grid>
          </Grid>
        ))}
      </Grid>
    </div>
  );
}
