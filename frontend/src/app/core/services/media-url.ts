import { environment } from '../../../environments/environment';

const absoluteUrlPattern = /^(https?:)?\/\//i;
const browserUrlPattern = /^(data|blob):/i;

export function toMediaUrl(url?: string | null): string {
  const trimmed = url?.trim();
  if (!trimmed) return '';

  if (absoluteUrlPattern.test(trimmed) || browserUrlPattern.test(trimmed)) {
    return trimmed;
  }

  if (!trimmed.startsWith('/')) {
    return trimmed;
  }

  const apiRoot = environment.apiUrl.replace(/\/api\/?$/i, '');
  if (absoluteUrlPattern.test(apiRoot)) {
    return `${apiRoot}${trimmed}`;
  }

  return trimmed;
}

export function toNullableMediaUrl(url?: string | null): string | null {
  const resolved = toMediaUrl(url);
  return resolved || null;
}
