import { test, expect } from "@playwright/test";

const api = process.env.API_URL ?? "http://localhost:__HTTP_PORT__";

test("login + create todo", async ({ page, request }) => {
  const email = `e2e.${Date.now()}@example.com`;
  const password = "E2e!Passw0rd";
  const register = await request.post(`${api}/api/auth/register`, {
    data: { email, password, firstName: "E2e", lastName: "User" },
  });
  expect(register.ok()).toBeTruthy();

  await page.goto("/login");
  await page.getByRole("button", { name: /openid connect/i }).click();
  await page.waitForURL(/\/auth\/login|\/todos|\/callback/);

  if (page.url().includes("/auth/login")) {
    await page.locator('input[name="email"], input[type="email"]').first().fill(email);
    await page.locator('input[name="password"], input[type="password"]').first().fill(password);
    await page.locator('button[type="submit"], button:has-text("Sign")').first().click();
    await page.waitForURL(/\/todos|\/callback/);
  }

  await page.goto("/todos");
  await page.getByTestId("todo-title").fill("e2e item");
  await page.getByTestId("todo-add").click();
  await expect(page.getByTestId("todo-item").filter({ hasText: "e2e item" })).toBeVisible();
});
