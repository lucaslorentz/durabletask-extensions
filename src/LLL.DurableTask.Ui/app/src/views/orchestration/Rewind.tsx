import { LoadingButton } from "@mui/lab";
import { Button, Grid } from "@mui/material";
import { useMutation } from "@tanstack/react-query";
import { useSnackbar } from "notistack";
import React, { useCallback } from "react";
import * as yup from "yup";
import { TextField } from "../../form/TextField";
import { useForm } from "../../form/useForm";
import { useApiClient } from "../../hooks/useApiClient";
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

  const rewindMutation = useMutation<
    void,
    unknown,
    Parameters<typeof apiClient.rewindOrchestration>
  >({
    mutationFn: (args) => apiClient.rewindOrchestration(...args),
  });

  const handleSaveClick = useCallback(async () => {
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
  }, [
    closeSnackbar,
    enqueueSnackbar,
    form,
    instanceId,
    onRewind,
    rewindMutation,
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
                loading={rewindMutation.isPending}
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
                disabled={rewindMutation.isPending}
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
