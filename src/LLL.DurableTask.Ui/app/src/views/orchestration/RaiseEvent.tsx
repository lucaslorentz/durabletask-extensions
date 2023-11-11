import { Button, Grid } from "@mui/material";
import { useSnackbar } from "notistack";
import React, { useCallback } from "react";
import * as yup from "yup";
import { useApiClient } from "../../hooks/useApiClient";
import { TextField } from "../../form/TextField";
import { useForm } from "../../form/useForm";
import { useMutation } from "@tanstack/react-query";
import { LoadingButton } from "@mui/lab";
import { CodeEditor } from "../../form/CodeEditor";

type Props = {
  instanceId: string;
  onRaiseEvent?: () => void;
};

const schema = yup
  .object({
    eventName: yup.string().label("Event name").default("").required(),
    eventData: yup
      .string()
      .label("Event data")
      .default("")
      .required()
      .test("JSON", "be a json", function (v) {
        if (!v) return true;
        try {
          JSON.parse(v);
          return true;
        } catch (e) {
          return this.createError({
            path: this.path,
            message: `Invalid JSON: ${e}`,
          });
        }
      }),
  })
  .required();

export function RaiseEvent(props: Props) {
  const { instanceId, onRaiseEvent } = props;

  const form = useForm(schema);
  const apiClient = useApiClient();
  const { enqueueSnackbar, closeSnackbar } = useSnackbar();

  const raiseEventMutation = useMutation<
    void,
    unknown,
    Parameters<typeof apiClient.raiseOrchestrationEvent>
  >({
    mutationFn: (args) => apiClient.raiseOrchestrationEvent(...args),
  });

  const handleSaveClick = useCallback(async () => {
    try {
      const eventName = form.value.eventName;
      const eventData = form.value.eventData
        ? JSON.parse(form.value.eventData)
        : null;

      await raiseEventMutation.mutateAsync([instanceId, eventName, eventData]);

      enqueueSnackbar("Event raised", {
        variant: "success",
      });

      form.reset();

      onRaiseEvent?.();
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
    onRaiseEvent,
    raiseEventMutation,
  ]);

  return (
    <div>
      <Grid container spacing={2}>
        <Grid item xs={12} md={6}>
          <TextField field={form.field("eventName")} />
        </Grid>
        <Grid item xs={12}>
          <CodeEditor
            field={form.field("eventData")}
            editorProps={{
              height: 200,
              defaultLanguage: "json",
            }}
          />
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
                loading={raiseEventMutation.isPending}
                disabled={
                  form.pendingValidation ||
                  Object.keys(form.errors).length > 0 ||
                  raiseEventMutation.isPending
                }
                onClick={handleSaveClick}
              >
                Raise Event
              </LoadingButton>
            </Grid>
            <Grid item>
              <Button
                onClick={() => form.reset()}
                disabled={raiseEventMutation.isPending}
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
