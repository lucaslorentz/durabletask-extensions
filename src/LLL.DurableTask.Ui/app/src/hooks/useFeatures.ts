import { useAsync } from "react-use";
import { apiAxios } from "../apiAxios";
import { Feature } from "../models/ApiModels";

export function useFeatures() {
  return useAsync(async () => {
    var response = await apiAxios.get<Feature[]>(`/v1/features`);
    const features: Partial<Record<Feature, true>> = {};
    response.data.forEach((f) => (features[f] = true));
    return features;
  }, []);
}
