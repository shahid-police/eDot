import { savedRequest } from "./saved";

export function getAddresses() {
  return savedRequest("/api/v1/customers/me/addresses");
}
