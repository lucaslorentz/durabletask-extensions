import DeleteIcon from "@mui/icons-material/Delete";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Button,
  FormControl,
  FormControlLabel,
  FormGroup,
  Grid,
  IconButton,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Switch,
  TextField,
} from "@mui/material";
import { produce } from "immer";
import React, { useState } from "react";
import { useApiClient } from "../../hooks/useApiClient";
import { OrchestrationsFilter } from "../../hooks/useOrchestrationsFilter";
import { toLocalISO, toUtcISO } from "../../utils/date-utils";

interface Props {
  searchState: OrchestrationsFilter;
}

export function OrchestrationsSearch(props: Props) {
  const {
    searchState: {
      instanceIdPrefix: instanceId,
      setInstanceIdPrefix: setInstanceId,
      namePrefix: name,
      setNamePrefix: setName,
      createdTimeFrom,
      setCreatedTimeFrom,
      createdTimeTo,
      setCreatedTimeTo,
      runtimeStatus: statuses,
      setRuntimeStatus: setStatuses,
      includePreviousExecutions,
      setIncludePreviousExecutions,
      tags,
      setTags,
    },
  } = props;

  const apiClient = useApiClient();

  const [searchExpanded, setSearchExpanded] = useState(() =>
    Boolean(
      instanceId || name || createdTimeFrom || createdTimeTo || statuses.length
    )
  );

  return (
    <Accordion
      variant="outlined"
      expanded={searchExpanded}
      onChange={(_, ex) => setSearchExpanded(ex)}
    >
      <AccordionSummary expandIcon={<ExpandMoreIcon />}>
        Search
      </AccordionSummary>
      <AccordionDetails>
        <Grid container spacing={2}>
          {apiClient.hasFeature("SearchByInstanceId") && (
            <Grid item xs={12} sm={6} md={3}>
              <TextField
                fullWidth
                label="InstanceId"
                size="small"
                value={instanceId}
                onChange={(e) => setInstanceId(e.target.value)}
              />
            </Grid>
          )}
          {apiClient.hasFeature("SearchByName") && (
            <Grid item xs={12} sm={6} md={3}>
              <TextField
                fullWidth
                label="Name"
                size="small"
                value={name}
                onChange={(e) => setName(e.target.value)}
              />
            </Grid>
          )}
          {apiClient.hasFeature("SearchByCreatedTime") && (
            <>
              <Grid item xs={12} sm={6} md={3}>
                <TextField
                  fullWidth
                  label="Created Time From"
                  type="datetime-local"
                  size="small"
                  value={toLocalISO(createdTimeFrom)}
                  onChange={(e) => setCreatedTimeFrom(toUtcISO(e.target.value))}
                  InputLabelProps={{
                    shrink: true,
                  }}
                />
              </Grid>
              <Grid item xs={12} sm={6} md={3}>
                <TextField
                  fullWidth
                  label="Created Time To"
                  type="datetime-local"
                  size="small"
                  value={toLocalISO(createdTimeTo)}
                  onChange={(e) => setCreatedTimeTo(toUtcISO(e.target.value))}
                  InputLabelProps={{
                    shrink: true,
                  }}
                />
              </Grid>
            </>
          )}
          {apiClient.hasFeature("SearchByStatus") && (
            <Grid item xs={12} sm={6} md={3}>
              <FormControl fullWidth size="small">
                <InputLabel>Status</InputLabel>
                <Select
                  multiple
                  value={statuses}
                  onChange={(e) => setStatuses(e.target.value as any)}
                  label="Status"
                  MenuProps={{
                    anchorOrigin: {
                      vertical: "bottom",
                      horizontal: "right",
                    },
                    transformOrigin: {
                      vertical: "top",
                      horizontal: "right",
                    },
                  }}
                >
                  <MenuItem value="Pending">Pending</MenuItem>
                  <MenuItem value="Running">Running</MenuItem>
                  <MenuItem value="Completed">Completed</MenuItem>
                  <MenuItem value="ContinuedAsNew">ContinuedAsNew</MenuItem>
                  <MenuItem value="Failed">Failed</MenuItem>
                  <MenuItem value="Canceled">Canceled</MenuItem>
                  <MenuItem value="Terminated">Terminated</MenuItem>
                </Select>
              </FormControl>
            </Grid>
          )}
          {apiClient.hasFeature("StatePerExecution") && (
            <Grid item xs>
              <FormGroup>
                <FormControlLabel
                  control={
                    <Switch
                      checked={includePreviousExecutions}
                      onChange={(e) =>
                        setIncludePreviousExecutions(e.currentTarget.checked)
                      }
                    />
                  }
                  label="Include previous executions"
                />
              </FormGroup>
            </Grid>
          )}
          {apiClient.hasFeature("Tags") && (
            <>
              <Grid item xs={12}>
                <Button
                  onClick={() => setTags([...tags, { key: "", value: "" }])}
                >
                  Add tag
                </Button>
              </Grid>
              {tags.map((tag, index) => (
                <Grid key={index} item xs={12}>
                  <Stack direction="row" alignItems="start" spacing={1}>
                    <Grid container spacing={2}>
                      <Grid item xs={12} sm={6}>
                        <TextField
                          fullWidth
                          size="small"
                          label="Key"
                          value={tag.key}
                          onChange={(e) =>
                            setTags(
                              produce(tags, (d) => {
                                d[index].key = e.currentTarget.value;
                              })
                            )
                          }
                        />
                      </Grid>
                      <Grid item xs={12} sm={6}>
                        <TextField
                          fullWidth
                          size="small"
                          label="Value"
                          value={tag.value}
                          onChange={(e) =>
                            setTags(
                              produce(tags, (d) => {
                                d[index].value = e.currentTarget.value;
                              })
                            )
                          }
                        />
                      </Grid>
                    </Grid>
                    <IconButton
                      onClick={() =>
                        setTags(
                          produce(tags, (d) => {
                            d.splice(index, 1);
                          })
                        )
                      }
                    >
                      <DeleteIcon />
                    </IconButton>
                  </Stack>
                </Grid>
              ))}
            </>
          )}
        </Grid>
      </AccordionDetails>
    </Accordion>
  );
}
