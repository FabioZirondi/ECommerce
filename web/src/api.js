const TOKEN_KEY = "ecommerce.token";
const USER_KEY = "ecommerce.user";

export function getToken() {
  return localStorage.getItem(TOKEN_KEY);
}

export function getStoredUser() {
  const raw = localStorage.getItem(USER_KEY);
  return raw ? JSON.parse(raw) : null;
}

export function storeSession(auth) {
  localStorage.setItem(TOKEN_KEY, auth.accessToken);
  localStorage.setItem(USER_KEY, JSON.stringify(auth.user));
}

export function clearSession() {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(USER_KEY);
}

export async function api(path, options = {}) {
  const { timeoutMs = 8000, ...fetchOptions } = options;
  const headers = { ...(fetchOptions.headers ?? {}) };
  if (fetchOptions.body && !(fetchOptions.body instanceof FormData)) {
    headers["Content-Type"] = "application/json";
  }

  const token = getToken();
  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  let response;
  try {
    response = await fetch(path, {
      ...fetchOptions,
      headers,
      signal: fetchOptions.signal ?? AbortSignal.timeout(timeoutMs)
    });
  } catch {
    throw new Error("Não foi possível conectar à API. Suba o backend em localhost:5264.");
  }

  const text = await response.text();
  let data = null;
  if (text) {
    try {
      data = JSON.parse(text);
    } catch {
      data = { message: text.slice(0, 180) };
    }
  }

  if (!response.ok) {
    const details = data?.errors
      ? Object.values(data.errors).flat().join(" ")
      : "";
    throw new Error([data?.message, details].filter(Boolean).join(" "));
  }

  return data;
}

export const money = (value) =>
  new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: "BRL",
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  }).format(Number(value) || 0);

export function parseMoney(raw) {
  const text = String(raw ?? "").trim().replace(/[R$\s]/gi, "");
  if (!text) return NaN;
  const lastComma = text.lastIndexOf(",");
  const lastDot = text.lastIndexOf(".");
  if (lastComma > lastDot) {
    return Number(text.replace(/\./g, "").replace(",", "."));
  }
  if (/^\d{1,3}(\.\d{3})+$/.test(text)) {
    return Number(text.replace(/\./g, ""));
  }
  return Number(text.replace(",", "."));
}

export function coverUrl(product) {
  const uploaded = product?.images?.find(Boolean) || product?.image;
  if (uploaded) {
    return uploaded;
  }
  return `https://picsum.photos/seed/${encodeURIComponent(product?.id ?? product?.productId ?? "item")}/800/600`;
}

export async function uploadImage(file) {
  const body = new FormData();
  body.append("file", file);
  const data = await api("/api/uploads", { method: "POST", body, timeoutMs: 30000 });
  return data.url;
}
