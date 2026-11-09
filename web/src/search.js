const PLACE_KEY = "ecommerce.place";

export function getNearPlace() {
  try {
    const raw = localStorage.getItem(PLACE_KEY);
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
}

export function setNearPlace(place) {
  const city = String(place?.city ?? "").trim();
  const state = String(place?.state ?? "").trim();
  const zipCode = String(place?.zipCode ?? "").replace(/\D/g, "").slice(0, 8);
  if (!city && zipCode.length !== 8) {
    localStorage.removeItem(PLACE_KEY);
    return;
  }
  localStorage.setItem(PLACE_KEY, JSON.stringify({
    city,
    state,
    zipCode: zipCode.length === 8 ? zipCode : ""
  }));
}

export function resolveNear(user) {
  const stored = getNearPlace();
  const city = (user?.city || stored?.city || "").trim();
  const state = (user?.state || stored?.state || "").trim();
  return city || state ? { city, state } : null;
}

export function resolveDeliveryDest(user) {
  const stored = getNearPlace();
  const zipCode = String(user?.zipCode || stored?.zipCode || "").trim();
  const city = (user?.city || stored?.city || "").trim();
  const state = (user?.state || stored?.state || "").trim();
  if (!city && zipCode.replace(/\D/g, "").length !== 8) {
    return null;
  }
  return { city, state, zipCode };
}

export function formatPlace(city, state) {
  const place = String(city ?? "").trim();
  const uf = String(state ?? "").trim();
  if (!place) return uf || "Brasil";
  if (!uf || new RegExp(`(?:^|[\\s,\\-/])${uf}$`, "i").test(place)) return place;
  return `${place} - ${uf}`;
}
