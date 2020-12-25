import { Button, Grid } from "@material-ui/core";
import React from "react";
import * as yup from "yup";
import { apiAxios } from "../../apiAxios";
import { TextField } from "../../form/fields";
import { useForm } from "../../form/form-hooks";
import { Observe } from "../../form/observation-components";
import { RaiseEventRequest } from "../../models/ApiModels";

type Props = {
  instanceId: string;
  onRaiseEvent?: () => void;
};

const schema = yup
  .object({
    eventName: yup.string().label("Event name").required(),
    eventData: yup
      .string()
      .label("Event data")
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

  const form = useForm(schema, () => ({
    eventName: "",
    eventData: "",
  }));

  async function handleSaveClick() {
    const request: RaiseEventRequest = {
      eventName: form.value.eventName,
      eventData: form.value.eventData ? JSON.parse(form.value.eventData) : null,
    };

    await apiAxios.post(`/v1/orchestrations/${instanceId}/raiseevent`, request);

    form.reset();

    onRaiseEvent?.();
  }

  return (
    <div>
      <Grid container spacing={2}>
        <Grid item xs={6}>
          {form.field("eventName").render((field) => (
            <TextField field={field} />
          ))}
        </Grid>
        <Grid item xs={6}></Grid>
        <Grid item xs={12}>
          {form.field("eventData").render((field) => (
            <TextField field={field} multiline rows={6} />
          ))}
        </Grid>
        <Observe form={form}>
          {({ form }) => (
            <Grid item xs={12} container spacing={1} justify="space-between">
              <Grid item>
                <Button
                  variant="contained"
                  color="primary"
                  onClick={handleSaveClick}
                  disabled={
                    form.pendingValidation ||
                    Object.keys(form.errors).length > 0
                  }
                >
                  Raise Event
                </Button>
              </Grid>
              <Grid item>
                <Button onClick={() => form.reset()}>Reset</Button>
              </Grid>
            </Grid>
          )}
        </Observe>
      </Grid>
    </div>
  );
}
