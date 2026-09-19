import { test, expect } from "@playwright/test";

const api = process.env.API_URL ?? "http://localhost:__HTTP_PORT__";

test("login + create todo", async ({ page, request }) => {
  const email = `e2e.${Date.now()}@example.com`;
  const password = "E2e!Passw0rd";
  const register = await request.post(`${api}/api/auth/register`, {
    data: { email, password, firstName: "E2e", lastName: "User" },
  });
  expect(register.ok(), await register.text()).toBeTruthy();

  await page.goto("/login", { waitUntil: "networkidle" });
  await expect(page.getByTestId("oidc-login")).toBeVisible();
  await Promise.all([
    page.waitForURL(/\/auth\/login|\/todos|\/callback/, { waitUntil: "domcontentloaded" }),
    page.getByTestId("oidc-login").click(),
  ]);

  if (page.url().includes("/auth/login")) {
    // Cookie login HTML is served by the API. Extensions 0.1.20-preview ForApi() CSP
    // is default-src 'none' without form-action, so a real form submit never navigates.
    // POST via the context cookie jar, then follow Location in the page (PKCE state stays).
    await expect(page.getByTestId("login-email")).toBeVisible();
    const returnUrl =
      (await page.locator('input[name="returnUrl"]').inputValue()) ||
      new URL(page.url()).searchParams.get("ReturnUrl") ||
      new URL(page.url()).searchParams.get("returnUrl") ||
      "/";
    const loginPost = await page.request.post(`${api}/auth/login`, {
      form: { email, password, returnUrl },
      maxRedirects: 0,
      failOnStatusCode: false,
    });
    expect(loginPost.status(), await loginPost.text()).toBe(302);
    const location = loginPost.headers()["location"];
    expect(location).toBeTruthy();
    const next = location!.startsWith("http") ? location! : `${api}${location}`;
    await page.goto(next, { waitUntil: "domcontentloaded" });
    await expect.poll(() => page.url(), { timeout: 20_000 }).toMatch(/\/callback|\/todos/);
  }

  if (page.url().includes("/callback")) {
    await expect.poll(() => page.url(), { timeout: 15_000 }).toMatch(/\/todos/);
  }

  if (!page.url().includes("/todos")) {
    await page.goto("/todos", { waitUntil: "domcontentloaded" });
  }

  await expect(page.getByTestId("todo-title")).toBeVisible({ timeout: 15_000 });
  await page.getByTestId("todo-title").fill("e2e item");
  await page.getByTestId("todo-add").click();
  await expect(page.getByTestId("todo-item").filter({ hasText: "e2e item" })).toBeVisible();
});
