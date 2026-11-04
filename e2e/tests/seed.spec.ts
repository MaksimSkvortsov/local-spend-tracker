import { test } from '@playwright/test';
import { HarnessHelper } from './pages/harness.helper';
import { Seeds } from './test-data/seeds';

/**
 * Environment setup for generated tests. Each test seeds its own starting state as its first step,
 * choosing Seeds.NewUser, Seeds.AiConfigured, Seeds.OneCard, or Seeds.ThreeCards.
 */
test.describe('Test group', () => {
  test('seed', async ({ page, request }) => {
    await new HarnessHelper(request).seed(Seeds.ThreeCards);
    await page.goto('/');
  });
});
