import { Button, Grid } from "@mui/material";
import { useSnackbar } from "notistack";
import React from "react";
import * as yup from "yup";
import { useApiClient } from "../../ApiClientProvider";
import { TextField } from "../../form/TextField";
import { useForm } from "../../form/useForm";
import { RewindRequest } from "../../models/ApiModels";

type Props = {
  instanceId: string;
  onRewind?: () => void;
};

const schema = yup
  .object({
    reason: yup.string().label("Reason").default(""),
  })
  .required();

export function Rewind(props: Props) {
  const { instanceId, onRewind } = props;

  const form = useForm(schema);
  const apiClient = useApiClient();
  const { enqueueSnackbar, closeSnackbar } = useSnackbar();

  async function handleSaveClick() {
    try {
      const request: RewindRequest = {
        reason: form.value.reason,
      };

      await apiClient.rewindOrchestration(instanceId, request);

      enqueueSnackbar("Failures rewound", {
        variant: "success",
      });

      form.reset();

      onRewind?.();
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
                Rewind
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
