import { Button, Grid } from "@material-ui/core";
import { useSnackbar } from "notistack";
import React, { useState } from "react";
import * as yup from "yup";
import { useApiClient } from "../../ApiClientProvider";
import { ErrorAlert } from "../../components/ErrorAlert";
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
  const [error, setError] = useState<any>();
  const { enqueueSnackbar } = useSnackbar();

  async function handleSaveClick() {
    try {
      setError(undefined);

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
      setError(error);
    }
  }

  return (
    <div>
      <Grid container spacing={2}>
        <Grid item xs={12}>
          <TextField field={form.field("reason")} multiline rows={6} />
        </Grid>
        {error && (
          <Grid item xs={12}>
            <ErrorAlert error={error} />
          </Grid>
        )}
        {form.render((form) => (
          <Grid item xs={12} container spacing={1} justify="space-between">
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
