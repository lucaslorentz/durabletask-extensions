import { useAsync } from "react-use";
import { Feature } from "../models/ApiModels";

export function useFeatures() {
  return useAsync(async () => {
    var response = await fetch(`/api/v1/features`);
    var data = (await response.json()) as Feature[];
    const features: Partial<Record<Feature, true>> = {};
    data.forEach((f) => (features[f] = true));
    return features;
  }, []);
}
