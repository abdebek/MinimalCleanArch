import { createMcaAuth } from "./auth";
import { apiUrl, spaOrigin } from "./config";

export const auth = createMcaAuth({
  authority: apiUrl,
  redirectUri: `${spaOrigin}/callback`,
  postLogoutRedirectUri: `${spaOrigin}/`,
});
