import { Dispatch, useMemo } from "react";
import { OrchestrationStatus } from "../models/ApiModels";
import { useQueryState } from "./useQueryState";

export interface OrchestrationsFilter {
  instanceIdPrefix: string;
  setInstanceIdPrefix: Dispatch<string>;
  namePrefix: string;
  setNamePrefix: Dispatch<string>;
  createdTimeFrom: string;
  setCreatedTimeFrom: Dispatch<string>;
  createdTimeTo: string;
  setCreatedTimeTo: Dispatch<string>;
  runtimeStatus: OrchestrationStatus[];
  setRuntimeStatus: Dispatch<OrchestrationStatus[]>;
  includePreviousExecutions: boolean;
  setIncludePreviousExecutions: Dispatch<boolean>;
  tags: { key: string; value: string }[];
  setTags: Dispatch<{ key: string; value: string }[]>;
}

export function useOrchestrationsFilter(): OrchestrationsFilter {
  const [instanceIdPrefix, setInstanceIdPrefix] = useQueryState<string>(
    "instanceIdPrefix",
    ""
  );
  const [namePrefix, setNamePrefix] = useQueryState<string>("namePrefix", "");
  const [createdTimeFrom, setCreatedTimeFrom] = useQueryState<string>(
    "createdTimeFrom",
    ""
  );
  const [createdTimeTo, setCreatedTimeTo] = useQueryState<string>(
    "createdTimeTo",
    ""
  );
  const [runtimeStatus, setRuntimeStatus] = useQueryState<
    OrchestrationStatus[]
  >("runtimeStatus", [], { multiple: true });
  const [includePreviousExecutions, setIncludePreviousExecutions] =
    useQueryState<boolean>("includePreviousExecutions", false, {
      parse: JSON.parse,
      stringify: JSON.stringify,
    });
  const [tags, setTags] = useQueryState<{ key: string; value: string }[]>(
    "tags",
    [],
    {
      parse: JSON.parse,
      stringify: JSON.stringify,
    }
  );

  return useMemo<OrchestrationsFilter>(() => {
    return {
      instanceIdPrefix,
      setInstanceIdPrefix,
      namePrefix,
      setNamePrefix,
      createdTimeFrom,
      setCreatedTimeFrom,
      createdTimeTo,
      setCreatedTimeTo,
      runtimeStatus,
      setRuntimeStatus,
      includePreviousExecutions,
      setIncludePreviousExecutions,
      tags,
      setTags,
    };
  }, [
    createdTimeFrom,
    createdTimeTo,
    includePreviousExecutions,
    instanceIdPrefix,
    namePrefix,
    setCreatedTimeFrom,
    setCreatedTimeTo,
    setIncludePreviousExecutions,
    setInstanceIdPrefix,
    setNamePrefix,
    setRuntimeStatus,
    runtimeStatus,
    tags,
    setTags,
  ]);
}
