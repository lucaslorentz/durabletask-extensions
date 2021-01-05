import { Button, Grid } from "@material-ui/core";
import { useSnackbar } from "notistack";
import React from "react";
import * as yup from "yup";
import { useApiClient } from "../../ApiClientProvider";
import { TextField } from "../../form/TextField";
import { useForm } from "../../form/useForm";

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

  async function handleSaveClick() {
    try {
      const eventName = form.value.eventName;
      const eventData = form.value.eventData
        ? JSON.parse(form.value.eventData)
        : null;

      await apiClient.raiseOrchestrationEvent(instanceId, eventName, eventData);

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
  }

  return (
    <div>
      <Grid container spacing={2}>
        <Grid item xs={6}>
          <TextField field={form.field("eventName")} />
        </Grid>
        <Grid item xs={6}></Grid>
        <Grid item xs={12}>
          <TextField field={form.field("eventData")} multiline rows={6} />
        </Grid>
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
                Raise Event
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
