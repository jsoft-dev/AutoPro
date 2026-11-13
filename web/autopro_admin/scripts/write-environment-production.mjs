/**
 * Writes environment.production.ts before Render/static builds.
 * Set API_BASE_URL in the Render dashboard (build-time env var).
 */
import { writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const apiBaseUrl =
  process.env.API_BASE_URL?.trim() || 'https://autopro-api-6mkp.onrender.com';

const target = join(dirname(fileURLToPath(import.meta.url)), '..', 'src', 'environments', 'environment.production.ts');

const content = `export const environment = {
  production: true,
  apiBaseUrl: '${apiBaseUrl.replace(/\\/g, '\\\\').replace(/'/g, "\\'")}',
};
`;

writeFileSync(target, content, 'utf8');
console.log(`[write-environment-production] apiBaseUrl → ${apiBaseUrl}`);
