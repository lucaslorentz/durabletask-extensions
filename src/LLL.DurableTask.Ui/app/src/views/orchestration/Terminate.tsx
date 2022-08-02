import { Button, Grid } from "@mui/material";
import { useSnackbar } from "notistack";
import React from "react";
import * as yup from "yup";
import { useApiClient } from "../../ApiClientProvider";
import { TextField } from "../../form/TextField";
import { useForm } from "../../form/useForm";
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

  async function handleSaveClick() {
    try {
      const request: TerminateRequest = {
        reason: form.value.reason,
      };

      await apiClient.terminateOrchestration(instanceId, request);

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
              <Button
                variant="contained"
                color="primary"
                onClick={handleSaveClick}
                disabled={
                  form.pendingValidation || Object.keys(form.errors).length > 0
                }
              >
                Terminate
              </Button>
            </Grid>
            <Grid item>
              <Button onClick={() => form.reset()}>Reset</Button>
            </Grid>
          </Grid>
        ))}
      </Grid>
    </div>
  );
}
