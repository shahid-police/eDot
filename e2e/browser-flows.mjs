// Browser flows for the storefront, run against a running API and frontend.
//
// Environment:
//   E2E_WEB_URL   frontend base URL          (default http://localhost:3000)
//   BROWSER_PATH  Chromium-based browser     (default Microsoft Edge install path)
//   E2E_HEADLESS  "false" to watch the run   (default headless)
//
// The auth limiter allows 10 logins or registrations per minute per IP in the
// default configuration. Development sets RateLimiting:AuthRequestsPerMinute
// higher; run against Development or wait a minute between runs.

import { chromium } from "playwright-core";

const BASE = process.env.E2E_WEB_URL ?? "http://localhost:3000";
const BROWSER_PATH =
  process.env.BROWSER_PATH ?? "C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe";
const HEADLESS = process.env.E2E_HEADLESS !== "false";
const EMAIL = `e2e${Date.now()}@example.com`;
const PASSWORD = "Passw0rd!long";
const PRODUCT_NAME = "Oval Halo Engagement Ring";

const results = [];
const pageErrors = [];

const browser = await chromium.launch({ executablePath: BROWSER_PATH, headless: HEADLESS });
const context = await browser.newContext({ viewport: { width: 1280, height: 900 } });
const page = await context.newPage();
page.on("pageerror", (err) => pageErrors.push(err.message));

const go = async (path) => {
  await page.goto(`${BASE}${path}`, { waitUntil: "domcontentloaded" });
  // Bootstrap's data-bs-toggle wiring loads after first paint.
  await page.waitForTimeout(1500);
};

const step = async (name, fn) => {
  try {
    await fn();
    results.push({ name, ok: true });
    console.log(`PASS  ${name}`);
  } catch (err) {
    results.push({ name, ok: false });
    console.log(`FAIL  ${name}  - ${err.message.split("\n")[0]}`);
  }
};

const text = (value) => page.getByText(value, { exact: false }).first();

// React-controlled inputs: set the value natively and fire the input event
// so React's onChange sees it.
const setField = async (scope, index, value) => {
  await page.evaluate(
    ({ selector, index, value }) => {
      const input = document.querySelectorAll(selector)[index];
      const setter = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, "value").set;
      setter.call(input, value);
      input.dispatchEvent(new Event("input", { bubbles: true }));
    },
    { selector: `${scope} input`, index, value }
  );
};

const openLogin = async () => {
  await page.locator('a[href="#log"]:visible').first().click({ force: true });
  await page.locator("#log").waitFor({ state: "visible", timeout: 10000 });
  await page.waitForTimeout(800);
};

await step("shop lists API products", async () => {
  await go("/shop-default");
  // A page prerendered while the API was down serves template data until its
  // 60-second revalidation runs; one reload after that picks up the API data.
  const fresh = await text(PRODUCT_NAME).waitFor({ timeout: 5000 }).then(() => true, () => false);
  if (!fresh) {
    await page.waitForTimeout(61000);
    await page.reload({ waitUntil: "domcontentloaded" });
  }
  await text(PRODUCT_NAME).waitFor({ timeout: 15000 });
});

await step("product detail shows bag, wishlist and compare controls", async () => {
  await go("/product-default/oval-halo-engagement-ring");
  await page.getByRole("button", { name: "add to bag" }).waitFor({ timeout: 15000 });
  await page.getByRole("button", { name: "add to wishlist" }).waitFor();
  await page.getByRole("button", { name: "add to compare" }).waitFor();
});

await step("guest wishlist survives reload", async () => {
  await page.getByRole("button", { name: "add to wishlist" }).click();
  await page.getByRole("button", { name: "remove from wishlist" }).waitFor({ timeout: 10000 });
  await page.reload({ waitUntil: "domcontentloaded" });
  await page.getByRole("button", { name: "remove from wishlist" }).waitFor({ timeout: 10000 });
});

await step("guest compare shows on the compare page", async () => {
  await page.getByRole("button", { name: "add to compare" }).click();
  await page.getByRole("button", { name: "remove from compare" }).waitFor({ timeout: 10000 });
  await go("/compare");
  await text(PRODUCT_NAME).waitFor({ timeout: 10000 });
});

await step("guest add to bag lands on cart", async () => {
  await go("/product-default/oval-halo-engagement-ring");
  await page.getByRole("button", { name: "add to bag" }).click();
  await page.waitForURL("**/shop-cart", { timeout: 15000 });
  await text(PRODUCT_NAME).waitFor({ timeout: 15000 });
  await text("$24900.00").waitFor({ timeout: 10000 });
});

await step("demo product page has no add-to-bag control", async () => {
  await go("/product-default/1");
  const controls = await page
    .locator("button, a")
    .filter({ hasText: /add to (bag|cart)/i })
    .count();
  if (controls > 0) throw new Error(`found ${controls} add controls`);
});

await step("register carries guest cart, wishlist and compare into the account", async () => {
  await go("/shop-default");
  await openLogin();
  await page.locator('#log a[href="#register"]').click({ force: true });
  const modal = page.locator("#register");
  await modal.waitFor({ state: "visible", timeout: 10000 });
  await page.waitForTimeout(800);
  await setField("#register form", 0, "Browser");
  await setField("#register form", 1, "Tester");
  await setField("#register form", 2, EMAIL);
  await setField("#register form", 3, PASSWORD);
  await modal.locator("button[type=submit]").click();
  await page.waitForFunction(() => !document.querySelector("#register.show"), null, { timeout: 15000 });
  await go("/shop-cart");
  await text(PRODUCT_NAME).waitFor({ timeout: 15000 });
  await go("/wishlist");
  await text(PRODUCT_NAME).waitFor({ timeout: 15000 });
  await go("/compare");
  await text(PRODUCT_NAME).waitFor({ timeout: 15000 });
});

await step("account greets the customer and survives reload", async () => {
  await go("/account-page");
  await text("BROWSER TESTER").waitFor({ timeout: 15000 });
  await page.reload({ waitUntil: "domcontentloaded" });
  await text("BROWSER TESTER").waitFor({ timeout: 15000 });
});

await step("log out from the account sidebar returns to guest greeting", async () => {
  await page.getByText("Log Out", { exact: false }).first().click();
  await text("GUEST").waitFor({ timeout: 15000 });
});

await step("wrong password shows an error", async () => {
  await go("/shop-default");
  await openLogin();
  await setField("#log form", 0, EMAIL);
  await setField("#log form", 1, "wrong-password-1");
  await page.locator("#log button[type=submit]").click();
  await page.locator("#log").getByText("Invalid email or password.").waitFor({ timeout: 15000 });
});

await step("correct password signs in", async () => {
  await setField("#log form", 1, PASSWORD);
  await page.locator("#log button[type=submit]").click();
  await page.waitForFunction(() => !document.querySelector("#log.show"), null, { timeout: 15000 });
  await go("/account-page");
  await text("BROWSER TESTER").waitFor({ timeout: 15000 });
});

await step("cart drawer lists the server cart", async () => {
  await go("/shop-default");
  await page.locator('a[href="#shoppingCart"]:visible').first().click({ force: true });
  await page.locator("#shoppingCart.show").waitFor({ timeout: 10000 });
  await page.locator("#shoppingCart").getByText(PRODUCT_NAME).waitFor({ timeout: 15000 });
});

await step("checkout shows the server subtotal and no template discount", async () => {
  await go("/checkout");
  await text("SHIPPING AND TAXES").waitFor({ timeout: 15000 });
  await text("$24900.00").waitFor({ timeout: 10000 });
  if ((await page.getByText("DISCOUNT:", { exact: false }).count()) > 0) {
    throw new Error("template discount line present");
  }
});

await browser.close();

const failed = results.filter((r) => !r.ok);
console.log(`\n${results.length - failed.length}/${results.length} steps passed`);
if (pageErrors.length) {
  console.log(`Page errors (${pageErrors.length}):`);
  [...new Set(pageErrors)].forEach((e) => console.log(`  ${e}`));
}
process.exit(failed.length || pageErrors.length ? 1 : 0);
