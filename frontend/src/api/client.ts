import { getAccessToken } from './authToken';

const GATEWAY_URL = import.meta.env.VITE_GATEWAY_URL ?? 'http://localhost:5000';

export class ApiError extends Error {
  status: number;
  details?: unknown;

  constructor(message: string, status: number, details?: unknown) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.details = details;
  }
}

async function extractErrorMessage(res: Response): Promise<{ message: string; details?: unknown }> {
  const text = await res.text();
  if (!text) return { message: res.statusText || `Request failed (${res.status})` };

  try {
    const body = JSON.parse(text);
    if (typeof body === 'string') return { message: body, details: body };
    if (body?.message) return { message: body.message, details: body };
    if (body?.title) {
      // ASP.NET ValidationProblem shape: { title, errors: { field: [messages] } }
      const firstError = body.errors ? Object.values(body.errors).flat()[0] : undefined;
      return { message: (firstError as string) ?? body.title, details: body };
    }
    return { message: text, details: body };
  } catch {
    return { message: text };
  }
}

interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE';
  body?: unknown;
}

async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const token = getAccessToken();
  const headers: Record<string, string> = { Accept: 'application/json' };
  if (options.body !== undefined) headers['Content-Type'] = 'application/json';
  if (token) headers.Authorization = `Bearer ${token}`;

  const res = await fetch(`${GATEWAY_URL}${path}`, {
    method: options.method ?? 'GET',
    headers,
    body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
  });

  if (!res.ok) {
    const { message, details } = await extractErrorMessage(res);
    throw new ApiError(message, res.status, details);
  }

  if (res.status === 204) return undefined as T;

  const text = await res.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

export const api = {
  get: <T>(path: string) => request<T>(path),
  post: <T>(path: string, body?: unknown) => request<T>(path, { method: 'POST', body }),
  put: <T>(path: string, body?: unknown) => request<T>(path, { method: 'PUT', body }),
  del: <T>(path: string) => request<T>(path, { method: 'DELETE' }),
};
