import { Button, Grid } from "@mui/material";
import { useSnackbar } from "notistack";
import React from "react";
import * as yup from "yup";
import { useApiClient } from "../../hooks/useApiClient";
import { TextField } from "../../form/TextField";
import { useForm } from "../../form/useForm";
import { TerminateRequest } from "../../models/ApiModels";
import { useMutation } from "@tanstack/react-query";
import { LoadingButton } from "@mui/lab";

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
  >((args) => apiClient.terminateOrchestration(...args));

  async function handleSaveClick() {
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
  }

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
                loading={terminateMutation.isLoading}
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
                disabled={terminateMutation.isLoading}
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
