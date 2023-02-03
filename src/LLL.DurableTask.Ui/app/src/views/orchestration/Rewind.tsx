import { Button, Grid } from "@mui/material";
import { useSnackbar } from "notistack";
import React from "react";
import * as yup from "yup";
import { useApiClient } from "../../hooks/useApiClient";
import { TextField } from "../../form/TextField";
import { useForm } from "../../form/useForm";
import { RewindRequest } from "../../models/ApiModels";
import { useMutation } from "@tanstack/react-query";
import { LoadingButton } from "@mui/lab";

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

  const rewindMutation = useMutation<
    void,
    unknown,
    Parameters<typeof apiClient.rewindOrchestration>
  >((args) => apiClient.rewindOrchestration(...args));

  async function handleSaveClick() {
    try {
      const request: RewindRequest = {
        reason: form.value.reason,
      };

      await rewindMutation.mutateAsync([instanceId, request]);

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
              <LoadingButton
                variant="contained"
                color="primary"
                loading={rewindMutation.isLoading}
                onClick={handleSaveClick}
                disabled={
                  form.pendingValidation || Object.keys(form.errors).length > 0
                }
              >
                Rewind
              </LoadingButton>
            </Grid>
            <Grid item>
              <Button
                onClick={() => form.reset()}
                disabled={rewindMutation.isLoading}
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
