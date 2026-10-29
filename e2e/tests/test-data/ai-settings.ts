/**
 * AI configuration entered in Settings. The e2e harness runs with fake AI, so the key is never sent to OpenAI.
 */
export interface AiSettings {
  apiKey: string;
  model: string;
  timeoutSeconds: string;
}

export class AiSettingsData {
  /** The form as a fresh install shows it: no key, default model and timeout. */
  static readonly Defaults: AiSettings = { apiKey: '', model: 'gpt-5.6-luna', timeoutSeconds: '25' };

  static readonly FakeKey: AiSettings = { apiKey: 'sk-test-fake-key', model: 'gpt-5.6-luna', timeoutSeconds: '25' };
}
