import { defineConfig, devices } from '@playwright/test';
import path from 'path';

/**
 * Tests run against the UI harness on its own port, with fake AI and a data directory under e2e/,
 * so they never touch the desktop app's data, a manually started harness, or OpenAI.
 */
const harnessUrl = 'http://localhost:5246';
const harnessDataDirectory = path.resolve(__dirname, '.harness-data');

export default defineConfig({
  testDir: './tests',
  /* The harness holds one database and its settings in memory, so tests run one at a time. */
  fullyParallel: false,
  workers: 1,
  /* Fail the build on CI if you accidentally left test.only in the source code. */
  forbidOnly: !!process.env.CI,
  /* Retry on CI only */
  retries: process.env.CI ? 2 : 0,
  reporter: 'html',
  use: {
    baseURL: harnessUrl,
    trace: 'on-first-retry',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  webServer: {
    command: 'dotnet run --project ../src/Spendnest.UiHarness --no-launch-profile',
    url: harnessUrl,
    reuseExistingServer: false,
    timeout: 180_000,
    env: {
      ASPNETCORE_ENVIRONMENT: 'Development',
      ASPNETCORE_URLS: harnessUrl,
      SPENDNEST_UiHarness__DataDirectory: harnessDataDirectory,
      SPENDNEST_UiHarness__Ai: 'Fake',
    },
  },
});
